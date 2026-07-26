using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Providers.Demo;

internal sealed class DemoProcessProvider : IProcessProvider
{
    private readonly DemoDataSeed _seed;
    public DemoProcessProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<ProcessSnapshot>> GetProcessesAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetProcesses());

    public Task<IReadOnlyList<ProcessParameter>> GetProcessParametersAsync(string processExternalId, CancellationToken ct = default)
        => Task.FromResult(_seed.GetProcessParameters(processExternalId));
}
