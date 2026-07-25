namespace BotPulse.Core.Abstractions.Persistence;

/// <summary>
/// Append-only repository for audit records.
/// No Update or Delete operations are exposed by design.
/// </summary>
public interface IAuditRepository
{
    Task RecordAsync(AuditRecordData record, CancellationToken ct = default);
    Task<IReadOnlyList<AuditRecordData>> QueryAsync(AuditQuery query, CancellationToken ct = default);
}

/// <summary>Data for a single audit record.</summary>
public sealed record AuditRecordData(
    string UserId,
    string UserName,
    string Action,
    string ResourceType,
    string? ResourceId,
    string Outcome,
    string? IpAddress,
    string CorrelationId,
    string? DetailsJson = null,
    DateTime? TimestampUtc = null);

/// <summary>Filter criteria for reading audit records.</summary>
public sealed record AuditQuery(
    string? UserId = null,
    string? Action = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Top = 100);
