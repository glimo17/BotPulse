using System.Globalization;
using BotPulse.Core.Domain.Entities;
using BotPulse.Intelligence.Contracts.Knowledge;

namespace BotPulse.Intelligence.Knowledge.Indexers;

/// <summary>
/// Builds a KnowledgeItem from an execution log entry for indexing into the
/// knowledge base (Requisito 4.1). The Id is deterministic (based on the log's
/// database Id), so re-indexing the same log is idempotent.
/// </summary>
public static class ExecutionLogIndexer
{
    public static KnowledgeItem ToKnowledgeItem(ExecutionLog log, Guid? organizationId = null)
    {
        var content = $"[{log.Severity}] {log.LoggerName}: {log.Message}";

        var metadata = new Dictionary<string, string>
        {
            ["logId"] = log.Id.ToString(CultureInfo.InvariantCulture),
            ["severity"] = log.Severity,
            ["providerName"] = log.ProviderName
        };

        if (log.JobExternalId is not null)
        {
            metadata["jobExternalId"] = log.JobExternalId;
        }

        if (log.RobotExternalId is not null)
        {
            metadata["robotExternalId"] = log.RobotExternalId;
        }

        return new KnowledgeItem(
            Id: $"log-{log.Id}",
            SourceType: KnowledgeSourceType.ExecutionLog,
            Content: content,
            Metadata: metadata,
            OrganizationId: organizationId);
    }
}
