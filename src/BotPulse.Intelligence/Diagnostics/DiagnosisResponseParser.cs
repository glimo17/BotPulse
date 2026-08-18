using System.Globalization;

namespace BotPulse.Intelligence.Diagnostics;

/// <summary>
/// Parses the structured LLM response produced using DiagnosisPromptBuilder's
/// format into typed fields. Falls back to safe defaults for any field that
/// cannot be parsed, so a malformed LLM response never throws.
/// </summary>
internal static class DiagnosisResponseParser
{
    private const double DefaultLowConfidence = 0.2;

    public static (string RootCause, string Impact, IReadOnlyList<string> Steps, double Confidence) Parse(
        string content)
    {
        var rootCause = ExtractLine(content, "ROOT_CAUSE:");
        var impact = ExtractLine(content, "IMPACT:");
        var stepsLine = ExtractLine(content, "RESOLUTION_STEPS:");
        var confidenceLine = ExtractLine(content, "CONFIDENCE:");

        var steps = string.IsNullOrWhiteSpace(stepsLine)
            ? Array.Empty<string>()
            : stepsLine.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var confidence = double.TryParse(confidenceLine, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Clamp(parsed, 0.0, 1.0)
            : DefaultLowConfidence;

        return (
            string.IsNullOrWhiteSpace(rootCause) ? "Unable to determine root cause from available context." : rootCause,
            string.IsNullOrWhiteSpace(impact) ? "Impact could not be determined." : impact,
            steps,
            confidence);
    }

    private static string ExtractLine(string content, string prefix)
    {
        foreach (var line in content.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[prefix.Length..].Trim();
            }
        }

        return string.Empty;
    }
}
