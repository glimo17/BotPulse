using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

internal sealed class MetricsRepository : GenericRepository<MetricPoint>, IMetricsRepository
{
    private readonly BotPulseDbContext _ctx;

    public MetricsRepository(BotPulseDbContext context) : base(context)
    {
        _ctx = context;
    }

    public async Task AddRawAsync(MetricPoint point, CancellationToken ct = default) =>
        await AddAsync(point, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<MetricPoint>> QueryRangeAsync(
        string metricName, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        await _ctx.MetricsRaw
            .Where(m => m.MetricName == metricName && m.TimestampUtc >= fromUtc && m.TimestampUtc <= toUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task UpsertHourlyAsync(MetricRollup rollup, CancellationToken ct = default)
    {
        var existing = await _ctx.MetricsRollups
            .FirstOrDefaultAsync(r => r.MetricName == rollup.MetricName
                && r.BucketStartUtc == rollup.BucketStartUtc
                && r.Granularity == "Hourly", ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            await _ctx.MetricsRollups.AddAsync(rollup, ct).ConfigureAwait(false);
        }
        else
        {
            _ctx.MetricsRollups.Update(rollup);
        }
    }

    public async Task UpsertDailyAsync(MetricRollup rollup, CancellationToken ct = default)
    {
        var existing = await _ctx.MetricsRollups
            .FirstOrDefaultAsync(r => r.MetricName == rollup.MetricName
                && r.BucketStartUtc == rollup.BucketStartUtc
                && r.Granularity == "Daily", ct)
            .ConfigureAwait(false);

        if (existing is null)
        {
            await _ctx.MetricsRollups.AddAsync(rollup, ct).ConfigureAwait(false);
        }
        else
        {
            _ctx.MetricsRollups.Update(rollup);
        }
    }

    public async Task<IReadOnlyList<MetricRollup>> QueryRollupsAsync(
        string metricName, string granularity, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        await _ctx.MetricsRollups
            .Where(r => r.MetricName == metricName && r.Granularity == granularity
                && r.BucketStartUtc >= fromUtc && r.BucketStartUtc <= toUtc)
            .OrderBy(r => r.BucketStartUtc)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}
