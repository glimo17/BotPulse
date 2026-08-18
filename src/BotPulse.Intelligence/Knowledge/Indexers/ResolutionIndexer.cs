using BotPulse.Intelligence.Contracts.Diagnostics;
using BotPulse.Intelligence.Contracts.Knowledge;

namespace BotPulse.Intelligence.Knowledge.Indexers;

/// <summary>
/// Builds a KnowledgeItem from an operator-validated diagnosis (Requisito 4.1,
/// 5.5 feedback loop). Only diagnoses explicitly validated by an operator are
/// indexed as trusted knowledge — rejected diagnoses are never re-fed into
/// the knowledge base. The Id is deterministic per job, so re-validating the
/// same job's diagnosis is idempotent.
/// </summary>
public static class ResolutionIndexer
{
    public static KnowledgeItem ToKnowledgeItem(
        string jobId, DiagnosisResult diagnosis, Guid? organizationId = null)
    {
        var content = $"Root cause: {diagnosis.RootCause}. " +
                      $"Impact: {diagnosis.Impact}. " +
                      $"Resolution: {string.Join("; ", diagnosis.ResolutionSteps)}";

        var metadata = new Dictionary<string, string>
        {
            ["jobId"] = jobId,
            ["validated"] = "true"
        };

        return new KnowledgeItem(
            Id: $"resolution-{jobId}",
            SourceType: KnowledgeSourceType.Resolution,
            Content: content,
            Metadata: metadata,
            OrganizationId: organizationId);
    }
}
