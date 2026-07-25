namespace BotPulse.Core.Abstractions.Providers.Models;

/// <summary>Point-in-time snapshot of a queue from an RPA provider.</summary>
public sealed record QueueSnapshot(
    string ExternalId,
    string Name,
    int TotalItems,
    int ProcessedItems,
    int FailedItems,
    int PendingItems);

/// <summary>Point-in-time snapshot of a single queue item.</summary>
public sealed record QueueItemSnapshot(
    string ExternalItemId,
    string QueueName,
    string Status,
    int RetryCount,
    DateTime? ProcessingStartUtc,
    DateTime? ProcessingEndUtc,
    string? OriginalExternalItemId,
    string? OutputMetadataJson);

/// <summary>Filter criteria for querying queue items.</summary>
public sealed record QueueItemQuery(
    string? QueueName = null,
    DateTime? UpdatedSinceUtc = null,
    int Top = 500);
