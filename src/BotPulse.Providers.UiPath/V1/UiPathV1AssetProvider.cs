using System.Globalization;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Providers.UiPath.Common;

namespace BotPulse.Providers.UiPath.V1;

/// <summary>
/// Retrieves asset metadata from UiPath. Secret values are NEVER exposed.
/// </summary>
internal sealed class UiPathV1AssetProvider : IAssetProvider
{
    private readonly UiPathHttpClient _http;
    public UiPathV1AssetProvider(UiPathHttpClient http) => _http = http;

    public async Task<IReadOnlyList<AssetMetadata>> GetAssetsAsync(CancellationToken ct = default)
    {
        var dtos = await _http.GetOdataAsync<UiPathAssetDto>("odata/Assets", ct: ct).ConfigureAwait(false);
        return dtos.Select(Map).ToList();
    }

    // Secret values are deliberately excluded — Map only returns metadata
    private static AssetMetadata Map(UiPathAssetDto dto) => new(
        ExternalId: dto.Id.ToString(CultureInfo.InvariantCulture),
        Name: dto.Name,
        Type: dto.ValueType ?? "Text",
        Scope: dto.HasDefaultValue ? "Global" : "PerRobot",
        LastModifiedUtc: dto.LastModificationTime ?? DateTime.UtcNow);

    private sealed class UiPathAssetDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? ValueType { get; init; }
        public bool HasDefaultValue { get; init; }
        public DateTime? LastModificationTime { get; init; }
        // StringValue, IntValue, BoolValue, CredentialUsername etc. are intentionally NOT mapped
    }
}
