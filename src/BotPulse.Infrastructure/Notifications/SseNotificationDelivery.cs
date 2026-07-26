using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using BotPulse.Core.Abstractions.Notifications;
using Microsoft.Extensions.Logging;

namespace BotPulse.Infrastructure.Notifications;

/// <summary>
/// Server-Sent Events notification delivery.
/// Clients connect via GET /api/v1/notifications/stream and receive events as text/event-stream.
/// </summary>
public sealed class SseNotificationDelivery : INotificationDelivery
{
    private static readonly Action<ILogger, string, Exception?> LogWriteFailed =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1, "SseWriteFailed"),
            "Failed to write notification to subscriber {SubscriberId}");

    private readonly ConcurrentDictionary<string, Channel<NotificationEvent>> _subscribers = new();
    private readonly ILogger<SseNotificationDelivery> _logger;

    public SseNotificationDelivery(ILogger<SseNotificationDelivery> logger) => _logger = logger;

    public async Task PublishAsync(NotificationEvent evt, CancellationToken ct = default)
    {
        foreach (var (subscriberId, channel) in _subscribers)
        {
            try
            {
                await channel.Writer.WriteAsync(evt, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogWriteFailed(_logger, subscriberId, ex);
            }
        }
    }

    public async IAsyncEnumerable<NotificationEvent> SubscribeAsync(
        NotificationSubscription subscription,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<NotificationEvent>(
            new UnboundedChannelOptions { SingleReader = true });
        var subscriberId = Guid.NewGuid().ToString("N");

        _subscribers.TryAdd(subscriberId, channel);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                if (subscription.EventTypes.Count > 0 &&
                    !subscription.EventTypes.Contains(evt.EventType))
                {
                    continue;
                }

                yield return evt;
            }
        }
        finally
        {
            _subscribers.TryRemove(subscriberId, out _);
            channel.Writer.TryComplete();
        }
    }
}
