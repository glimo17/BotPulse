namespace BotPulse.Core.Abstractions.Notifications;

/// <summary>
/// Throttles notification delivery to prevent flooding clients with rapid successive updates.
/// Maximum rate: 1 delivery per second per (ResourceType, ResourceId) pair.
/// </summary>
public interface INotificationThrottler
{
    /// <summary>
    /// Returns true if the event should be delivered now; false if it should be coalesced.
    /// </summary>
    bool ShouldDeliver(NotificationEvent evt);
}
