using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BotPulse.Core.Abstractions.Notifications;

namespace BotPulse.Infrastructure.Notifications;

/// <summary>
/// Polling-based notification delivery.
/// Buffers events in memory. Clients poll GET /api/v1/notifications/pull?since=X.
/// </summary>
public sealed class PollingNotificationDelivery : INotificationDelivery
{
    private readonly ConcurrentQueue<NotificationEvent> _buffer = new();
    private readonly int _maxBufferSize;

    public PollingNotificationDelivery(int maxBufferSize = 1000) => _maxBufferSize = maxBufferSize;

    public Task PublishAsync(NotificationEvent evt, CancellationToken ct = default)
    {
        _buffer.Enqueue(evt);

        // Evict oldest when over limit
        while (_buffer.Count > _maxBufferSize)
        {
            _buffer.TryDequeue(out _);
        }

        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<NotificationEvent> SubscribeAsync(
        NotificationSubscription subscription,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Polling returns nothing via stream — clients use GetEventsSince instead
        await Task.CompletedTask.ConfigureAwait(false);
        yield break;
    }

    public IReadOnlyList<NotificationEvent> GetEventsSince(DateTime since, IReadOnlyList<string>? eventTypes = null)
    {
        var events = _buffer.Where(e => e.TimestampUtc > since);

        if (eventTypes is { Count: > 0 })
        {
            events = events.Where(e => eventTypes.Contains(e.EventType));
        }

        return events.OrderBy(e => e.TimestampUtc).ToList();
    }
}
