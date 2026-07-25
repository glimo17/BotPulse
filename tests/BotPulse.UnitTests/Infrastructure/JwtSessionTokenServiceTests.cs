using System.Security.Claims;
using BotPulse.Core.Abstractions.Authentication;
using BotPulse.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BotPulse.UnitTests.Infrastructure;

public sealed class JwtSessionTokenServiceTests
{
    private static JwtSessionTokenService CreateSut(int expirationMinutes = 60) =>
        new(Options.Create(new JwtOptions
        {
            SigningKeyBase64 = Convert.ToBase64String(new byte[32]),
            Issuer = "botpulse-test",
            Audience = "botpulse-api-test",
            ExpirationMinutes = expirationMinutes,
        }), NullLogger<JwtSessionTokenService>.Instance);

    private static AuthenticationResult ValidAuth(string userId = "user-1") =>
        new(true, userId, "alice", "alice@example.com",
            new[] { "Operator" }, null);

    [Fact]
    public void IssueToken_ThenValidate_ShouldReturnSameClaims()
    {
        var sut = CreateSut();
        var auth = ValidAuth();
        var token = sut.IssueToken(auth, "Local");
        var principal = sut.ValidateToken(token);

        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(auth.ExternalUserId);
        principal.FindFirst(ClaimTypes.Name)!.Value.Should().Be(auth.UserName);
        principal.FindFirst(ClaimTypes.Email)!.Value.Should().Be(auth.Email);
        principal.FindFirst("auth_provider")!.Value.Should().Be("Local");
        principal.IsInRole("Operator").Should().BeTrue();
    }

    [Fact]
    public void ValidateToken_WithTamperedToken_ShouldThrow()
    {
        var sut = CreateSut();
        var token = sut.IssueToken(ValidAuth(), "Local");
        var tampered = token[..^5] + "XXXXX";

        var act = () => sut.ValidateToken(tampered);
        act.Should().Throw<SecurityTokenException>();
    }

    [Fact]
    public void IssueToken_IncludesAllRoles()
    {
        var sut = CreateSut();
        var auth = new AuthenticationResult(true, "u1", "bob", "bob@x.com",
            new[] { "Viewer", "Operator" }, null);

        var token = sut.IssueToken(auth, "EntraID");
        var principal = sut.ValidateToken(token);

        principal.IsInRole("Viewer").Should().BeTrue();
        principal.IsInRole("Operator").Should().BeTrue();
        principal.IsInRole("Administrator").Should().BeFalse();
    }
}
