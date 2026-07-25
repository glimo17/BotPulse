using BotPulse.Core.Abstractions.Authentication;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using BotPulse.Core.Domain.ValueObjects;
using BotPulse.Infrastructure.Authentication;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace BotPulse.UnitTests.Infrastructure;

public sealed class LocalAuthenticationProviderTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    private LocalAuthenticationProvider CreateSut() =>
        new(_users, NullLogger<LocalAuthenticationProvider>.Instance);

    private static User CreateActiveUser(string userName, string password)
    {
        var hash = Argon2idPasswordHasher.Hash(password);
        var user = User.Create(userName, userName, $"{userName}@example.com",
            UserRole.Operator, "Local", hash);
        return user;
    }

    [Fact]
    public async Task Authenticate_WithValidCredentials_ShouldSucceed()
    {
        var user = CreateActiveUser("alice", "correctpassword");
        _users.FindByUserNameAsync("alice").Returns(user);
        var sut = CreateSut();

        var result = await sut.AuthenticateAsync(new AuthenticationRequest("alice", "correctpassword", null));

        result.Succeeded.Should().BeTrue();
        result.UserName.Should().Be("alice");
        result.Roles.Should().Contain("Operator");
    }

    [Fact]
    public async Task Authenticate_WithWrongPassword_ShouldFail()
    {
        var user = CreateActiveUser("alice", "correctpassword");
        _users.FindByUserNameAsync("alice").Returns(user);
        var sut = CreateSut();

        var result = await sut.AuthenticateAsync(new AuthenticationRequest("alice", "wrongpassword", null));

        result.Succeeded.Should().BeFalse();
        result.FailureReason.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task Authenticate_WithNonExistentUser_ShouldFail()
    {
        _users.FindByUserNameAsync("ghost").Returns((User?)null);
        var sut = CreateSut();

        var result = await sut.AuthenticateAsync(new AuthenticationRequest("ghost", "any", null));

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Authenticate_WithEmptyCredentials_ShouldFail()
    {
        var sut = CreateSut();

        var result = await sut.AuthenticateAsync(new AuthenticationRequest("", "", null));

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Authenticate_WithDifferentProvider_ShouldFail()
    {
        var user = User.Create("entra-user", "bob", "bob@example.com",
            UserRole.Viewer, "EntraID");
        _users.FindByUserNameAsync("bob").Returns(user);
        var sut = CreateSut();

        var result = await sut.AuthenticateAsync(new AuthenticationRequest("bob", "any", null));

        result.Succeeded.Should().BeFalse();
    }
}
