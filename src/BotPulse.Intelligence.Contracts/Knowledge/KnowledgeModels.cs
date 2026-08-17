namespace BotPulse.Intelligence.Contracts.Knowledge;

/// <summary>Type of operational content indexed into the knowledge base.</summary>
public enum KnowledgeSourceType
{
    ExecutionLog,
    Exception,
    Alert,
    Runbook,
    HistoricalIncident,
    Resolution
}

/// <summary>
/// A unit of knowledge to index. The Id must be deterministic for a given source
/// item so re-indexing is idempotent (no duplicates).
/// </summary>
public sealed record KnowledgeItem(
    string Id,
    KnowledgeSourceType SourceType,
    string Content,
    IReadOnlyDictionary<string, string> Metadata,
    Guid? OrganizationId = null);
