using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Domain.Entities;
using BotPulse.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IJobRepository.</summary>
internal sealed class JobRepository : GenericRepository<Job>, IJobRepository
{
    public JobRepository(BotPulseDbContext context) : base(context) { }

    public async Task<Job?> GetByExternalIdAsync(string providerName, string externalId, CancellationToken ct = default) =>
        await Context.Jobs
            .FirstOrDefaultAsync(j => j.ProviderName == providerName && j.ExternalJobId == externalId, ct)
            .ConfigureAwait(false);

    public async Task<DateTime?> GetMaxUpdatedAtAsync(string providerName, CancellationToken ct = default) =>
        await Context.Jobs
            .Where(j => j.ProviderName == providerName)
            .MaxAsync(j => (DateTime?)j.UpdatedAtUtc, ct)
            .ConfigureAwait(false);

    public async Task<(IReadOnlyList<Job> Items, int TotalCount)> QueryAsync(JobFilter filter, CancellationToken ct = default)
    {
        var query = Context.Jobs.AsQueryable();

        if (!string.IsNullOrEmpty(filter.ProviderName))
        {
            query = query.Where(j => j.ProviderName == filter.ProviderName);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(j => j.StartTimeUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(j => j.StartTimeUtc <= filter.ToUtc.Value);
        }

        if (!string.IsNullOrEmpty(filter.RobotExternalId))
        {
            query = query.Where(j => j.RobotExternalId == filter.RobotExternalId);
        }

        if (!string.IsNullOrEmpty(filter.ProcessExternalId))
        {
            query = query.Where(j => j.ProcessExternalId == filter.ProcessExternalId);
        }

        if (!string.IsNullOrEmpty(filter.Status))
        {
            query = query.Where(j => j.Status == JobStatus.Parse(filter.Status));
        }

        if (!string.IsNullOrEmpty(filter.ErrorType))
        {
            query = query.Where(j => j.ErrorType == filter.ErrorType);
        }

        var total = await query.CountAsync(ct).ConfigureAwait(false);

        query = (filter.SortBy?.ToUpperInvariant()) switch
        {
            "DURATION" when filter.SortDescending => query.OrderByDescending(j => j.Duration),
            "DURATION" => query.OrderBy(j => j.Duration),
            _ when filter.SortDescending => query.OrderByDescending(j => j.StartTimeUtc),
            _ => query.OrderBy(j => j.StartTimeUtc),
        };

        var skip = (filter.Page - 1) * filter.PageSize;
        var items = await query.Skip(skip).Take(filter.PageSize).ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }

    public async Task UpsertAsync(JobSnapshot snapshot, string providerName, CancellationToken ct = default)
    {
        var existing = await GetByExternalIdAsync(providerName, snapshot.ExternalId, ct).ConfigureAwait(false);
        if (existing is null)
        {
            var newJob = Job.FromSnapshot(snapshot, providerName);
            await AddAsync(newJob, ct).ConfigureAwait(false);
        }
        else
        {
            existing.UpdateFromSnapshot(snapshot);
            Update(existing);
        }
    }
}
