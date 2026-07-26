using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Providers.Demo;

internal sealed class DemoAssetProvider : IAssetProvider
{
    private readonly DemoDataSeed _seed;
    public DemoAssetProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<AssetMetadata>> GetAssetsAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetAssets());
}
