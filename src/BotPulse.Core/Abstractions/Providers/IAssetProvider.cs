using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Abstractions.Providers;

/// <summary>
/// Provides read access to asset metadata from an RPA vendor.
/// Secret values are never exposed; only metadata is returned.
/// </summary>
public interface IAssetProvider
{
    Task<IReadOnlyList<AssetMetadata>> GetAssetsAsync(CancellationToken ct = default);
}
