using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Abstractions.Persistence;

/// <summary>Specialized repository for Job entities with query and sync support.</summary>
public interface IJobRepository : IRepository<Job>
{
    Task<Job?> GetByExternalIdAsync(string providerName, string externalId, CancellationToken ct = default);
    Task<DateTime?> GetMaxUpdatedAtAsync(string providerName, CancellationToken ct = default);
    Task<(IReadOnlyList<Job> Items, int TotalCount)> QueryAsync(JobFilter filter, CancellationToken ct = default);
    Task UpsertAsync(JobSnapshot snapshot, string providerName, CancellationToken ct = default);
}

/// <summary>Filter criteria for querying persisted jobs.</summary>
public sealed record JobFilter(
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? RobotExternalId = null,
    string? ProcessExternalId = null,
    string? MachineExternalId = null,
    string? Status = null,
    string? ErrorType = null,
    string? ProviderName = null,
    int Page = 1,
    int PageSize = 50,
    string? SortBy = null,
    bool SortDescending = true);
