using System.Security.Claims;

namespace BotPulse.Core.Abstractions.Authentication;

/// <summary>Issues and validates session tokens (JWT) after successful authentication.</summary>
public interface ISessionTokenService
{
    /// <summary>Issues a session token for an authenticated user.</summary>
    string IssueToken(AuthenticationResult authenticated, string providerName);

    /// <summary>Validates a session token and returns the claims principal, or throws if invalid.</summary>
    ClaimsPrincipal ValidateToken(string token);
}
