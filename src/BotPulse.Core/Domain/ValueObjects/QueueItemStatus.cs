namespace BotPulse.Core.Domain.ValueObjects;

/// <summary>Strongly typed queue item status.</summary>
public enum QueueItemStatus
{
    New = 0,
    InProgress = 1,
    Success = 2,
    Failed = 3,
    Retried = 4,
    Abandoned = 5,
}
