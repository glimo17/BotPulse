namespace BotPulse.Core.Abstractions.Alerts;

/// <summary>
/// Represents an alert delivery channel (Log, Email, Slack, Teams, Webhook).
/// New channels are added by implementing this interface without modifying the Alert Engine.
/// </summary>
public interface IAlertChannel
{
    /// <summary>Unique name identifying this channel (e.g. "Log", "Email", "Slack").</summary>
    string Name { get; }

    /// <summary>Delivers an alert through this channel.</summary>
    Task DeliverAsync(AlertDelivery delivery, CancellationToken ct = default);

    /// <summary>Checks whether this channel is operational.</summary>
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}

/// <summary>Payload passed to an alert channel for delivery.</summary>
public sealed record AlertDelivery(
    Guid AlertId,
    Guid RuleId,
    string RuleName,
    string Severity,
    string ConditionDescription,
    string AffectedResourceType,
    string AffectedResourceId,
    DateTime RaisedAtUtc,
    int EscalationLevel);
