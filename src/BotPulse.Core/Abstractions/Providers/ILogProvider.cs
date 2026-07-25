using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Abstractions.Providers;

/// <summary>Provides read access to execution logs from an RPA vendor.</summary>
public interface ILogProvider
{
    Task<IReadOnlyList<ExecutionLogSnapshot>> GetExecutionLogsAsync(LogQuery query, CancellationToken ct = default);
}
