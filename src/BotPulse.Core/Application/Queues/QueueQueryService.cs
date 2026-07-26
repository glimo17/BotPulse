using BotPulse.Core.Abstractions.Caching;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Application.Queues;

/// <summary>Queue metadata query service — reads on-demand from provider with cache.</summary>
public sealed class QueueQueryService
{
    private const string CacheKeyAll = "queues.all";
    private readonly IQueueProvider _provider;
    private readonly ICacheService _cache;
    private readonly QueueCacheOptions _cacheOptions;

    public QueueQueryService(IQueueProvider provider, ICacheService cache, QueueCacheOptions cacheOptions)
    {
        _provider = provider;
        _cache = cache;
        _cacheOptions = cacheOptions;
    }

    public async Task<IReadOnlyList<QueueSnapshot>> GetQueuesAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && _cacheOptions.Enabled)
        {
            var cached = await _cache.GetAsync<List<QueueSnapshot>>(CacheKeyAll, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }
        }

        var queues = await _provider.GetQueuesAsync(ct).ConfigureAwait(false);

        if (_cacheOptions.Enabled)
        {
            await _cache.SetAsync(CacheKeyAll, queues.ToList(), TimeSpan.FromSeconds(_cacheOptions.TtlSeconds), ct)
                .ConfigureAwait(false);
        }

        return queues;
    }
}

/// <summary>Cache configuration for QueueQueryService.</summary>
public sealed class QueueCacheOptions
{
    public bool Enabled { get; init; } = true;
    public int TtlSeconds { get; init; } = 180;
}
