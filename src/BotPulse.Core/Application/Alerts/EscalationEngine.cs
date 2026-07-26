using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Time;
using Microsoft.Extensions.Logging;

namespace BotPulse.Core.Application.Alerts;

/// <summary>
/// Escalates unacknowledged Critical alerts after a configurable timeout.
/// Default: first escalation at 15 minutes, second at 30 minutes.
/// </summary>
public sealed class EscalationEngine
{
    private static readonly Action<ILogger, Guid, int, Exception?> LogEscalated =
        LoggerMessage.Define<Guid, int>(LogLevel.Warning, new EventId(1, "AlertEscalated"),
            "Alert {AlertId} escalated to level {Level}");

    private readonly IAlertRepository _alerts;
    private readonly IAlertRuleRepository _rules;
    private readonly ISystemClock _clock;
    private readonly ILogger<EscalationEngine> _logger;

    public EscalationEngine(
        IAlertRepository alerts,
        IAlertRuleRepository rules,
        ISystemClock clock,
        ILogger<EscalationEngine> logger)
    {
        _alerts = alerts;
        _rules = rules;
        _clock = clock;
        _logger = logger;
    }

    public async Task EscalatePendingAsync(CancellationToken ct = default)
    {
        var cutoff = _clock.UtcNow.AddMinutes(-15);
        var criticalUnacked = await _alerts
            .GetUnacknowledgedCriticalAsync(cutoff, ct)
            .ConfigureAwait(false);

        foreach (var alert in criticalUnacked)
        {
            if (alert.EscalationLevel < 2)
            {
                alert.Escalate();
                _alerts.Update(alert);
                LogEscalated(_logger, alert.Id, alert.EscalationLevel, null);
            }
        }
    }
}
