namespace BotPulse.Core.Abstractions.Providers.Models;

/// <summary>
/// Metadata-only representation of an RPA provider asset.
/// Secret values are never included in this record.
/// </summary>
public sealed record AssetMetadata(
    string ExternalId,
    string Name,
    string Type,
    string Scope,
    DateTime LastModifiedUtc);
