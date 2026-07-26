using BotPulse.Core.Abstractions.Persistence;

namespace BotPulse.Core.Application.Queues;

/// <summary>Analytics over persisted queue items.</summary>
public sealed class QueueAnalyticsService
{
    private readonly IQueueItemRepository _queueItems;

    public QueueAnalyticsService(IQueueItemRepository queueItems) => _queueItems = queueItems;

    public async Task<QueueAnalytics> GetQueueAnalyticsAsync(string queueName, CancellationToken ct = default)
    {
        var items = await _queueItems.GetByQueueNameAsync(queueName, ct: ct).ConfigureAwait(false);

        var total = items.Count;
        var processed = items.Count(i => i.Status == "Success");
        var failed = items.Count(i => i.Status is "Failed" or "ApplicationException" or "BusinessException");
        var pending = items.Count(i => i.Status is "New" or "InProgress");

        var durations = items
            .Where(i => i.ProcessingStartUtc.HasValue && i.ProcessingEndUtc.HasValue)
            .Select(i => (i.ProcessingEndUtc!.Value - i.ProcessingStartUtc!.Value).TotalSeconds)
            .ToList();

        var avgProcessingSeconds = durations.Count > 0 ? durations.Average() : 0;

        return new QueueAnalytics(queueName, total, processed, failed, pending, avgProcessingSeconds);
    }
}

/// <summary>Analytics summary for a single queue.</summary>
public sealed record QueueAnalytics(
    string QueueName,
    int TotalItems,
    int ProcessedItems,
    int FailedItems,
    int PendingItems,
    double AvgProcessingSeconds);
