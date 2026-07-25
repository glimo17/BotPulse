using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Abstractions.Providers;

/// <summary>Provides read and management access to job entities from an RPA vendor.</summary>
public interface IJobProvider
{
    Task<IReadOnlyList<JobSnapshot>> GetJobsAsync(JobQuery query, CancellationToken ct = default);
    Task<JobSnapshot?> GetJobByIdAsync(string externalId, CancellationToken ct = default);
    Task<StartJobResult> StartJobAsync(StartJobRequest request, CancellationToken ct = default);
    Task StopJobAsync(string externalId, CancellationToken ct = default);
    Task CancelJobAsync(string externalId, CancellationToken ct = default);
}
