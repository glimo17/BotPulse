using BotPulse.Core.Domain.ValueObjects;
using FluentAssertions;

namespace BotPulse.UnitTests.Domain;

public sealed class JobStatusTests
{
    [Theory]
    [InlineData("Success", true)]
    [InlineData("Failed", true)]
    [InlineData("Stopped", true)]
    [InlineData("Cancelled", true)]
    [InlineData("Pending", false)]
    [InlineData("Running", false)]
    public void IsTerminal_ShouldReturnExpectedValue(string status, bool expectedTerminal)
    {
        JobStatus.Parse(status).IsTerminal.Should().Be(expectedTerminal);
    }

    [Fact]
    public void Parse_WithUnknownStatus_ShouldThrow()
    {
        var act = () => JobStatus.Parse("UnknownStatus");
        act.Should().Throw<ArgumentException>().WithMessage("*UnknownStatus*");
    }

    [Fact]
    public void Parse_IsCaseInsensitive()
    {
        JobStatus.Parse("success").Should().Be(JobStatus.Success);
        JobStatus.Parse("FAILED").Should().Be(JobStatus.Failed);
    }
}
