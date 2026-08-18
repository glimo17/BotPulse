using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Configuration;
using Microsoft.Extensions.Options;

namespace BotPulse.Intelligence.Providers.OpenAI;

/// <summary>
/// Chat completion provider backed by the OpenAI Chat Completions API.
/// </summary>
public sealed class OpenAIChatCompletionProvider : IChatCompletionProvider
{
    private const string BaseUrl = "https://api.openai.com/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;

    public string Name => "OpenAI";

    public OpenAIChatCompletionProvider(HttpClient httpClient, IOptions<IntelligenceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.OpenAI;
    }

    public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default)
    {
        using var httpRequest = BuildRequest(request, stream: false);
        var response = await _httpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(cancellationToken: ct)
            .ConfigureAwait(false);

        var choice = body?.Choices is { Count: > 0 } choices ? choices[0] : null;
        return new ChatResponse(
            Content: choice?.Message?.Content ?? string.Empty,
            PromptTokens: body?.Usage?.PromptTokens ?? 0,
            CompletionTokens: body?.Usage?.CompletionTokens ?? 0);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var httpRequest = BuildRequest(request, stream: true);
        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: ", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = line["data: ".Length..];
            if (payload == "[DONE]")
            {
                yield break;
            }

            var chunk = JsonSerializer.Deserialize<OpenAIStreamChunk>(payload);
            var delta = chunk?.Choices is { Count: > 0 } streamChoices ? streamChoices[0].Delta?.Content : null;
            if (!string.IsNullOrEmpty(delta))
            {
                yield return delta;
            }
        }
    }

    private HttpRequestMessage BuildRequest(ChatRequest request, bool stream)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = JsonContent.Create(new
            {
                model = _options.ChatModel,
                stream,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                messages = request.Messages.Select(m => new
                {
                    role = m.Role.ToString().ToLowerInvariant(),
                    content = m.Content
                })
            })
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        return httpRequest;
    }

    private sealed record OpenAIChatResponse(IReadOnlyList<OpenAIChoice>? Choices, OpenAIUsage? Usage);
    private sealed record OpenAIChoice(OpenAIMessage? Message);
    private sealed record OpenAIMessage(string Content);
    private sealed record OpenAIUsage(int PromptTokens, int CompletionTokens);

    private sealed record OpenAIStreamChunk(IReadOnlyList<OpenAIStreamChoice>? Choices);
    private sealed record OpenAIStreamChoice(OpenAIStreamDelta? Delta);
    private sealed record OpenAIStreamDelta(string? Content);
}
