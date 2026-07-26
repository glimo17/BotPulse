using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace BotPulse.Core.Application.Alerts;

/// <summary>Purges old alerts beyond the retention period. Default: 12 months.</summary>
public sealed class AlertRetentionService
{
    private static readonly Action<ILogger, int, Exception?> LogPurged =
        LoggerMessage.Define<int>(LogLevel.Information, new EventId(1, "AlertsPurged"),
            "Purged {Count} expired alert records");

    private readonly IAlertRepository _alerts;
    private readonly ISystemClock _clock;
    private readonly ILogger<AlertRetentionService> _logger;
    private readonly int _retentionMonths;

    public AlertRetentionService(
        IAlertRepository alerts,
        ISystemClock clock,
        ILogger<AlertRetentionService> logger,
        int retentionMonths = 12)
    {
        _alerts = alerts;
        _clock = clock;
        _logger = logger;
        _retentionMonths = retentionMonths;
    }

    public async Task PurgeExpiredAsync(CancellationToken ct = default)
    {
        var cutoff = _clock.UtcNow.AddMonths(-_retentionMonths);
        var expired = await _alerts.FindAllAsync(a => a.RaisedAtUtc < cutoff, ct).ConfigureAwait(false);

        foreach (var alert in expired)
        {
            _alerts.Remove(alert);
        }

        LogPurged(_logger, expired.Count, null);
    }
}
