namespace BotPulse.Worker.Services;

/// <summary>Contract for all independent synchronization services.</summary>
public interface ISynchronizationService
{
    string Name { get; }
    SynchronizationOptions Options { get; }
    SynchronizationServiceStatus CurrentStatus { get; }

    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task RunOnceAsync(CancellationToken ct);
    Task<bool> IsHealthyAsync(CancellationToken ct);
}

/// <summary>Configuration for a synchronization service.</summary>
public sealed class SynchronizationOptions
{
    public bool Enabled { get; init; } = true;
    public int IntervalSeconds { get; init; } = 120;
    public int BatchSize { get; init; } = 500;
}

/// <summary>Runtime status snapshot for a synchronization service.</summary>
public sealed record SynchronizationServiceStatus(
    string Name,
    bool IsRunning,
    DateTime? LastRunUtc,
    string LastOutcome,
    DateTime? NextRunUtc,
    long ItemsProcessedLastRun,
    bool IsHealthy);
