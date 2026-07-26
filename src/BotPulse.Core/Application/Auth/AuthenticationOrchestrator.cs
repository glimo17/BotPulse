using BotPulse.Core.Abstractions.Authentication;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using BotPulse.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace BotPulse.Core.Application.Auth;

/// <summary>
/// Orchestrates authentication: delegates to IAuthenticationProvider,
/// syncs/upserts the user, issues JWT, and records audit.
/// </summary>
public sealed class AuthenticationOrchestrator
{
    private static readonly Action<ILogger, string, Exception?> LogLoginSuccess =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "LoginSuccess"),
            "User {UserName} authenticated successfully");

    private static readonly Action<ILogger, string, string, Exception?> LogLoginFailed =
        LoggerMessage.Define<string, string>(LogLevel.Warning, new EventId(2, "LoginFailed"),
            "Authentication failed for {UserName}: {Reason}");

    private readonly IAuthenticationProvider _authProvider;
    private readonly ISessionTokenService _tokenService;
    private readonly IUserRepository _users;
    private readonly IAuditRepository _audit;
    private readonly ILogger<AuthenticationOrchestrator> _logger;

    public AuthenticationOrchestrator(
        IAuthenticationProvider authProvider,
        ISessionTokenService tokenService,
        IUserRepository users,
        IAuditRepository audit,
        ILogger<AuthenticationOrchestrator> logger)
    {
        _authProvider = authProvider;
        _tokenService = tokenService;
        _users = users;
        _audit = audit;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates the user and returns a JWT session token.
    /// Also upserts the user record and records the audit.
    /// </summary>
    public async Task<LoginResult> LoginAsync(
        AuthenticationRequest request,
        string correlationId,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var authResult = await _authProvider.AuthenticateAsync(request, ct).ConfigureAwait(false);

        if (!authResult.Succeeded)
        {
            LogLoginFailed(_logger, request.UserName ?? "unknown", authResult.FailureReason ?? "unknown", null);

            await _audit.RecordAsync(new AuditRecordData(
                UserId: "anonymous",
                UserName: request.UserName ?? "unknown",
                Action: "Login",
                ResourceType: "User",
                ResourceId: null,
                Outcome: "Failure",
                IpAddress: ipAddress,
                CorrelationId: correlationId,
                DetailsJson: $"{{\"reason\":\"{authResult.FailureReason}\"}}"), ct)
                .ConfigureAwait(false);

            return new LoginResult(false, null, authResult.FailureReason);
        }

        // Upsert user
        var user = await _users.FindByExternalIdAsync(_authProvider.ProviderName, authResult.ExternalUserId!, ct)
            .ConfigureAwait(false);

        var role = authResult.Roles.Contains("Administrator") ? UserRole.Administrator
            : authResult.Roles.Contains("Operator") ? UserRole.Operator
            : UserRole.Viewer;

        if (user is null)
        {
            user = User.Create(
                authResult.ExternalUserId!,
                authResult.UserName ?? authResult.ExternalUserId!,
                authResult.Email ?? string.Empty,
                role,
                _authProvider.ProviderName);
            await _users.AddAsync(user, ct).ConfigureAwait(false);
        }
        else
        {
            user.UpdateRole(role);
            user.RecordLogin();
            _users.Update(user);
        }

        var token = _tokenService.IssueToken(authResult, _authProvider.ProviderName);

        LogLoginSuccess(_logger, authResult.UserName ?? "unknown", null);

        await _audit.RecordAsync(new AuditRecordData(
            UserId: user.ExternalId,
            UserName: user.UserName,
            Action: "Login",
            ResourceType: "User",
            ResourceId: user.Id.ToString(),
            Outcome: "Success",
            IpAddress: ipAddress,
            CorrelationId: correlationId), ct)
            .ConfigureAwait(false);

        return new LoginResult(true, token, null);
    }

    public async Task LogoutAsync(
        string userId,
        string userName,
        string correlationId,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        await _audit.RecordAsync(new AuditRecordData(
            UserId: userId,
            UserName: userName,
            Action: "Logout",
            ResourceType: "User",
            ResourceId: userId,
            Outcome: "Success",
            IpAddress: ipAddress,
            CorrelationId: correlationId), ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Result of a login attempt.</summary>
public sealed record LoginResult(bool Succeeded, string? Token, string? FailureReason);
