namespace BotPulse.Intelligence.Contracts.Diagnostics;

/// <summary>Input for an AI-assisted job failure diagnosis.</summary>
public sealed record DiagnosisRequest(
    string JobId,
    string ErrorMessage,
    string? StackTrace = null,
    string? ProcessName = null,
    string? RobotName = null,
    Guid? OrganizationId = null);

/// <summary>Result of an AI-assisted diagnosis.</summary>
public sealed record DiagnosisResult(
    string RootCause,
    string Impact,
    IReadOnlyList<string> ResolutionSteps,
    double Confidence,
    IReadOnlyList<string> ReferencedKnowledgeIds);
