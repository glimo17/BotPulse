namespace BotPulse.Core.Abstractions.Caching;

/// <summary>
/// Abstraction for application-level caching.
/// MVP: in-memory. Future: Redis, distributed cache.
/// Business services depend only on this interface.
/// </summary>
public interface ICacheService
{
    /// <summary>Gets a cached value by key, or null if not found or expired.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>Sets a value in cache with the specified TTL.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default) where T : class;

    /// <summary>Removes a single entry by key.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Removes all entries whose key starts with the given prefix pattern.</summary>
    Task InvalidatePatternAsync(string pattern, CancellationToken ct = default);
}
