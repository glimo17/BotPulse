using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Providers.Demo;

internal sealed class DemoMachineProvider : IMachineProvider
{
    private readonly DemoDataSeed _seed;
    public DemoMachineProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<MachineSnapshot>> GetMachinesAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetMachines());

    public Task<MachineSnapshot?> GetMachineByIdAsync(string externalId, CancellationToken ct = default)
    {
        var machine = _seed.GetMachines().FirstOrDefault(m => m.ExternalId == externalId);
        return Task.FromResult(machine);
    }
}
