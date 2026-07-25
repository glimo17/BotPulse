using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Domain.Entities;

/// <summary>Persisted execution log entry from an RPA provider.</summary>
public sealed class ExecutionLog
{
    private ExecutionLog() { }

    public long Id { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string Severity { get; private set; } = string.Empty;
    public string LoggerName { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string? JobExternalId { get; private set; }
    public string? RobotExternalId { get; private set; }
    public string? ProcessExternalId { get; private set; }
    public string PropertiesJson { get; private set; } = "{}";
    public string ProviderName { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public static ExecutionLog FromSnapshot(ExecutionLogSnapshot snapshot, string providerName)
    {
        return new ExecutionLog
        {
            TimestampUtc = snapshot.TimestampUtc,
            Severity = snapshot.Severity,
            LoggerName = snapshot.LoggerName,
            Message = snapshot.Message,
            JobExternalId = snapshot.JobExternalId,
            RobotExternalId = snapshot.RobotExternalId,
            ProcessExternalId = snapshot.ProcessExternalId,
            PropertiesJson = snapshot.PropertiesJson,
            ProviderName = providerName,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
