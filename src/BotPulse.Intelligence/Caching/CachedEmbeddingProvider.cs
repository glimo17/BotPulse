using System.Security.Cryptography;
using System.Text;
using BotPulse.Intelligence.Contracts.AI;
using Microsoft.Extensions.Caching.Memory;

namespace BotPulse.Intelligence.Caching;

/// <summary>
/// Decorator over IEmbeddingProvider that caches embeddings by content hash.
/// Identical text is never re-embedded, reducing cost and latency (ADR-016 NFR-01).
/// </summary>
public sealed class CachedEmbeddingProvider : IEmbeddingProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly IEmbeddingProvider _inner;
    private readonly IMemoryCache _cache;

    public string Name => _inner.Name;
    public int Dimensions => _inner.Dimensions;

    public CachedEmbeddingProvider(IEmbeddingProvider inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken ct = default)
    {
        var key = CacheKey(text);
        if (_cache.TryGetValue(key, out EmbeddingResult? cached))
        {
            return cached!;
        }

        var result = await _inner.EmbedAsync(text, ct).ConfigureAwait(false);
        _cache.Set(key, result, CacheDuration);
        return result;
    }

    public async Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default)
    {
        var results = new EmbeddingResult[texts.Count];
        var uncachedIndexes = new List<int>();
        var uncachedTexts = new List<string>();

        for (var i = 0; i < texts.Count; i++)
        {
            if (_cache.TryGetValue(CacheKey(texts[i]), out EmbeddingResult? cached))
            {
                results[i] = cached!;
            }
            else
            {
                uncachedIndexes.Add(i);
                uncachedTexts.Add(texts[i]);
            }
        }

        if (uncachedTexts.Count > 0)
        {
            var fresh = await _inner.EmbedBatchAsync(uncachedTexts, ct).ConfigureAwait(false);
            for (var i = 0; i < fresh.Count; i++)
            {
                var index = uncachedIndexes[i];
                results[index] = fresh[i];
                _cache.Set(CacheKey(texts[index]), fresh[i], CacheDuration);
            }
        }

        return results;
    }

    /// <summary>Deterministic cache key based on the SHA-256 hash of the text content.</summary>
    private static string CacheKey(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return $"embedding:{Convert.ToHexString(hash)}";
    }
}
