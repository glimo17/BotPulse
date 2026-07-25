using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BotPulse.Core.Abstractions.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BotPulse.Infrastructure.Authentication;

/// <summary>Issues and validates JWT session tokens post-authentication.</summary>
internal sealed class JwtSessionTokenService : ISessionTokenService
{
    private static readonly Action<ILogger, string?, string, Exception?> LogJwtIssued =
        LoggerMessage.Define<string?, string>(
            LogLevel.Information,
            new EventId(1, nameof(IssueToken)),
            "JWT issued for user {UserId} via {Provider}");

    private readonly JwtOptions _options;
    private readonly ILogger<JwtSessionTokenService> _logger;

    public JwtSessionTokenService(IOptions<JwtOptions> options, ILogger<JwtSessionTokenService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string IssueToken(AuthenticationResult authenticated, string providerName)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(_options.SigningKeyBase64));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authenticated.ExternalUserId ?? string.Empty),
            new(ClaimTypes.Name, authenticated.UserName ?? string.Empty),
            new(ClaimTypes.Email, authenticated.Email ?? string.Empty),
            new("auth_provider", providerName),
        };

        claims.AddRange((authenticated.Roles ?? [])
            .Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: creds);

        LogJwtIssued(_logger, authenticated.ExternalUserId, providerName, null);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc/>
    public ClaimsPrincipal ValidateToken(string token)
    {
        var key = new SymmetricSecurityKey(Convert.FromBase64String(_options.SigningKeyBase64));
        var handler = new JwtSecurityTokenHandler();

        var principal = handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10),
        }, out _);

        return principal;
    }
}
