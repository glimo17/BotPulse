using BotPulse.Core.Abstractions.Persistence;

namespace BotPulse.Core.Domain.Entities;

/// <summary>Immutable audit record. Created only via the For factory method.</summary>
public sealed class AuditRecord
{
    private AuditRecord() { }

    public long Id { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string? ResourceId { get; private set; }
    public string Outcome { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public string? DetailsJson { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;

    public static AuditRecord For(AuditRecordData data)
    {
        return new AuditRecord
        {
            TimestampUtc = data.TimestampUtc ?? DateTime.UtcNow,
            UserId = data.UserId,
            UserName = data.UserName,
            Action = data.Action,
            ResourceType = data.ResourceType,
            ResourceId = data.ResourceId,
            Outcome = data.Outcome,
            IpAddress = data.IpAddress,
            DetailsJson = data.DetailsJson,
            CorrelationId = data.CorrelationId,
        };
    }
}
