namespace BotPulse.Intelligence.Contracts.Agents;

/// <summary>Input for an agent run.</summary>
public sealed record AgentRequest(
    string Input,
    IReadOnlyDictionary<string, string>? Parameters = null,
    Guid? OrganizationId = null,
    Guid? UserId = null);

/// <summary>Result of an agent run.</summary>
public sealed record AgentResult(
    bool Success,
    string Output,
    IReadOnlyList<string>? ProposedActions = null,
    string? Error = null);

/// <summary>
/// A specialized AI agent (diagnostic, monitoring, recommendation, self-healing, etc.).
/// Agents share common infrastructure and are resolved by name from the registry.
/// </summary>
public interface IAgent
{
    /// <summary>Unique agent name.</summary>
    string Name { get; }

    /// <summary>Executes the agent's task.</summary>
    Task<AgentResult> RunAsync(AgentRequest request, CancellationToken ct = default);
}
