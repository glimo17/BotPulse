namespace BotPulse.Core.Abstractions.Alerts;

/// <summary>Evaluates a specific type of alert rule against current operational data.</summary>
public interface IAlertRuleEvaluator
{
    /// <summary>Identifies the rule type this evaluator handles (e.g. "RobotOffline").</summary>
    string RuleType { get; }

    /// <summary>
    /// Evaluates the rule and returns candidate alerts. Empty list means no condition met.
    /// </summary>
    Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRuleContext rule, CancellationToken ct = default);
}

/// <summary>Context passed to a rule evaluator.</summary>
public sealed record AlertRuleContext(
    Guid RuleId,
    string RuleName,
    string Severity,
    string ParametersJson);

/// <summary>A potential alert identified by an evaluator, subject to deduplication.</summary>
public sealed record AlertCandidate(
    string AffectedResourceType,
    string AffectedResourceId,
    string ConditionDescription);
