using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Worker.Services;

/// <summary>
/// Synchronizes queue items from the RPA provider to local PostgreSQL.
/// Default interval: 180s.
/// </summary>
public sealed class QueueItemSynchronizationService : SynchronizationServiceBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SynchronizationOptions> _optionsMonitor;

    public QueueItemSynchronizationService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SynchronizationOptions> optionsMonitor,
        ILogger<QueueItemSynchronizationService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    public override string Name => "QueueItemSync";
    public override SynchronizationOptions Options =>
        _optionsMonitor.Get("QueueItemSync");

    protected override async Task<long> SyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IQueueProvider>();
        var repo = scope.ServiceProvider.GetRequiredService<IQueueItemRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var since = await repo.GetMaxUpdatedAtAsync("UiPath", ct).ConfigureAwait(false);

        var snapshots = await provider.GetQueueItemsAsync(
            new Core.Abstractions.Providers.Models.QueueItemQuery(
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
