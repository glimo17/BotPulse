using BotPulse.Core.Abstractions.Authentication;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Application.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BotPulse.UnitTests.Application;

public sealed class AuthenticationOrchestratorTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnToken()
    {
        var authProvider = Substitute.For<IAuthenticationProvider>();
        var tokenService = Substitute.For<ISessionTokenService>();
        var users = Substitute.For<IUserRepository>();
        var audit = Substitute.For<IAuditRepository>();

        authProvider.ProviderName.Returns("Local");
        authProvider.AuthenticateAsync(Arg.Any<AuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AuthenticationResult(true, "ext-1", "alice", "alice@x.com", ["Operator"], null));
        tokenService.IssueToken(Arg.Any<AuthenticationResult>(), Arg.Any<string>())
            .Returns("jwt-token-xyz");

        users.FindByExternalIdAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((BotPulse.Core.Domain.Entities.User?)null);

        var sut = new AuthenticationOrchestrator(
            authProvider, tokenService, users, audit, NullLogger<AuthenticationOrchestrator>.Instance);

        var result = await sut.LoginAsync(new AuthenticationRequest("alice", "pass", null), "corr-1");

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be("jwt-token-xyz");
        await audit.Received(1).RecordAsync(
            Arg.Is<AuditRecordData>(a => a.Action == "Login" && a.Outcome == "Success"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ShouldReturnFailure()
    {
        var authProvider = Substitute.For<IAuthenticationProvider>();
        var audit = Substitute.For<IAuditRepository>();

        authProvider.ProviderName.Returns("Local");
        authProvider.AuthenticateAsync(Arg.Any<AuthenticationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new AuthenticationResult(false, null, null, null, [], "Invalid credentials"));

        var sut = new AuthenticationOrchestrator(
            authProvider,
            Substitute.For<ISessionTokenService>(),
            Substitute.For<IUserRepository>(),
            audit,
            NullLogger<AuthenticationOrchestrator>.Instance);

        var result = await sut.LoginAsync(new AuthenticationRequest("alice", "wrong", null), "corr-1");

        result.Succeeded.Should().BeFalse();
        result.Token.Should().BeNull();
        await audit.Received(1).RecordAsync(
            Arg.Is<AuditRecordData>(a => a.Action == "Login" && a.Outcome == "Failure"),
            Arg.Any<CancellationToken>());
    }
}
