using BotPulse.Core.Abstractions.Alerts;
using BotPulse.Core.Abstractions.Notifications;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Time;
using BotPulse.Core.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace BotPulse.Core.Application.Alerts;

/// <summary>
/// Evaluates all enabled alert rules, applies deduplication,
/// persists generated alerts, and dispatches notification events.
/// </summary>
public sealed class AlertEngine
{
    private static readonly Action<ILogger, string, Exception?> LogRuleEvaluated =
        LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "RuleEvaluated"),
            "Alert rule '{RuleType}' evaluated");

    private static readonly Action<ILogger, string, string, Exception?> LogAlertRaised =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(2, "AlertRaised"),
            "Alert raised: [{Severity}] {Description}");

    private static readonly Action<ILogger, string, Exception?> LogRuleEvaluationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(3, "RuleEvaluationFailed"),
            "Alert rule '{RuleType}' evaluation failed");

    private readonly IEnumerable<IAlertRuleEvaluator> _evaluators;
    private readonly IAlertRuleRepository _rules;
    private readonly IAlertRepository _alerts;
    private readonly IAlertDeduplicator _dedup;
    private readonly INotificationDelivery _notifications;
    private readonly IAuditRepository _audit;
    private readonly ISystemClock _clock;
    private readonly ILogger<AlertEngine> _logger;

    public AlertEngine(
        IEnumerable<IAlertRuleEvaluator> evaluators,
        IAlertRuleRepository rules,
        IAlertRepository alerts,
        IAlertDeduplicator dedup,
        INotificationDelivery notifications,
        IAuditRepository audit,
        ISystemClock clock,
        ILogger<AlertEngine> logger)
    {
        _evaluators = evaluators;
        _rules = rules;
        _alerts = alerts;
        _dedup = dedup;
        _notifications = notifications;
        _audit = audit;
        _clock = clock;
        _logger = logger;
    }

    public async Task EvaluateAllAsync(CancellationToken ct = default)
    {
        var enabledRules = await _rules.GetEnabledAsync(ct).ConfigureAwait(false);

        foreach (var rule in enabledRules)
        {
            var evaluator = _evaluators.FirstOrDefault(e =>
                string.Equals(e.RuleType, rule.RuleType, StringComparison.OrdinalIgnoreCase));

            if (evaluator is null)
            {
                continue;
            }

            try
            {
                var context = new AlertRuleContext(rule.Id, rule.Name, rule.Severity, rule.ParametersJson);
                var candidates = await evaluator.EvaluateAsync(context, ct).ConfigureAwait(false);

                LogRuleEvaluated(_logger, rule.RuleType, null);

                foreach (var candidate in candidates)
                {
                    if (!_dedup.ShouldEmit(context, candidate, _clock.UtcNow))
                    {
                        continue;
                    }

                    var alert = Alert.Raise(rule.Id, rule.Severity, candidate.ConditionDescription,
                        candidate.AffectedResourceType, candidate.AffectedResourceId);

                    await _alerts.AddAsync(alert, ct).ConfigureAwait(false);

                    LogAlertRaised(_logger, rule.Severity, candidate.ConditionDescription, null);

                    await _notifications.PublishAsync(new NotificationEvent(
                        "alert.raised", candidate.AffectedResourceType, candidate.AffectedResourceId,
                        $"{{\"alertId\":\"{alert.Id}\",\"severity\":\"{rule.Severity}\",\"description\":\"{candidate.ConditionDescription}\"}}",
                        _clock.UtcNow), ct).ConfigureAwait(false);

                    await _audit.RecordAsync(new AuditRecordData(
                        UserId: "system", UserName: "AlertEngine",
                        Action: "AlertRaised", ResourceType: candidate.AffectedResourceType,
                        ResourceId: candidate.AffectedResourceId,
                        Outcome: "Success", IpAddress: null,
                        CorrelationId: alert.Id.ToString()), ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogRuleEvaluationFailed(_logger, rule.RuleType, ex);
            }
        }
    }
}
