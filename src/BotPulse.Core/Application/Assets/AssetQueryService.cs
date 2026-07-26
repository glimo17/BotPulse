using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Application.Assets;

/// <summary>
/// Query service for assets. Reads on-demand from the RPA provider.
/// Assets are NEVER cached and NEVER persisted — secret values must never be stored.
/// Every access is audit-logged.
/// </summary>
public sealed class AssetQueryService
{
    private readonly IAssetProvider _provider;
    private readonly IAuditRepository _audit;

    public AssetQueryService(IAssetProvider provider, IAuditRepository audit)
    {
        _provider = provider;
        _audit = audit;
    }

    /// <summary>
    /// Returns asset metadata only. Secret values are never included.
    /// Every call is recorded in the audit log.
    /// </summary>
    public async Task<IReadOnlyList<AssetMetadata>> GetAssetsAsync(
        string requestingUserId,
        string requestingUserName,
        string correlationId,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var assets = await _provider.GetAssetsAsync(ct).ConfigureAwait(false);

        await _audit.RecordAsync(new AuditRecordData(
            UserId: requestingUserId,
            UserName: requestingUserName,
            Action: "ViewAssets",
            ResourceType: "Asset",
            ResourceId: null,
            Outcome: "Success",
            IpAddress: ipAddress,
            CorrelationId: correlationId,
            DetailsJson: $"{{\"count\":{assets.Count}}}"),
            ct).ConfigureAwait(false);

        return assets;
    }
}
