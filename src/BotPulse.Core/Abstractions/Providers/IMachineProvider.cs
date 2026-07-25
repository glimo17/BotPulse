using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Abstractions.Providers;

/// <summary>Provides read access to machine entities from an RPA vendor.</summary>
public interface IMachineProvider
{
    Task<IReadOnlyList<MachineSnapshot>> GetMachinesAsync(CancellationToken ct = default);
    Task<MachineSnapshot?> GetMachineByIdAsync(string externalId, CancellationToken ct = default);
}
