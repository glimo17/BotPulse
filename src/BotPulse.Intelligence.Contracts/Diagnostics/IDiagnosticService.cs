namespace BotPulse.Intelligence.Contracts.Diagnostics;

/// <summary>
/// RAG-based diagnostic service. The MVP capability of the Intelligence Platform.
/// This is the only Intelligence contract the Core needs to know about for diagnosis.
/// </summary>
public interface IDiagnosticService
{
    /// <summary>Produces an AI-assisted diagnosis for a failed job.</summary>
    Task<DiagnosisResult> DiagnoseAsync(DiagnosisRequest request, CancellationToken ct = default);
}
