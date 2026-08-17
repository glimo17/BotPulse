using System.Net.Http.Headers;
using System.Net.Http.Json;
using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Configuration;
using Microsoft.Extensions.Options;

namespace BotPulse.Intelligence.Providers.OpenAI;

/// <summary>
/// Embedding provider backed by the OpenAI Embeddings API.
/// </summary>
public sealed class OpenAIEmbeddingProvider : IEmbeddingProvider
{
    private const string BaseUrl = "https://api.openai.com/v1/embeddings";

    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;

    public string Name => "OpenAI";

    // text-embedding-3-small default dimensionality.
    public int Dimensions { get; } = 1536;

    public OpenAIEmbeddingProvider(HttpClient httpClient, IOptions<IntelligenceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.OpenAI;
    }

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken ct = default)
    {
        var results = await EmbedBatchAsync(new[] { text }, ct).ConfigureAwait(false);
        return results[0];
    }

    public async Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl)
        {
            Content = JsonContent.Create(new { model = _options.EmbeddingModel, input = texts })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OpenAIEmbeddingResponse>(cancellationToken: ct)
            .ConfigureAwait(false);

        return body?.Data
            .Select(d => new EmbeddingResult(d.Embedding, d.Embedding.Length))
            .ToList()
            ?? new List<EmbeddingResult>();
    }

    private sealed record OpenAIEmbeddingResponse(IReadOnlyList<OpenAIEmbeddingData> Data);
    private sealed record OpenAIEmbeddingData(float[] Embedding);
}
