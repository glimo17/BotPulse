namespace BotPulse.Intelligence.Contracts.Diagnostics;

/// <summary>
/// Persists operator feedback on AI-generated diagnoses (Requisito 5.5).
/// Validated diagnoses feed the knowledge base's learning loop; rejected ones
/// are recorded but never re-indexed as trusted knowledge.
/// Owned by the Intelligence Platform's own persistence (ADR-016 §3).
/// </summary>
public interface IDiagnosisFeedbackStore
{
    /// <summary>Records an operator's validate/reject signal for a diagnosis.</summary>
    Task RecordAsync(DiagnosisFeedback feedback, CancellationToken ct = default);
}

/// <summary>A single operator feedback record on a diagnosis.</summary>
public sealed record DiagnosisFeedback(
    string JobId,
    DiagnosisResult Diagnosis,
    bool Validated,
    Guid? OrganizationId = null);
