using BotPulse.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace BotPulse.UnitTests.Infrastructure;

public sealed class MemoryCacheServiceTests
{
    private static MemoryCacheService CreateSut() =>
        new(new MemoryCache(new MemoryCacheOptions()), NullLogger<MemoryCacheService>.Instance);

    [Fact]
    public async Task SetAsync_ThenGetAsync_ShouldReturnValue()
    {
        var sut = CreateSut();
        await sut.SetAsync("key1", new TestItem("hello"), TimeSpan.FromMinutes(5));
        var result = await sut.GetAsync<TestItem>("key1");
        result.Should().NotBeNull();
        result!.Value.Should().Be("hello");
    }

    [Fact]
    public async Task GetAsync_MissingKey_ShouldReturnNull()
    {
        var sut = CreateSut();
        var result = await sut.GetAsync<TestItem>("missing");
        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveAsync_ShouldEvictEntry()
    {
        var sut = CreateSut();
        await sut.SetAsync("key2", new TestItem("x"), TimeSpan.FromMinutes(1));
        await sut.RemoveAsync("key2");
        var result = await sut.GetAsync<TestItem>("key2");
        result.Should().BeNull();
    }

    [Fact]
    public async Task InvalidatePatternAsync_ShouldRemoveMatchingKeys()
    {
        var sut = CreateSut();
        await sut.SetAsync("robots.all", new TestItem("a"), TimeSpan.FromMinutes(1));
        await sut.SetAsync("robots.detail.1", new TestItem("b"), TimeSpan.FromMinutes(1));
        await sut.SetAsync("jobs.all", new TestItem("c"), TimeSpan.FromMinutes(1));

        await sut.InvalidatePatternAsync("robots.");

        (await sut.GetAsync<TestItem>("robots.all")).Should().BeNull();
        (await sut.GetAsync<TestItem>("robots.detail.1")).Should().BeNull();
        (await sut.GetAsync<TestItem>("jobs.all")).Should().NotBeNull();
    }

    [Fact]
    public async Task InvalidatePatternAsync_ShouldNotRemoveNonMatchingKeys()
    {
        var sut = CreateSut();
        await sut.SetAsync("prefix.a", new TestItem("1"), TimeSpan.FromMinutes(1));
        await sut.SetAsync("other.b", new TestItem("2"), TimeSpan.FromMinutes(1));

        await sut.InvalidatePatternAsync("prefix.");

        (await sut.GetAsync<TestItem>("other.b")).Should().NotBeNull();
    }

    private sealed record TestItem(string Value);
}
