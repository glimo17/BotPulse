using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Domain.ValueObjects;

namespace BotPulse.Core.Domain.Entities;

/// <summary>Persisted job entity representing an RPA job execution instance.</summary>
public sealed class Job
{
    private Job() { }

    public long Id { get; private set; }
    public string ExternalJobId { get; private set; } = string.Empty;
    public string ProviderName { get; private set; } = string.Empty;
    public string ProcessExternalId { get; private set; } = string.Empty;
    public string RobotExternalId { get; private set; } = string.Empty;
    public string? MachineExternalId { get; private set; }
    public JobStatus Status { get; private set; } = JobStatus.Pending;
    public DateTime StartTimeUtc { get; private set; }
    public DateTime? EndTimeUtc { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public string? ErrorType { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long? RetryOfJobId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static Job FromSnapshot(JobSnapshot snapshot, string providerName)
    {
        return new Job
        {
            ExternalJobId = snapshot.ExternalId,
            ProviderName = providerName,
            ProcessExternalId = snapshot.ProcessExternalId,
            RobotExternalId = snapshot.RobotExternalId,
            MachineExternalId = snapshot.MachineExternalId,
            Status = JobStatus.Parse(snapshot.Status),
            StartTimeUtc = snapshot.StartTimeUtc,
            EndTimeUtc = snapshot.EndTimeUtc,
            Duration = snapshot.Duration,
            ErrorType = snapshot.ErrorType,
            ErrorMessage = snapshot.ErrorMessage,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateFromSnapshot(JobSnapshot snapshot)
    {
        if (Status.IsTerminal)
        {
            return;
        }

        Status = JobStatus.Parse(snapshot.Status);
        EndTimeUtc = snapshot.EndTimeUtc;
        Duration = snapshot.Duration;
        ErrorType = snapshot.ErrorType;
        ErrorMessage = snapshot.ErrorMessage;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetRetryOf(long originalJobId) => RetryOfJobId = originalJobId;
}
