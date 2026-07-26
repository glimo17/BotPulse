using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotPulse.Worker.Services;

/// <summary>
/// Synchronizes execution logs from the RPA provider to local PostgreSQL.
/// Uses batch inserts (default batch size 500).
/// Default interval: 60s.
/// </summary>
public sealed class LogSynchronizationService : SynchronizationServiceBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<SynchronizationOptions> _optionsMonitor;

    public LogSynchronizationService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<SynchronizationOptions> optionsMonitor,
        ILogger<LogSynchronizationService> logger) : base(logger)
    {
        _scopeFactory = scopeFactory;
        _optionsMonitor = optionsMonitor;
    }

    public override string Name => "LogSync";
    public override SynchronizationOptions Options =>
        _optionsMonitor.Get("LogSync");

    protected override async Task<long> SyncAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ILogProvider>();
        var repo = scope.ServiceProvider.GetRequiredService<ILogRepository>();

        var since = await repo.GetMaxTimestampAsync("UiPath", ct).ConfigureAwait(false);

        var snapshots = await provider.GetExecutionLogsAsync(
            new Core.Abstractions.Providers.Models.LogQuery(
                FromUtc: since,
                Top: Options.BatchSize),
            ct).ConfigureAwait(false);

        if (snapshots.Count > 0)
        {
            await repo.AddBatchAsync(snapshots, "UiPath", ct).ConfigureAwait(false);
        }

        return snapshots.Count;
    }
}
