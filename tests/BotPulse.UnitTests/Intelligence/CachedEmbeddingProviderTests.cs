using BotPulse.Intelligence.Caching;
using BotPulse.Intelligence.Contracts.AI;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace BotPulse.UnitTests.Intelligence;

public sealed class CachedEmbeddingProviderTests
{
    private readonly IEmbeddingProvider _inner = Substitute.For<IEmbeddingProvider>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private CachedEmbeddingProvider CreateSut() => new(_inner, _cache);

    [Fact]
    public async Task EmbedAsync_WithSameText_ShouldOnlyCallInnerProviderOnce()
    {
        var expected = new EmbeddingResult(new[] { 0.1f, 0.2f }, 2);
        _inner.EmbedAsync("hello world", Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut();

        var first = await sut.EmbedAsync("hello world");
        var second = await sut.EmbedAsync("hello world");

        first.Should().BeEquivalentTo(expected);
        second.Should().BeEquivalentTo(expected);
        await _inner.Received(1).EmbedAsync("hello world", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmbedAsync_WithDifferentText_ShouldCallInnerProviderForEach()
    {
        _inner.EmbedAsync("text a", Arg.Any<CancellationToken>()).Returns(new EmbeddingResult(new[] { 1f }, 1));
        _inner.EmbedAsync("text b", Arg.Any<CancellationToken>()).Returns(new EmbeddingResult(new[] { 2f }, 1));

        var sut = CreateSut();

        await sut.EmbedAsync("text a");
        await sut.EmbedAsync("text b");

        await _inner.Received(1).EmbedAsync("text a", Arg.Any<CancellationToken>());
        await _inner.Received(1).EmbedAsync("text b", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmbedBatchAsync_ShouldOnlyRequestUncachedTexts()
    {
        _inner.EmbedAsync("cached", Arg.Any<CancellationToken>()).Returns(new EmbeddingResult(new[] { 1f }, 1));
        var sut = CreateSut();
        await sut.EmbedAsync("cached"); // warm the cache

        _inner.EmbedBatchAsync(
                Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "fresh"),
                Arg.Any<CancellationToken>())
            .Returns(new List<EmbeddingResult> { new(new[] { 2f }, 1) });

        var results = await sut.EmbedBatchAsync(new[] { "cached", "fresh" });

        results.Should().HaveCount(2);
        await _inner.Received(1).EmbedBatchAsync(
            Arg.Is<IReadOnlyList<string>>(l => l.Count == 1 && l[0] == "fresh"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Name_And_Dimensions_ShouldDelegateToInnerProvider()
    {
        _inner.Name.Returns("Ollama");
        _inner.Dimensions.Returns(768);

        var sut = CreateSut();

        sut.Name.Should().Be("Ollama");
        sut.Dimensions.Should().Be(768);
    }
}
