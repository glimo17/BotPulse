using BotPulse.Core.Abstractions.Caching;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Application.Machines;

/// <summary>
/// Query service for machines. Reads on-demand from the RPA provider with optional short-lived cache.
/// Machines are NOT persisted locally.
/// </summary>
public sealed class MachineQueryService
{
    private const string CacheKeyAll = "machines.all";
    private const string CacheKeyPrefix = "machines.";
    private readonly IMachineProvider _provider;
    private readonly ICacheService _cache;
    private readonly MachineCacheOptions _cacheOptions;

    public MachineQueryService(IMachineProvider provider, ICacheService cache, MachineCacheOptions cacheOptions)
    {
        _provider = provider;
        _cache = cache;
        _cacheOptions = cacheOptions;
    }

    public async Task<IReadOnlyList<MachineSnapshot>> GetMachinesAsync(bool forceRefresh = false, CancellationToken ct = default)
    {
        if (!forceRefresh && _cacheOptions.Enabled)
        {
            var cached = await _cache.GetAsync<List<MachineSnapshot>>(CacheKeyAll, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }
        }

        var machines = await _provider.GetMachinesAsync(ct).ConfigureAwait(false);

        if (_cacheOptions.Enabled)
        {
            await _cache.SetAsync(CacheKeyAll, machines.ToList(), TimeSpan.FromSeconds(_cacheOptions.TtlSeconds), ct)
                .ConfigureAwait(false);
        }

        return machines;
    }

    public async Task<MachineSnapshot?> GetMachineByIdAsync(string externalId, CancellationToken ct = default)
    {
        var cacheKey = $"{CacheKeyPrefix}{externalId}";

        if (_cacheOptions.Enabled)
        {
            var cached = await _cache.GetAsync<MachineSnapshot>(cacheKey, ct).ConfigureAwait(false);
            if (cached is not null)
            {
                return cached;
            }
        }

        var machine = await _provider.GetMachineByIdAsync(externalId, ct).ConfigureAwait(false);

        if (machine is not null && _cacheOptions.Enabled)
        {
            await _cache.SetAsync(cacheKey, machine, TimeSpan.FromSeconds(_cacheOptions.TtlSeconds), ct)
                .ConfigureAwait(false);
        }

        return machine;
    }
}

/// <summary>Cache configuration for MachineQueryService.</summary>
public sealed class MachineCacheOptions
{
    public bool Enabled { get; init; } = true;
    public int TtlSeconds { get; init; } = 300;
}
