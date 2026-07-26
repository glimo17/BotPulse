using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Application.Jobs;

/// <summary>Query service for persisted jobs with filtering and pagination.</summary>
public sealed class JobQueryService
{
    private readonly IJobRepository _jobs;

    public JobQueryService(IJobRepository jobs) => _jobs = jobs;

    public async Task<(IReadOnlyList<Job> Items, int TotalCount)> GetJobsAsync(
        JobFilter filter, CancellationToken ct = default) =>
        await _jobs.QueryAsync(filter, ct).ConfigureAwait(false);

    public async Task<Job?> GetJobByExternalIdAsync(
        string providerName, string externalId, CancellationToken ct = default) =>
        await _jobs.GetByExternalIdAsync(providerName, externalId, ct).ConfigureAwait(false);
}
