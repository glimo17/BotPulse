namespace BotPulse.Core.Abstractions.Authentication;

/// <summary>Input credentials for an authentication attempt.</summary>
public sealed record AuthenticationRequest(
    string? UserName,
    string? Password,
    string? IdTokenFromExternalIdp,
    IReadOnlyDictionary<string, string>? AdditionalParameters = null);

/// <summary>Result of an authentication attempt.</summary>
public sealed record AuthenticationResult(
    bool Succeeded,
    string? ExternalUserId,
    string? UserName,
    string? Email,
    IReadOnlyList<string> Roles,
    string? FailureReason);
