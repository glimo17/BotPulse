using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Exceptions;

namespace BotPulse.Core.Application.Alerts;

/// <summary>Acknowledges alerts and records the action in the audit log.</summary>
public sealed class AlertAcknowledgmentService
{
    private readonly IAlertRepository _alerts;
    private readonly IAuditRepository _audit;

    public AlertAcknowledgmentService(IAlertRepository alerts, IAuditRepository audit)
    {
        _alerts = alerts;
        _audit = audit;
    }

    public async Task AcknowledgeAsync(
        Guid alertId, string userId, string userName,
        string correlationId, CancellationToken ct = default)
    {
        var alert = await _alerts.FindAsync(a => a.Id == alertId, ct).ConfigureAwait(false)
            ?? throw new EntityNotFoundException("Alert", alertId);

        await _alerts.AcknowledgeAsync(alertId, userId, ct).ConfigureAwait(false);

        await _audit.RecordAsync(new AuditRecordData(
            UserId: userId, UserName: userName,
            Action: "AcknowledgeAlert", ResourceType: "Alert",
            ResourceId: alertId.ToString(), Outcome: "Success",
            IpAddress: null, CorrelationId: correlationId), ct)
            .ConfigureAwait(false);
    }
}
