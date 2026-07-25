namespace BotPulse.Core.Abstractions.Notifications;

/// <summary>
/// Abstraction for real-time notification delivery.
/// MVP implementations: Polling, SSE. Future: SignalR, WebSockets.
/// </summary>
public interface INotificationDelivery
{
    /// <summary>Publishes a notification event to all matching subscribers.</summary>
    Task PublishAsync(NotificationEvent evt, CancellationToken ct = default);

    /// <summary>Subscribes to notification events matching the given subscription criteria.</summary>
    IAsyncEnumerable<NotificationEvent> SubscribeAsync(NotificationSubscription subscription, CancellationToken ct = default);
}
