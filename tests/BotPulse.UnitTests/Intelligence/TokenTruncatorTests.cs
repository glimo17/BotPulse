using BotPulse.Intelligence.Knowledge;
using FluentAssertions;

namespace BotPulse.UnitTests.Intelligence;

public sealed class TokenTruncatorTests
{
    [Fact]
    public void Truncate_ContentShorterThanLimit_ShouldReturnUnchanged()
    {
        var result = TokenTruncator.Truncate("short text", maxTokens: 100);

        result.Should().Be("short text");
    }

    [Fact]
    public void Truncate_ContentLongerThanLimit_ShouldCutToApproxCharBudget()
    {
        var content = new string('a', 1000);

        var result = TokenTruncator.Truncate(content, maxTokens: 10); // ~40 chars

        result.Length.Should().Be(40);
    }

    [Fact]
    public void Truncate_WithZeroOrNegativeMaxTokens_ShouldReturnContentUnchanged()
    {
        var content = "some content";

        TokenTruncator.Truncate(content, maxTokens: 0).Should().Be(content);
        TokenTruncator.Truncate(content, maxTokens: -5).Should().Be(content);
    }

    [Fact]
    public void Truncate_EmptyContent_ShouldReturnEmpty()
    {
        TokenTruncator.Truncate(string.Empty, maxTokens: 10).Should().BeEmpty();
    }
}
