using BotPulse.Core.Abstractions.Caching;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Application.Robots;

/// <summary>
/// Query service for robots. Reads on-demand from the RPA provider with optional short-lived cache.
/// Robots are NOT persisted locally — always read from the provider.
/// </summary>
public sealed class RobotQueryService
{
    private const string CacheKeyAll = "robots.all";
    private const string CacheKeyPrefix = "robots.";
    private readonly IRobotProvider _provider;
    private readonly ICacheService _cache;
    private readonly RobotCacheOptions _cacheOptions;

    public RobotQueryService(IRobotProvider provider, ICacheService cache, RobotCacheOptions cacheOptions)
    {
        _provider = provider;
        _cache = cache;
        _cacheOptions = cacheOptions;
    }

    /// <summary>Returns all robots, from cache if available and forceRefresh is false.</summary>
    public async Task<IReadOnlyList<RobotSnapshot>> GetRobotsAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && _cacheOptions.Enabled)
        {
            var cached = await _cache.GetAsync<List<RobotSnapshot>>(CacheKeyAll, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }
        }

        var robots = await _provider.GetRobotsAsync(ct).ConfigureAwait(false);

        if (_cacheOptions.Enabled)
        {
            await _cache.SetAsync(CacheKeyAll, robots.ToList(), TimeSpan.FromSeconds(_cacheOptions.TtlSeconds), ct)
                .ConfigureAwait(false);
        }

        return robots;
    }

    /// <summary>Returns a single robot by external ID.</summary>
    public async Task<RobotSnapshot?> GetRobotByIdAsync(string externalId, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{externalId}";

        if (_cacheOptions.Enabled)
        {
            var cached = await _cache.GetAsync<RobotSnapshot>(cacheKey, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }
        }

        var robot = await _provider.GetRobotByIdAsync(externalId, ct).ConfigureAwait(false);

        if (robot is not null && _cacheOptions.Enabled)
        {
            await _cache.SetAsync(cacheKey, robot, TimeSpan.FromSeconds(_cacheOptions.TtlSeconds), ct)
                .ConfigureAwait(false);
        }

        return robot;
    }
}

/// <summary>Cache configuration for RobotQueryService.</summary>
public sealed class RobotCacheOptions
{
    public bool Enabled { get; init; } = true;
    public int TtlSeconds { get; init; } = 120;
}
