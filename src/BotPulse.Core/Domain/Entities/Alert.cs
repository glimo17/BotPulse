namespace BotPulse.Core.Domain.Entities;

/// <summary>Alert raised by the Alert Engine when a rule condition is met.</summary>
public sealed class Alert
{
    private Alert() { }

    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public string Severity { get; private set; } = string.Empty;
    public DateTime RaisedAtUtc { get; private set; }
    public string ConditionDescription { get; private set; } = string.Empty;
    public string AffectedResourceType { get; private set; } = string.Empty;
    public string AffectedResourceId { get; private set; } = string.Empty;
    public bool Acknowledged { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public int EscalationLevel { get; private set; }

    public static Alert Raise(Guid ruleId, string severity, string conditionDescription,
        string affectedResourceType, string affectedResourceId)
    {
        return new Alert
        {
            Id = Guid.NewGuid(),
            RuleId = ruleId,
            Severity = severity,
            RaisedAtUtc = DateTime.UtcNow,
            ConditionDescription = conditionDescription,
            AffectedResourceType = affectedResourceType,
            AffectedResourceId = affectedResourceId,
        };
    }

    public void Acknowledge(string userId)
    {
        Acknowledged = true;
        AcknowledgedBy = userId;
        AcknowledgedAtUtc = DateTime.UtcNow;
    }

    public void Escalate() => EscalationLevel++;
}

/// <summary>Alert rule configuration.</summary>
public sealed class AlertRule
{
    private AlertRule() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string RuleType { get; private set; } = string.Empty;
    public bool Enabled { get; private set; }
    public string Severity { get; private set; } = string.Empty;
    public string ParametersJson { get; private set; } = "{}";
    public string ChannelsJson { get; private set; } = "[]";
    public bool EscalationEnabled { get; private set; }
    public int EscalationTimeoutMinutes { get; private set; } = 15;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static AlertRule Configure(string name, string ruleType, string severity, string parametersJson = "{}", string channelsJson = "[\"Log\"]")
    {
        return new AlertRule
        {
            Id = Guid.NewGuid(),
            Name = name,
            RuleType = ruleType,
            Enabled = true,
            Severity = severity,
            ParametersJson = parametersJson,
            ChannelsJson = channelsJson,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Enable() { Enabled = true; UpdatedAtUtc = DateTime.UtcNow; }
    public void Disable() { Enabled = false; UpdatedAtUtc = DateTime.UtcNow; }
}
