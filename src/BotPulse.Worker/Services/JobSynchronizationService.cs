using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Worker.Services;

/// <summary>
/// Synchronizes jobs from the RPA provider to local PostgreSQL.
/// Uses incremental sync (UpdatedSinceUtc watermark) to minimize API calls.
/// Terminal jobs (Success/Failed/Stopped/Cancelled) are never updated again.
/// Default interval: 120s.
/// </summary>
public sealed class JobSynchronizationService : SynchronizationServiceBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SynchronizationOptions> _optionsMonitor;

    public JobSynchronizationService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SynchronizationOptions> optionsMonitor,
        ILogger<JobSynchronizationService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    public override string Name => "JobSync";
    public override SynchronizationOptions Options =>
        _optionsMonitor.Get("JobSync");

    protected override async Task<long> SyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IJobProvider>();
        var repo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var since = await repo.GetMaxUpdatedAtAsync("UiPath", ct).ConfigureAwait(false);

        var snapshots = await provider.GetJobsAsync(
            new Core.Abstractions.Providers.Models.JobQuery(
                UpdatedSinceUtc: since,
                Top: Options.BatchSize),
            ct).ConfigureAwait(false);

        foreach (var snapshot in snapshots)
        {
            await repo.UpsertAsync(snapshot, "UiPath", ct).ConfigureAwait(false);
        }

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        return snapshots.Count;
    }
}
