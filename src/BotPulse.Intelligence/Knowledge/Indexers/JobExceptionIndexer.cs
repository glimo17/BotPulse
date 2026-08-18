using System.Globalization;
using BotPulse.Core.Domain.Entities;
using BotPulse.Intelligence.Contracts.Knowledge;

namespace BotPulse.Intelligence.Knowledge.Indexers;

/// <summary>
/// Builds a KnowledgeItem from a failed job's error information (Requisito 4.1).
/// The Id is deterministic (based on the job's database Id), so re-indexing
/// the same job failure is idempotent.
/// </summary>
public static class JobExceptionIndexer
{
    /// <summary>
    /// Returns a KnowledgeItem for the job's failure, or null if the job has
    /// no error information to index (e.g. it did not fail).
    /// </summary>
    public static KnowledgeItem? ToKnowledgeItem(Job job, Guid? organizationId = null)
    {
        if (string.IsNullOrWhiteSpace(job.ErrorMessage))
        {
            return null;
        }

        var content = string.IsNullOrWhiteSpace(job.ErrorType)
            ? job.ErrorMessage
            : $"{job.ErrorType}: {job.ErrorMessage}";

        var metadata = new Dictionary<string, string>
        {
            ["jobId"] = job.Id.ToString(CultureInfo.InvariantCulture),
            ["externalJobId"] = job.ExternalJobId,
            ["processExternalId"] = job.ProcessExternalId,
            ["robotExternalId"] = job.RobotExternalId,
            ["providerName"] = job.ProviderName
        };

        if (job.ErrorType is not null)
        {
            metadata["errorType"] = job.ErrorType;
        }

        return new KnowledgeItem(
            Id: $"job-exception-{job.Id}",
            SourceType: KnowledgeSourceType.Exception,
            Content: content,
            Metadata: metadata,
            OrganizationId: organizationId);
    }
}
