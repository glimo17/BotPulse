using System.Net.Http.Json;
using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Configuration;
using Microsoft.Extensions.Options;

namespace BotPulse.Intelligence.Providers.Ollama;

/// <summary>
/// Embedding provider backed by a local Ollama instance. No API key required —
/// suitable for on-prem / privacy-sensitive deployments (ADR-016 §4).
/// </summary>
public sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _options;

    public string Name => "Ollama";

    // nomic-embed-text default dimensionality; adjust via configuration if a
    // different embedding model is used.
    public int Dimensions { get; }

    public OllamaEmbeddingProvider(HttpClient httpClient, IOptions<IntelligenceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value.Ollama;
        Dimensions = 768;
    }

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"{_options.Endpoint}/api/embeddings",
            new { model = _options.EmbeddingModel, prompt = text },
            ct).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: ct)
            .ConfigureAwait(false);

        var vector = body?.Embedding ?? Array.Empty<float>();
        return new EmbeddingResult(vector, vector.Length);
    }

    public async Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var results = new List<EmbeddingResult>(texts.Count);
        foreach (var text in texts)
        {
            results.Add(await EmbedAsync(text, ct).ConfigureAwait(false));
        }

        return results;
    }

    private sealed record OllamaEmbeddingResponse(float[] Embedding);
}
