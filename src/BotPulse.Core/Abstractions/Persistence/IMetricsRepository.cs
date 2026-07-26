using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Abstractions.Persistence;

/// <summary>Repository for raw metric points and aggregated rollups.</summary>
public interface IMetricsRepository
{
    Task AddRawAsync(MetricPoint point, CancellationToken ct = default);
    Task<IReadOnlyList<MetricPoint>> QueryRangeAsync(string metricName, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
    Task UpsertHourlyAsync(MetricRollup rollup, CancellationToken ct = default);
    Task UpsertDailyAsync(MetricRollup rollup, CancellationToken ct = default);
    Task<IReadOnlyList<MetricRollup>> QueryRollupsAsync(string metricName, string granularity, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}
