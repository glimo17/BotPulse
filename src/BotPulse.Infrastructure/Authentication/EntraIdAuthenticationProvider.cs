using BotPulse.Core.Abstractions.Authentication;

namespace BotPulse.Infrastructure.Authentication;

/// <summary>
/// Microsoft Entra ID (Azure AD) authentication provider using OpenID Connect.
/// TODO Phase 2: Implement OIDC flow with PKCE and token validation.
/// </summary>
internal sealed class EntraIdAuthenticationProvider : IAuthenticationProvider
{
    public string ProviderName => "EntraID";

    public Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default) =>
        throw new NotImplementedException("EntraID authentication will be implemented in Phase 2.");

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) =>
        Task.FromResult(false);
}
