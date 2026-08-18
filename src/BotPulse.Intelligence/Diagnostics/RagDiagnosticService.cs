using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Diagnostics;
using BotPulse.Intelligence.Contracts.Knowledge;
using BotPulse.Intelligence.Contracts.Vector;

namespace BotPulse.Intelligence.Diagnostics;

/// <summary>
/// The MVP capability of the Intelligence Platform (ADR-016, Requisito 5):
/// diagnoses a failed job using Retrieval-Augmented Generation.
///
/// Pipeline: embed the failure -> retrieve similar knowledge (tenant-scoped)
/// -> build a grounded prompt -> complete with the LLM -> parse into a
/// structured DiagnosisResult. Never throws on missing context — produces a
/// best-effort, low-confidence diagnosis instead (Requisito 5.4).
/// </summary>
public sealed class RagDiagnosticService : IDiagnosticService
{
    private const int DefaultTopK = 5;
    private const double MinRelevanceScore = 0.6;

    private readonly IKnowledgeBase _knowledgeBase;
    private readonly IChatCompletionProvider _chatProvider;

    public RagDiagnosticService(IKnowledgeBase knowledgeBase, IChatCompletionProvider chatProvider)
    {
        _knowledgeBase = knowledgeBase;
        _chatProvider = chatProvider;
    }

    public async Task<DiagnosisResult> DiagnoseAsync(DiagnosisRequest request, CancellationToken ct = default)
    {
        var query = BuildSearchQuery(request);

        IReadOnlyList<VectorSearchResult> context = await _knowledgeBase
            .SearchAsync(query, DefaultTopK, request.OrganizationId, ct)
            .ConfigureAwait(false);

        // Only keep results that clear the relevance bar — low-relevance
        // matches would mislead the LLM more than help it (Requisito 5.4).
        var relevantContext = context.Where(r => r.Score >= MinRelevanceScore).ToList();

        var chatRequest = new ChatRequest(
            Messages: new[]
            {
                new ChatMessage(ChatRole.System, DiagnosisPromptBuilder.SystemPrompt),
                new ChatMessage(ChatRole.User, DiagnosisPromptBuilder.BuildUserPrompt(request, relevantContext))
            },
            Temperature: 0.2);

        var response = await _chatProvider.CompleteAsync(chatRequest, ct).ConfigureAwait(false);
        var (rootCause, impact, steps, confidence) = DiagnosisResponseParser.Parse(response.Content);

        // Confidence is capped when no grounding context was found, even if
        // the LLM reports otherwise — the platform's own signal takes
        // precedence over a possibly overconfident model.
        if (relevantContext.Count == 0)
        {
            confidence = Math.Min(confidence, 0.3);
        }

        return new DiagnosisResult(
            RootCause: rootCause,
            Impact: impact,
            ResolutionSteps: steps,
            Confidence: confidence,
            ReferencedKnowledgeIds: relevantContext.Select(r => r.Id).ToList());
    }

    private static string BuildSearchQuery(DiagnosisRequest request) =>
        string.IsNullOrWhiteSpace(request.StackTrace)
            ? request.ErrorMessage
            : $"{request.ErrorMessage} {request.StackTrace}";
}
