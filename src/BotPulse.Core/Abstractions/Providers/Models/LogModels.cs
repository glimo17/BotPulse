namespace BotPulse.Core.Abstractions.Providers.Models;

/// <summary>Point-in-time snapshot of an execution log entry from an RPA provider.</summary>
public sealed record ExecutionLogSnapshot(
    string? JobExternalId,
    string? RobotExternalId,
    string? ProcessExternalId,
    DateTime TimestampUtc,
    string Severity,
    string LoggerName,
    string Message,
    string PropertiesJson = "{}");

/// <summary>Filter criteria for querying execution logs.</summary>
public sealed record LogQuery(
    string? JobExternalId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    string? MinSeverity = null,
    int Top = 1000);
