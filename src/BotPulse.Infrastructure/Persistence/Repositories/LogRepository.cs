using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of ILogRepository with batch insert support.</summary>
internal sealed class LogRepository : GenericRepository<ExecutionLog>, ILogRepository
{
    private const int DefaultBatchSize = 500;

    public LogRepository(BotPulseDbContext context) : base(context) { }

    public async Task<DateTime?> GetMaxTimestampAsync(string providerName, CancellationToken ct = default) =>
        await Context.ExecutionLogs
            .Where(l => l.ProviderName == providerName)
            .MaxAsync(l => (DateTime?)l.TimestampUtc, ct)
            .ConfigureAwait(false);

    public async Task AddBatchAsync(IEnumerable<ExecutionLogSnapshot> snapshots, string providerName, CancellationToken ct = default)
    {
        var entities = snapshots.Select(s => ExecutionLog.FromSnapshot(s, providerName)).ToList();
        for (var i = 0; i < entities.Count; i += DefaultBatchSize)
        {
            var batch = entities.Skip(i).Take(DefaultBatchSize);
            await Context.ExecutionLogs.AddRangeAsync(batch, ct).ConfigureAwait(false);
            await Context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}
