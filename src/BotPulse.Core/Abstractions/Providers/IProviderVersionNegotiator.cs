namespace BotPulse.Core.Abstractions.Providers;

/// <summary>Negotiates the provider API version at startup.</summary>
public interface IProviderVersionNegotiator
{
    /// <summary>
    /// Queries the vendor for its API version and returns the matching implementation identifier.
    /// Returns a record with null SupportedImplementation if the version is not supported.
    /// </summary>
    Task<ProviderVersion> NegotiateAsync(CancellationToken ct = default);
}

/// <summary>Represents the result of a provider version negotiation.</summary>
public sealed record ProviderVersion(
    string ProviderName,
    string VendorVersion,
    string? SupportedImplementation);
