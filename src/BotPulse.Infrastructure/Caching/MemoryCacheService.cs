using System.Collections.Concurrent;
using BotPulse.Core.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BotPulse.Infrastructure.Caching;

/// <summary>
/// In-process memory cache implementation of ICacheService.
/// Uses IMemoryCache with a ConcurrentDictionary for prefix-based invalidation.
/// Future: replace with RedisCacheService without changing consumers.
/// </summary>
internal sealed class MemoryCacheService : ICacheService
{
    private static readonly Action<ILogger, string, TimeSpan, Exception?> LogCacheSet =
        LoggerMessage.Define<string, TimeSpan>(
            LogLevel.Debug,
            new EventId(1, nameof(SetAsync)),
            "Cache set: {Key} (TTL {Ttl})");

    private static readonly Action<ILogger, int, string, Exception?> LogCacheInvalidated =
        LoggerMessage.Define<int, string>(
            LogLevel.Debug,
            new EventId(2, nameof(InvalidatePatternAsync)),
            "Cache invalidated {Count} keys matching prefix '{Pattern}'");

    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        _cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    /// <inheritdoc/>
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class
    {
        _cache.Set(key, value, ttl);
        _keys.TryAdd(key, 0);
        LogCacheSet(_logger, key, ttl, null);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task InvalidatePatternAsync(string pattern, CancellationToken ct = default)
    {
        var toRemove = _keys.Keys
            .Where(k => k.StartsWith(pattern, StringComparison.Ordinal))
            .ToList();

        foreach (var key in toRemove)
        {
            _cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        LogCacheInvalidated(_logger, toRemove.Count, pattern, null);
        return Task.CompletedTask;
    }
}
