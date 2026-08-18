using System.Globalization;
using System.Text;
using BotPulse.Intelligence.Contracts.Diagnostics;
using BotPulse.Intelligence.Contracts.Vector;

namespace BotPulse.Intelligence.Diagnostics;

/// <summary>
/// Builds the system and user prompt for the RAG diagnostic pipeline
/// (Requisito 5.2). Instructs the LLM to respond in a strict, parseable
/// format so DiagnosisResponseParser can extract structured fields.
/// </summary>
internal static class DiagnosisPromptBuilder
{
    public const string SystemPrompt = """
        You are an expert RPA operations diagnostic assistant for BotPulse.
        Given a job failure and relevant historical context, produce a diagnosis
        with exactly these four sections, each on its own line, in this format:

        ROOT_CAUSE: <one or two sentences>
        IMPACT: <one sentence describing the operational impact>
        RESOLUTION_STEPS: <step 1> | <step 2> | <step 3>
        CONFIDENCE: <a number between 0.0 and 1.0>

        Base CONFIDENCE on how well the provided context matches this failure.
        If no relevant context was provided, use a low confidence (<= 0.3) and
        still provide your best-effort general guidance.
        Do not include any other text, headers, or explanation.
        """;

    public static string BuildUserPrompt(
        DiagnosisRequest request, IReadOnlyList<VectorSearchResult> context)
    {
        var sb = new StringBuilder();
        sb.Append("Job failure:\n");
        sb.Append("Error: ").Append(request.ErrorMessage).Append('\n');

        if (!string.IsNullOrWhiteSpace(request.StackTrace))
        {
            sb.Append("Stack trace: ").Append(request.StackTrace).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(request.ProcessName))
        {
            sb.Append("Process: ").Append(request.ProcessName).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(request.RobotName))
        {
            sb.Append("Robot: ").Append(request.RobotName).Append('\n');
        }

        if (context.Count > 0)
        {
            sb.Append("\nRelevant historical context:\n");
            foreach (var item in context)
            {
                sb.Append("- (score ").Append(item.Score.ToString("F2", CultureInfo.InvariantCulture)).Append(") ")
                  .Append(item.Content).Append('\n');
            }
        }
        else
        {
            sb.Append("\nNo relevant historical context was found.\n");
        }

        return sb.ToString();
    }
}
