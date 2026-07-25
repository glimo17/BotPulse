using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Abstractions.Providers;

/// <summary>Provides read access to queue and queue item entities from an RPA vendor.</summary>
public interface IQueueProvider
{
    Task<IReadOnlyList<QueueSnapshot>> GetQueuesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<QueueItemSnapshot>> GetQueueItemsAsync(QueueItemQuery query, CancellationToken ct = default);
}
