using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Providers.Demo;

internal sealed class DemoRobotProvider : IRobotProvider
{
    private readonly DemoDataSeed _seed;
    public DemoRobotProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<RobotSnapshot>> GetRobotsAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetRobots());

    public Task<RobotSnapshot?> GetRobotByIdAsync(string externalId, CancellationToken ct = default)
    {
        var robot = _seed.GetRobots().FirstOrDefault(r => r.ExternalId == externalId);
        return Task.FromResult(robot);
    }
}
