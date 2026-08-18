using System.Runtime.CompilerServices;
using System.Text;
using BotPulse.Intelligence.Contracts.AI;

namespace BotPulse.Intelligence.Providers.Demo;

/// <summary>
/// Dependency-free chat completion provider for demos and local development
/// (mirrors the Demo RPA provider philosophy, ADR-014). Produces a plausible,
/// well-formed diagnosis in the exact format the DiagnosisResponseParser
/// expects, so the full pipeline and UI can be demonstrated without any
/// external LLM.
/// </summary>
public sealed class DemoChatCompletionProvider : IChatCompletionProvider
{
    public string Name => "Demo";

    public Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default)
    {
        var content = BuildDiagnosis(request);
        return Task.FromResult(new ChatResponse(content, PromptTokens: 0, CompletionTokens: 0));
    }

    public async IAsyncEnumerable<string> StreamAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var content = BuildDiagnosis(request);
        foreach (var word in content.Split(' '))
        {
            ct.ThrowIfCancellationRequested();
            yield return word + " ";
            await Task.Delay(15, ct).ConfigureAwait(false);
        }
    }

    private static string BuildDiagnosis(ChatRequest request)
    {
        // Extract the user prompt to tailor the canned response slightly.
        var userContent = request.Messages
            .LastOrDefault(m => m.Role == ChatRole.User)?.Content ?? string.Empty;

        var hasContext = userContent.Contains("Relevant historical context", StringComparison.OrdinalIgnoreCase);
        var confidence = hasContext ? "0.82" : "0.30";

        var sb = new StringBuilder();
        sb.AppendLine("ROOT_CAUSE: The automation failed because a targeted UI element could not be located, most likely due to a selector or timing change in the target application.");
        sb.AppendLine("IMPACT: The affected process cannot complete unattended and will fail on every run until corrected.");
        sb.AppendLine("RESOLUTION_STEPS: Verify the target application version | Update the broken selector or add a wait/retry | Re-run the job to confirm the fix");
        sb.Append("CONFIDENCE: ").Append(confidence);
        return sb.ToString();
    }
}
