using BotPulse.Core.Abstractions.Authentication;
using BotPulse.Core.Abstractions.Persistence;
using Microsoft.Extensions.Logging;

namespace BotPulse.Infrastructure.Authentication;

/// <summary>
/// Authentication provider using a local user store with Argon2id password hashing.
/// INTENDED FOR DEVELOPMENT ENVIRONMENTS ONLY. Use EntraID or LDAP in production.
/// </summary>
internal sealed class LocalAuthenticationProvider : IAuthenticationProvider
{
    private static readonly Action<ILogger, Exception?> LogDevWarning =
        LoggerMessage.Define(LogLevel.Warning, new EventId(1, "DevAuthProvider"),
            "LocalAuthenticationProvider is active. This provider is intended for development environments only.");

    private readonly IUserRepository _users;
    private readonly ILogger<LocalAuthenticationProvider> _logger;

    public LocalAuthenticationProvider(
        IUserRepository users,
        ILogger<LocalAuthenticationProvider> logger)
    {
        _users = users;
        _logger = logger;
        LogDevWarning(logger, null);
    }

    public string ProviderName => "Local";

    public async Task<AuthenticationResult> AuthenticateAsync(
        AuthenticationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
        {
            return Fail("Invalid credentials");
        }

        var user = await _users.FindByUserNameAsync(request.UserName, ct).ConfigureAwait(false);

        if (user is null || !user.IsActive || user.AuthProvider != "Local")
        {
            return Fail("Invalid credentials");
        }

        if (string.IsNullOrEmpty(user.PasswordHash) || !Argon2idPasswordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Fail("Invalid credentials");
        }

        return new AuthenticationResult(
            Succeeded: true,
            ExternalUserId: user.ExternalId,
            UserName: user.UserName,
            Email: user.Email,
            Roles: [user.Role.ToString()],
            FailureReason: null);
    }

    public Task<bool> IsHealthyAsync(CancellationToken ct = default) =>
        Task.FromResult(true);

    private static AuthenticationResult Fail(string reason) =>
        new(false, null, null, null, [], reason);
}
