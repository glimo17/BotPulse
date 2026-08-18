namespace BotPulse.Intelligence.Knowledge;

/// <summary>
/// Truncates text to stay within a configured token budget before embedding
/// or prompting (ADR-016 NFR-01: cost control).
/// Uses a simple heuristic (~4 chars per token) to avoid a tokenizer
/// dependency in the MVP — good enough for a safety cap, not exact counting.
/// </summary>
internal static class TokenTruncator
{
    private const int ApproxCharsPerToken = 4;

    /// <summary>
    /// Returns the content truncated to approximately <paramref name="maxTokens"/> tokens.
    /// If the content already fits, it is returned unchanged.
    /// </summary>
    public static string Truncate(string content, int maxTokens)
    {
        if (maxTokens <= 0 || string.IsNullOrEmpty(content))
        {
            return content;
        }

        var maxChars = maxTokens * ApproxCharsPerToken;
        return content.Length <= maxChars ? content : content[..maxChars];
    }
}
