using BotPulse.Intelligence.Diagnostics;
using FluentAssertions;

namespace BotPulse.UnitTests.Intelligence;

public sealed class DiagnosisResponseParserTests
{
    [Fact]
    public void Parse_WellFormedResponse_ShouldExtractAllFields()
    {
        var content = """
            ROOT_CAUSE: Selector broke after UI change.
            IMPACT: All executions fail.
            RESOLUTION_STEPS: Fix selector | Test | Deploy
            CONFIDENCE: 0.75
            """;

        var (rootCause, impact, steps, confidence) = DiagnosisResponseParser.Parse(content);

        rootCause.Should().Be("Selector broke after UI change.");
        impact.Should().Be("All executions fail.");
        steps.Should().Equal("Fix selector", "Test", "Deploy");
        confidence.Should().Be(0.75);
    }

    [Fact]
    public void Parse_MissingFields_ShouldReturnSafeDefaults()
    {
        var (rootCause, impact, steps, confidence) = DiagnosisResponseParser.Parse("garbage output");

        rootCause.Should().NotBeNullOrWhiteSpace();
        impact.Should().NotBeNullOrWhiteSpace();
        steps.Should().BeEmpty();
        confidence.Should().Be(0.2); // default low confidence fallback
    }

    [Theory]
    [InlineData("CONFIDENCE: 1.5", 1.0)]   // clamped to max
    [InlineData("CONFIDENCE: -0.5", 0.0)]  // clamped to min
    [InlineData("CONFIDENCE: not-a-number", 0.2)] // fallback
    public void Parse_ConfidenceOutOfRangeOrInvalid_ShouldClampOrFallback(string confidenceSection, double expected)
    {
        var content = $"ROOT_CAUSE: x\nIMPACT: y\nRESOLUTION_STEPS: z\n{confidenceSection}";

        var (_, _, _, confidence) = DiagnosisResponseParser.Parse(content);

        confidence.Should().Be(expected);
    }
}
