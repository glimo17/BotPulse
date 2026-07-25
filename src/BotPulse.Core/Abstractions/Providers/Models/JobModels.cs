namespace BotPulse.Core.Abstractions.Providers.Models;

/// <summary>Point-in-time snapshot of a job from an RPA provider.</summary>
public sealed record JobSnapshot(
    string ExternalId,
    string ProcessExternalId,
    string RobotExternalId,
    string? MachineExternalId,
    string Status,
    DateTime StartTimeUtc,
    DateTime? EndTimeUtc,
    TimeSpan? Duration,
    string? ErrorType,
    string? ErrorMessage);

/// <summary>Request to start a new job on an RPA provider.</summary>
public sealed record StartJobRequest(
    string ProcessExternalId,
    string? RobotExternalId,
    IReadOnlyDictionary<string, object>? Parameters = null,
    string Priority = "Normal");

/// <summary>Result of a successful job start operation.</summary>
public sealed record StartJobResult(string JobExternalId);

/// <summary>Filter criteria for querying jobs from an RPA provider.</summary>
public sealed record JobQuery(
    DateTime? UpdatedSinceUtc = null,
    string? Status = null,
    string? RobotExternalId = null,
    string? ProcessExternalId = null,
    int Top = 100,
    int Skip = 0);
