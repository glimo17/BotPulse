using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Application.Metrics;
using BotPulse.Core.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Worker.Services;

/// <summary>
/// Collects operational metrics from persisted data and stores raw + aggregated rollups.
/// Computes: jobs total/success/failed, queue backlog, robot/machine availability.
/// Default interval: 300s.
/// </summary>
public sealed class MetricsCollectionService : SynchronizationServiceBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SynchronizationOptions> _optionsMonitor;

    public MetricsCollectionService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SynchronizationOptions> optionsMonitor,
        ILogger<MetricsCollectionService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    public override string Name => "MetricsCollection";
    public override SynchronizationOptions Options =>
        _optionsMonitor.Get("MetricsCollection");

    protected override async Task<long> SyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var metricsRepo = scope.ServiceProvider.GetRequiredService<IMetricsRepository>();
        var jobRepo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var queueItemRepo = scope.ServiceProvider.GetRequiredService<IQueueItemRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var aggregation = scope.ServiceProvider.GetRequiredService<MetricsAggregationService>();

        var now = DateTime.UtcNow;
        var windowStart = now.AddSeconds(-Options.IntervalSeconds);

        // Jobs metrics
        var (jobs, _) = await jobRepo.QueryAsync(
            new Core.Abstractions.Persistence.JobFilter(FromUtc: windowStart, ToUtc: now),
            ct).ConfigureAwait(false);

        var metrics = new List<(string name, double value)>
        {
            ("jobs.total", jobs.Count),
            ("jobs.success", jobs.Count(j => j.Status.Value == "Success")),
            ("jobs.failed", jobs.Count(j => j.Status.Value == "Failed")),
            ("jobs.stopped", jobs.Count(j => j.Status.Value is "Stopped" or "Cancelled")),
        };

        var avgDuration = jobs
            .Where(j => j.Duration.HasValue)
            .Select(j => j.Duration!.Value.TotalSeconds)
            .DefaultIfEmpty(0)
            .Average();
        metrics.Add(("jobs.avg_duration_seconds", avgDuration));

        var successCount = jobs.Count(j => j.Status.Value == "Success");
        var successRate = jobs.Count > 0 ? (double)successCount / jobs.Count * 100 : 0;
        metrics.Add(("jobs.success_rate", successRate));

        // Queue backlog
        var queueItems = await queueItemRepo.FindAllAsync(
            q => q.Status == "New" || q.Status == "InProgress", ct).ConfigureAwait(false);
        metrics.Add(("queue.backlog", queueItems.Count));

        // Persist raw metrics
        foreach (var (name, value) in metrics)
        {
            var point = MetricPoint.Create(name, value, "UiPath");
            await metricsRepo.AddRawAsync(point, ct).ConfigureAwait(false);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);

        // Compute hourly rollup for the current hour bucket
        var hourBucket = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        foreach (var (name, _) in metrics)
        {
            await aggregation.AggregateHourlyAsync(name, hourBucket, ct).ConfigureAwait(false);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return metrics.Count;
    }
}
