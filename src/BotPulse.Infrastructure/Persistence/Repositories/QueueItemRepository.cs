using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.Infrastructure.Persistence.Repositories;

/// <summary>EF Core implementation of IQueueItemRepository.</summary>
internal sealed class QueueItemRepository : GenericRepository<QueueItem>, IQueueItemRepository
{
    public QueueItemRepository(BotPulseDbContext context) : base(context) { }

    public async Task<QueueItem?> GetByExternalIdAsync(string providerName, string externalItemId, CancellationToken ct = default) =>
        await Context.QueueItems
            .FirstOrDefaultAsync(q => q.ProviderName == providerName && q.ExternalItemId == externalItemId, ct)
            .ConfigureAwait(false);

    public async Task<DateTime?> GetMaxUpdatedAtAsync(string providerName, CancellationToken ct = default) =>
        await Context.QueueItems
            .Where(q => q.ProviderName == providerName)
            .MaxAsync(q => (DateTime?)q.UpdatedAtUtc, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<QueueItem>> GetByQueueNameAsync(string queueName, string? status = null, CancellationToken ct = default)
    {
        var query = Context.QueueItems.Where(q => q.QueueName == queueName);
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(q => q.Status == status);
        }

        return await query.OrderByDescending(q => q.UpdatedAtUtc).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task UpsertAsync(QueueItemSnapshot snapshot, string providerName, CancellationToken ct = default)
    {
        var existing = await GetByExternalIdAsync(providerName, snapshot.ExternalItemId, ct).ConfigureAwait(false);
        if (existing is null)
        {
            var newItem = QueueItem.FromSnapshot(snapshot, providerName);
            if (!string.IsNullOrEmpty(snapshot.OriginalExternalItemId))
            {
                var original = await GetByExternalIdAsync(providerName, snapshot.OriginalExternalItemId, ct).ConfigureAwait(false);
                if (original is not null)
                {
                    newItem.SetOriginalItem(original.Id);
                }
            }

            await AddAsync(newItem, ct).ConfigureAwait(false);
        }
        else
        {
            existing.UpdateFromSnapshot(snapshot);
            Update(existing);
        }
    }
}
