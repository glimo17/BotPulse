using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Abstractions.Providers;

/// <summary>Provides read access to process (release) entities from an RPA vendor.</summary>
public interface IProcessProvider
{
    Task<IReadOnlyList<ProcessSnapshot>> GetProcessesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ProcessParameter>> GetProcessParametersAsync(string processExternalId, CancellationToken ct = default);
}
