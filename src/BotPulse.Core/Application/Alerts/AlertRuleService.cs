using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using BotPulse.Core.Exceptions;

namespace BotPulse.Core.Application.Alerts;

/// <summary>CRUD service for alert rule management. All mutations are audit-logged.</summary>
public sealed class AlertRuleService
{
    private readonly IAlertRuleRepository _rules;
    private readonly IAuditRepository _audit;

    public AlertRuleService(IAlertRuleRepository rules, IAuditRepository audit)
    {
        _rules = rules;
        _audit = audit;
    }

    public async Task<IReadOnlyList<AlertRule>> GetEnabledRulesAsync(CancellationToken ct = default) =>
        await _rules.GetEnabledAsync(ct).ConfigureAwait(false);

    public async Task<AlertRule> CreateAsync(
        string name, string ruleType, string severity,
        string parametersJson, string channelsJson,
        string userId, string userName, string correlationId,
        CancellationToken ct = default)
    {
        var rule = AlertRule.Configure(name, ruleType, severity, parametersJson, channelsJson);
        await _rules.AddAsync(rule, ct).ConfigureAwait(false);

        await _audit.RecordAsync(new AuditRecordData(
            UserId: userId, UserName: userName,
            Action: "CreateAlertRule", ResourceType: "AlertRule",
            ResourceId: rule.Id.ToString(), Outcome: "Success",
            IpAddress: null, CorrelationId: correlationId), ct)
            .ConfigureAwait(false);

        return rule;
    }

    public async Task EnableAsync(Guid ruleId, string userId, string userName, string correlationId, CancellationToken ct = default)
    {
        var rule = await _rules.FindAsync(r => r.Id == ruleId, ct).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("AlertRule", ruleId);
        rule.Enable();
        _rules.Update(rule);

        await _audit.RecordAsync(new AuditRecordData(
            UserId: userId, UserName: userName,
            Action: "EnableAlertRule", ResourceType: "AlertRule",
            ResourceId: ruleId.ToString(), Outcome: "Success",
            IpAddress: null, CorrelationId: correlationId), ct)
            .ConfigureAwait(false);
    }

    public async Task DisableAsync(Guid ruleId, string userId, string userName, string correlationId, CancellationToken ct = default)
    {
        var rule = await _rules.FindAsync(r => r.Id == ruleId, ct).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("AlertRule", ruleId);
        rule.Disable();
        _rules.Update(rule);

        await _audit.RecordAsync(new AuditRecordData(
            UserId: userId, UserName: userName,
            Action: "DisableAlertRule", ResourceType: "AlertRule",
            ResourceId: ruleId.ToString(), Outcome: "Success",
            IpAddress: null, CorrelationId: correlationId), ct)
            .ConfigureAwait(false);
    }
}
