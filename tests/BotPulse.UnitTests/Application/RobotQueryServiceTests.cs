using BotPulse.Core.Abstractions.Caching;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Application.Robots;
using FluentAssertions;
using NSubstitute;

namespace BotPulse.UnitTests.Application;

public sealed class RobotQueryServiceTests
{
    private static RobotSnapshot MakeRobot(string id) =>
        new(id, $"Robot-{id}", "Online", null, null, DateTime.UtcNow);

    [Fact]
    public async Task GetRobotsAsync_WhenCacheMiss_ShouldCallProvider()
    {
        var provider = Substitute.For<IRobotProvider>();
        var cache = Substitute.For<ICacheService>();
        var robots = new[] { MakeRobot("1"), MakeRobot("2") };

        provider.GetRobotsAsync().Returns(robots);
        cache.GetAsync<List<RobotSnapshot>>("robots.all").Returns((List<RobotSnapshot>?)null);

        var sut = new RobotQueryService(provider, cache, new RobotCacheOptions());
        var result = await sut.GetRobotsAsync();

        result.Should().HaveCount(2);
        await provider.Received(1).GetRobotsAsync();
    }

    [Fact]
    public async Task GetRobotsAsync_WhenCacheHit_ShouldNotCallProvider()
    {
        var provider = Substitute.For<IRobotProvider>();
        var cache = Substitute.For<ICacheService>();
        var cached = new List<RobotSnapshot> { MakeRobot("1") };

        cache.GetAsync<List<RobotSnapshot>>("robots.all").Returns(cached);

        var sut = new RobotQueryService(provider, cache, new RobotCacheOptions());
        var result = await sut.GetRobotsAsync();

        result.Should().HaveCount(1);
        await provider.DidNotReceive().GetRobotsAsync();
    }

    [Fact]
    public async Task GetRobotsAsync_WhenForceRefresh_ShouldBypassCache()
    {
        var provider = Substitute.For<IRobotProvider>();
        var cache = Substitute.For<ICacheService>();
        var cached = new List<RobotSnapshot> { MakeRobot("cached") };
        var fresh = new[] { MakeRobot("fresh1"), MakeRobot("fresh2") };

        cache.GetAsync<List<RobotSnapshot>>("robots.all").Returns(cached);
        provider.GetRobotsAsync().Returns(fresh);

        var sut = new RobotQueryService(provider, cache, new RobotCacheOptions());
        var result = await sut.GetRobotsAsync(forceRefresh: true);

        result.Should().HaveCount(2);
        await provider.Received(1).GetRobotsAsync();
    }

    [Fact]
    public async Task GetRobotsAsync_WhenCacheDisabled_ShouldAlwaysCallProvider()
    {
        var provider = Substitute.For<IRobotProvider>();
        var cache = Substitute.For<ICacheService>();
        provider.GetRobotsAsync().Returns(Array.Empty<RobotSnapshot>());

        var sut = new RobotQueryService(provider, cache, new RobotCacheOptions { Enabled = false });
        await sut.GetRobotsAsync();
        await sut.GetRobotsAsync();

        await provider.Received(2).GetRobotsAsync();
    }
}
