using Microsoft.Extensions.Logging;

namespace BotPulse.Worker.Services;

/// <summary>
/// Base class for independent synchronization services.
/// Uses PeriodicTimer + SemaphoreSlim(1,1) for single-flight execution.
/// Clamps interval minimum to 30 seconds.
/// </summary>
public abstract partial class SynchronizationServiceBase : ISynchronizationService, IDisposable
{
    private const int MinIntervalSeconds = 30;

    private readonly SemaphoreSlim _runLock = new(1, 1);
    private readonly ILogger _logger;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    private DateTime? _lastRunUtc;
    private string _lastOutcome = "NotRun";
    private long _itemsProcessedLastRun;
    private bool _isRunning;
    private bool _isHealthy = true;
    private bool _disposed;

    protected SynchronizationServiceBase(ILogger logger) => _logger = logger;

    public abstract string Name { get; }
    public abstract SynchronizationOptions Options { get; }

    public SynchronizationServiceStatus CurrentStatus => new(
        Name,
        _isRunning,
        _lastRunUtc,
        _lastOutcome,
        _timer is not null ? _lastRunUtc?.AddSeconds(EffectiveInterval) : null,
        _itemsProcessedLastRun,
        _isHealthy);

    protected int EffectiveInterval => Math.Max(Options.IntervalSeconds, MinIntervalSeconds);

    public Task StartAsync(CancellationToken ct)
    {
        if (!Options.Enabled)
        {
            LogDisabled(_logger, Name);
            return Task.CompletedTask;
        }

        if (Options.IntervalSeconds < MinIntervalSeconds)
        {
            LogIntervalClamped(_logger, Name, Options.IntervalSeconds, MinIntervalSeconds);
        }

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = RunLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _cts?.Cancel();
        _timer?.Dispose();
        _timer = null;
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task RunOnceAsync(CancellationToken ct)
    {
        if (!await _runLock.WaitAsync(0, ct).ConfigureAwait(false))
        {
            LogManualTriggerSkipped(_logger, Name);
            return;
        }

        try
        {
            await ExecuteOnceAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _runLock.Release();
        }
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct) => Task.FromResult(_isHealthy);

    protected abstract Task<long> SyncAsync(CancellationToken ct);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _timer?.Dispose();
        _runLock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(EffectiveInterval));

        try
        {
            while (await _timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
            {
                if (!await _runLock.WaitAsync(0, ct).ConfigureAwait(false))
                {
                    LogTickSkipped(_logger, Name);
                    continue;
                }

                try
                {
                    await ExecuteOnceAsync(ct).ConfigureAwait(false);
                }
                finally
                {
                    _runLock.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
            LogStopped(_logger, Name);
        }
    }

    private async Task ExecuteOnceAsync(CancellationToken ct)
    {
        _isRunning = true;
        var started = DateTime.UtcNow;
        LogStarting(_logger, Name);

        try
        {
            _itemsProcessedLastRun = await SyncAsync(ct).ConfigureAwait(false);
            _lastOutcome = "Success";
            _isHealthy = true;
            LogCompleted(_logger, Name, (long)(DateTime.UtcNow - started).TotalMilliseconds, _itemsProcessedLastRun);
        }
        catch (OperationCanceledException)
        {
            _lastOutcome = "Cancelled";
            throw;
        }
        catch (Exception ex)
        {
            _lastOutcome = "Error";
            _isHealthy = false;
            LogFailed(_logger, Name, ex);
        }
        finally
        {
            _lastRunUtc = started;
            _isRunning = false;
        }
    }

    [LoggerMessage(EventId = 10, Level = LogLevel.Information,
        Message = "Sync service {Name} is disabled by configuration")]
    private static partial void LogDisabled(ILogger logger, string name);

    [LoggerMessage(EventId = 11, Level = LogLevel.Warning,
        Message = "Sync service {Name} interval {Interval}s is below minimum {Min}s — clamped")]
    private static partial void LogIntervalClamped(ILogger logger, string name, int interval, int min);

    [LoggerMessage(EventId = 12, Level = LogLevel.Debug,
        Message = "Sync service {Name} already running — skipping manual trigger")]
    private static partial void LogManualTriggerSkipped(ILogger logger, string name);

    [LoggerMessage(EventId = 13, Level = LogLevel.Debug,
        Message = "Sync service {Name} tick skipped — previous run still in progress")]
    private static partial void LogTickSkipped(ILogger logger, string name);

    [LoggerMessage(EventId = 14, Level = LogLevel.Information,
        Message = "Sync service {Name} stopped")]
    private static partial void LogStopped(ILogger logger, string name);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information,
        Message = "Sync service {Name} starting")]
    private static partial void LogStarting(ILogger logger, string name);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information,
        Message = "Sync service {Name} completed in {ElapsedMs}ms — {Items} items")]
    private static partial void LogCompleted(ILogger logger, string name, long elapsedMs, long items);

    [LoggerMessage(EventId = 17, Level = LogLevel.Error,
        Message = "Sync service {Name} failed")]
    private static partial void LogFailed(ILogger logger, string name, Exception ex);
}
