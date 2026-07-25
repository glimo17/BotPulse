namespace BotPulse.Core.Abstractions.Authentication;

/// <summary>Abstraction for pluggable authentication providers (Entra ID, LDAP, Local).</summary>
public interface IAuthenticationProvider
{
    /// <summary>Unique name identifying this provider (e.g. "EntraID", "LDAP", "Local").</summary>
    string ProviderName { get; }

    /// <summary>Authenticates a user using provider-specific credentials.</summary>
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default);

    /// <summary>Checks whether this provider is reachable and healthy.</summary>
    Task<bool> IsHealthyAsync(CancellationToken ct = default);
}
