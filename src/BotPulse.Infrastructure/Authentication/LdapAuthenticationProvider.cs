using BotPulse.Core.Abstractions.Authentication;

namespace BotPulse.Infrastructure.Authentication;

/// <summary>
/// LDAP / Active Directory authentication provider.
/// TODO Phase 2: Implement LDAP bind with group-to-role mapping.
/// </summary>
internal sealed class LdapAuthenticationProvider : IAuthenticationProvider
{
    public string ProviderName => "LDAP";

    public Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct = default) =>
        throw new NotSupportedException("LDAP authentication will be implemented in Phase 2.");

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) =>
        Task.FromResult(false);
}
