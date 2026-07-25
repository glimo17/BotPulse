using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Domain.Entities;

/// <summary>Persisted queue item entity.</summary>
public sealed class QueueItem
{
    private QueueItem() { }

    public long Id { get; private set; }
    public string ExternalItemId { get; private set; } = string.Empty;
    public string ProviderName { get; private set; } = string.Empty;
    public string QueueName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public int RetryCount { get; private set; }
    public DateTime? ProcessingStartUtc { get; private set; }
    public DateTime? ProcessingEndUtc { get; private set; }
    public string? OutputMetadataJson { get; private set; }
    public long? OriginalItemId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static QueueItem FromSnapshot(QueueItemSnapshot snapshot, string providerName)
    {
        return new QueueItem
        {
            ExternalItemId = snapshot.ExternalItemId,
            ProviderName = providerName,
            QueueName = snapshot.QueueName,
            Status = snapshot.Status,
            RetryCount = snapshot.RetryCount,
            ProcessingStartUtc = snapshot.ProcessingStartUtc,
            ProcessingEndUtc = snapshot.ProcessingEndUtc,
            OutputMetadataJson = snapshot.OutputMetadataJson,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateFromSnapshot(QueueItemSnapshot snapshot)
    {
        Status = snapshot.Status;
        RetryCount = snapshot.RetryCount;
        ProcessingStartUtc = snapshot.ProcessingStartUtc;
        ProcessingEndUtc = snapshot.ProcessingEndUtc;
        OutputMetadataJson = snapshot.OutputMetadataJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetOriginalItem(long originalId) => OriginalItemId = originalId;
}
