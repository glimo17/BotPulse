using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BotPulse.Worker.Services;

/// <summary>
/// Coordinates the lifecycle of all independent synchronization services.
/// Registered as IHostedService — starts/stops all enabled services.
/// Provides status aggregation for health checks and admin API.
/// </summary>
public sealed class SynchronizationOrchestrator : IHostedService
{
    private static readonly Action<ILogger, string, Exception?> LogServiceStarted =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "ServiceStarted"),
            "Sync service {Name} started");

    private static readonly Action<ILogger, string, Exception?> LogServiceDisabled =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(2, "ServiceDisabled"),
            "Sync service {Name} is disabled — skipping");

    private readonly IReadOnlyList<ISynchronizationService> _services;
    private readonly ILogger<SynchronizationOrchestrator> _logger;

    public SynchronizationOrchestrator(
        IEnumerable<ISynchronizationService> services,
        ILogger<SynchronizationOrchestrator> logger)
    {
        _services = services.ToList();
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var service in _services)
        {
            if (!service.Options.Enabled)
            {
                LogServiceDisabled(_logger, service.Name, null);
                continue;
            }

            await service.StartAsync(cancellationToken).ConfigureAwait(false);
            LogServiceStarted(_logger, service.Name, null);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var tasks = _services.Select(s => s.StopAsync(cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>Returns per-service status for health checks and admin endpoints.</summary>
    public IReadOnlyList<SynchronizationServiceStatus> GetStatuses() =>
        _services.Select(s => s.CurrentStatus).ToList();

    /// <summary>Triggers a manual run of a specific service by name.</summary>
    public async Task TriggerAsync(string serviceName, CancellationToken ct = default)
    {
        var service = _services.FirstOrDefault(s =>
            string.Equals(s.Name, serviceName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Sync service '{serviceName}' not found.");

        await service.RunOnceAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Returns true if all enabled services are healthy.</summary>
    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        foreach (var service in _services.Where(s => s.Options.Enabled))
        {
            if (!await service.IsHealthyAsync(ct).ConfigureAwait(false))
            {
                return false;
            }
        }

        return true;
    }
}
