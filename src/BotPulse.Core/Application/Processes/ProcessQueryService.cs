using BotPulse.Core.Abstractions.Caching;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Application.Processes;

/// <summary>
/// Query service for processes (releases). Reads on-demand from the RPA provider.
/// Processes are NOT persisted locally.
/// </summary>
public sealed class ProcessQueryService
{
    private const string CacheKeyAll = "processes.all";
    private const string CacheKeyParamsPrefix = "processes.params.";
    private readonly IProcessProvider _provider;
    private readonly ICacheService _cache;
    private readonly ProcessCacheOptions _cacheOptions;

    public ProcessQueryService(IProcessProvider provider, ICacheService cache, ProcessCacheOptions cacheOptions)
    {
        _provider = provider;
        _cache = cache;
        _cacheOptions = cacheOptions;
    }

    public async Task<IReadOnlyList<ProcessSnapshot>> GetProcessesAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && _cacheOptions.Enabled)
        {
            var cached = await _cache.GetAsync<List<ProcessSnapshot>>(CacheKeyAll, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }
        }

        var processes = await _provider.GetProcessesAsync(ct).ConfigureAwait(false);

        if (_cacheOptions.Enabled)
        {
            await _cache.SetAsync(CacheKeyAll, processes.ToList(), TimeSpan.FromSeconds(_cacheOptions.TtlSeconds), ct)
                .ConfigureAwait(false);
        }

        return processes;
    }

    public async Task<IReadOnlyList<ProcessParameter>> GetProcessParametersAsync(string processExternalId, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyParamsPrefix}{processExternalId}";

        if (_cacheOptions.Enabled)
        {
            var cached = await _cache.GetAsync<List<ProcessParameter>>(cacheKey, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }
        }

        var parameters = await _provider.GetProcessParametersAsync(processExternalId, ct).ConfigureAwait(false);

        if (_cacheOptions.Enabled)
        {
            await _cache.SetAsync(cacheKey, parameters.ToList(), TimeSpan.FromSeconds(_cacheOptions.TtlSeconds), ct)
                .ConfigureAwait(false);
        }

        return parameters;
    }
}

/// <summary>Cache configuration for ProcessQueryService.</summary>
public sealed class ProcessCacheOptions
{
    public bool Enabled { get; init; } = true;
    public int TtlSeconds { get; init; } = 600;
}
