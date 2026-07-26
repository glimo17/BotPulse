using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Application.Metrics;

/// <summary>Queries persisted metric points and rollups.</summary>
public sealed class MetricsQueryService
{
    private readonly IMetricsRepository _metrics;

    public MetricsQueryService(IMetricsRepository metrics) => _metrics = metrics;

    public async Task<IReadOnlyList<MetricPoint>> GetRawMetricsAsync(
        string metricName,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default) =>
        await _metrics.QueryRangeAsync(metricName, fromUtc, toUtc, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<MetricRollup>> GetRollupsAsync(
        string metricName,
        string granularity,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct = default) =>
        await _metrics.QueryRollupsAsync(metricName, granularity, fromUtc, toUtc, ct).ConfigureAwait(false);
}
