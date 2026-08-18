using System.Security.Cryptography;
using System.Text;
using BotPulse.Intelligence.Contracts.AI;
using Microsoft.Extensions.Caching.Memory;

namespace BotPulse.Intelligence.Caching;

/// <summary>
/// Decorator over IChatCompletionProvider that caches full completions by
/// request content hash. Identical prompts are never re-sent to the LLM,
/// reducing cost and latency (ADR-016 NFR-01). Streaming responses are never
/// cached — only CompleteAsync results.
/// </summary>
public sealed class CachedChatCompletionProvider : IChatCompletionProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);

    private readonly IChatCompletionProvider _inner;
    private readonly IMemoryCache _cache;

    public string Name => _inner.Name;

    public CachedChatCompletionProvider(IChatCompletionProvider inner, IMemoryCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default)
    {
        var key = CacheKey(request);
        if (_cache.TryGetValue(key, out ChatResponse? cached))
        {
            return cached!;
        }

        var result = await _inner.CompleteAsync(request, ct).ConfigureAwait(false);
        _cache.Set(key, result, CacheDuration);
        return result;
    }

    public IAsyncEnumerable<string> StreamAsync(ChatRequest request, CancellationToken ct = default) =>
        _inner.StreamAsync(request, ct);

    /// <summary>Deterministic cache key based on the SHA-256 hash of the request content.</summary>
    private static string CacheKey(ChatRequest request)
    {
        var sb = new StringBuilder();
        sb.Append(request.Temperature).Append('|').Append(request.MaxTokens).Append('|');
        foreach (var message in request.Messages)
        {
            sb.Append(message.Role).Append(':').Append(message.Content).Append('\n');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return $"chat:{Convert.ToHexString(hash)}";
    }
}
