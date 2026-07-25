namespace BotPulse.Core.Abstractions.Notifications;

/// <summary>A notification event delivered to subscribed clients.</summary>
public sealed record NotificationEvent(
    string EventType,
    string ResourceType,
    string ResourceId,
    string PayloadJson,
    DateTime TimestampUtc);

/// <summary>Criteria used to filter notification events for a subscriber.</summary>
public sealed record NotificationSubscription(
    string UserId,
    IReadOnlyList<string> EventTypes,
    IReadOnlyList<string>? ResourceIds = null);
