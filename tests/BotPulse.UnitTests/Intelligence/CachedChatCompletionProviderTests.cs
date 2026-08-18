using BotPulse.Intelligence.Caching;
using BotPulse.Intelligence.Contracts.AI;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;

namespace BotPulse.UnitTests.Intelligence;

public sealed class CachedChatCompletionProviderTests
{
    private readonly IChatCompletionProvider _inner = Substitute.For<IChatCompletionProvider>();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private CachedChatCompletionProvider CreateSut() => new(_inner, _cache);

    private static ChatRequest CreateRequest(string userContent) => new(
        Messages: new[] { new ChatMessage(ChatRole.User, userContent) });

    [Fact]
    public async Task CompleteAsync_WithIdenticalRequest_ShouldOnlyCallInnerProviderOnce()
    {
        var expected = new ChatResponse("cached answer", 10, 5);
        _inner.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>()).Returns(expected);

        var sut = CreateSut();
        var request = CreateRequest("diagnose this error");

        var first = await sut.CompleteAsync(request);
        var second = await sut.CompleteAsync(request);

        first.Should().BeEquivalentTo(expected);
        second.Should().BeEquivalentTo(expected);
        await _inner.Received(1).CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CompleteAsync_WithDifferentPrompts_ShouldCallInnerProviderForEach()
    {
        _inner.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse("answer", 1, 1));

        var sut = CreateSut();
        await sut.CompleteAsync(CreateRequest("prompt A"));
        await sut.CompleteAsync(CreateRequest("prompt B"));

        await _inner.Received(2).CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Name_ShouldDelegateToInnerProvider()
    {
        _inner.Name.Returns("Ollama");

        CreateSut().Name.Should().Be("Ollama");
    }
}
