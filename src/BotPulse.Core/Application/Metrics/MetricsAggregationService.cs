using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Application.Metrics;

/// <summary>
/// Computes hourly and daily rollups from raw metric points.
/// Property invariant: sum(hourlyBuckets) == sum(rawPoints) for the same time range.
/// </summary>
public sealed class MetricsAggregationService
{
    private readonly IMetricsRepository _metrics;

    public MetricsAggregationService(IMetricsRepository metrics) => _metrics = metrics;

    public async Task AggregateHourlyAsync(
        string metricName,
        DateTime bucketStartUtc,
        CancellationToken ct = default)
    {
        var bucketEnd = bucketStartUtc.AddHours(1);
        var points = await _metrics.QueryRangeAsync(metricName, bucketStartUtc, bucketEnd, ct)
            .ConfigureAwait(false);

        if (points.Count == 0)
        {
            return;
        }

        var values = points.Select(p => p.Value).ToList();
        var rollup = MetricRollup.Create(
            bucketStart: bucketStartUtc,
            granularity: "Hourly",
            metricName: metricName,
            sum: values.Sum(),
            min: values.Min(),
            max: values.Max(),
            avg: values.Average(),
            count: values.Count);

        await _metrics.UpsertHourlyAsync(rollup, ct).ConfigureAwait(false);
    }

    public async Task AggregateDailyAsync(
        string metricName,
        DateTime bucketDateUtc,
        CancellationToken ct = default)
    {
        var bucketStart = bucketDateUtc.Date;
        var bucketEnd = bucketStart.AddDays(1);
        var points = await _metrics.QueryRangeAsync(metricName, bucketStart, bucketEnd, ct)
            .ConfigureAwait(false);

        if (points.Count == 0)
        {
            return;
        }

        var values = points.Select(p => p.Value).ToList();
        var rollup = MetricRollup.Create(
            bucketStart: bucketStart,
            granularity: "Daily",
            metricName: metricName,
            sum: values.Sum(),
            min: values.Min(),
            max: values.Max(),
            avg: values.Average(),
            count: values.Count);

        await _metrics.UpsertDailyAsync(rollup, ct).ConfigureAwait(false);
    }
}
