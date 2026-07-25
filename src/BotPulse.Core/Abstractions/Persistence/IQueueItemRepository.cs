using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Domain.Entities;

namespace BotPulse.Core.Abstractions.Persistence;

/// <summary>Specialized repository for QueueItem entities.</summary>
public interface IQueueItemRepository : IRepository<QueueItem>
{
    Task<QueueItem?> GetByExternalIdAsync(string providerName, string externalItemId, CancellationToken ct = default);
    Task<DateTime?> GetMaxUpdatedAtAsync(string providerName, CancellationToken ct = default);
    Task<IReadOnlyList<QueueItem>> GetByQueueNameAsync(string queueName, string? status = null, CancellationToken ct = default);
    Task UpsertAsync(QueueItemSnapshot snapshot, string providerName, CancellationToken ct = default);
}
