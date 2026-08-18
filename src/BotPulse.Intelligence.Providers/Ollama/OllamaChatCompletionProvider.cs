using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Configuration;
using Microsoft.Extensions.Options;

namespace BotPulse.Intelligence.Providers.Ollama;

/// <summary>
/// Chat completion provider backed by a local Ollama instance. No API key
/// required — suitable for on-prem / privacy-sensitive deployments
/// (ADR-016 §4, Implementation Constraint).
/// </summary>
public sealed class OllamaChatCompletionProvider : IChatCompletionProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public string Name => "Ollama";

    public OllamaChatCompletionProvider(HttpClient httpClient, IOptions<IntelligenceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Ollama;
    }

    public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_options.Endpoint}/api/chat",
            BuildPayload(request, stream: false),
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(cancellationToken: ct)
            .ConfigureAwait(false);

        return new ChatResponse(
            Content: body?.Message?.Content ?? string.Empty,
            PromptTokens: body?.PromptEvalCount ?? 0,
            CompletionTokens: body?.EvalCount ?? 0);
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.Endpoint}/api/chat")
        {
            Content = JsonContent.Create(BuildPayload(request, stream: true))
        };

        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var chunk = JsonSerializer.Deserialize<OllamaChatResponse>(line);
            if (chunk?.Message?.Content is { Length: > 0 } content)
            {
                yield return content;
            }

            if (chunk?.Done == true)
            {
                yield break;
            }
        }
    }

    private object BuildPayload(ChatRequest request, bool stream) => new
    {
        model = _options.ChatModel,
        stream,
        options = new { temperature = request.Temperature },
        messages = request.Messages.Select(m => new
        {
            role = m.Role.ToString().ToLowerInvariant(),
            content = m.Content
        })
    };

    private sealed record OllamaChatResponse(
        OllamaChatMessage? Message,
        bool Done,
        int? PromptEvalCount,
        int? EvalCount);

    private sealed record OllamaChatMessage(string Content);
}
