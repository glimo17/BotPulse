using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Providers.Demo;

internal sealed class DemoQueueProvider : IQueueProvider
{
    private readonly DemoDataSeed _seed;
    public DemoQueueProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<QueueSnapshot>> GetQueuesAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetQueues());

    public Task<IReadOnlyList<QueueItemSnapshot>> GetQueueItemsAsync(QueueItemQuery query, CancellationToken ct = default)
    {
        var items = _seed.GetQueueItems().AsEnumerable();

        if (!string.IsNullOrEmpty(query.QueueName))
        {
            items = items.Where(qi => qi.QueueName == query.QueueName);
        }

        if (query.UpdatedSinceUtc.HasValue)
        {
            items = items.Where(qi => (qi.ProcessingStartUtc.HasValue && qi.ProcessingStartUtc >= query.UpdatedSinceUtc) ||
                                      (qi.ProcessingEndUtc.HasValue && qi.ProcessingEndUtc >= query.UpdatedSinceUtc));
        }

        if (query.Top > 0)
        {
            items = items.Take(query.Top);
        }

        return Task.FromResult<IReadOnlyList<QueueItemSnapshot>>(items.ToList());
    }
}
