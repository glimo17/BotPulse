namespace BotPulse.Worker;

/// <summary>Root configuration section for all synchronization service intervals.</summary>
public sealed class WorkerOptions
{
    public SynchronizationServiceConfig JobSync { get; init; } = new();
    public SynchronizationServiceConfig QueueItemSync { get; init; } = new();
    public SynchronizationServiceConfig LogSync { get; init; } = new() { IntervalSeconds = 60 };
    public SynchronizationServiceConfig MetricsCollection { get; init; } = new() { IntervalSeconds = 300 };
}

/// <summary>Per-service configuration bound from appsettings.</summary>
public sealed class SynchronizationServiceConfig
{
    public bool Enabled { get; init; } = true;
    public int IntervalSeconds { get; init; } = 120;
    public int BatchSize { get; init; } = 500;
}
