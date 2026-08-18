using System.Security.Cryptography;
using BotPulse.Intelligence.Contracts.AI;

namespace BotPulse.Intelligence.Providers.Demo;

/// <summary>
/// Deterministic, dependency-free embedding provider for demos and local
/// development (mirrors the Demo RPA provider philosophy, ADR-014).
/// Produces a stable pseudo-embedding from the text's SHA-256 hash so the
/// full RAG pipeline works end-to-end without any external AI service.
/// </summary>
public sealed class DemoEmbeddingProvider : IEmbeddingProvider
{
    public string Name => "Demo";
    public int Dimensions { get; } = 64;

    public Task<EmbeddingResult> EmbedAsync(string text, CancellationToken ct = default)
    {
        return Task.FromResult(new EmbeddingResult(Embed(text), Dimensions));
    }

    public Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        IReadOnlyList<EmbeddingResult> results = texts
            .Select(t => new EmbeddingResult(Embed(t), Dimensions))
            .ToList();
        return Task.FromResult(results);
    }

    private float[] Embed(string text)
    {
        // Derive a stable vector from the content hash. Same text always maps
        // to the same vector, so similarity search behaves consistently.
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text ?? string.Empty));
        var vector = new float[Dimensions];
        for (var i = 0; i < Dimensions; i++)
        {
            // Map each byte (cycled) into [-1, 1].
            vector[i] = (hash[i % hash.Length] / 127.5f) - 1f;
        }

        return vector;
    }
}
