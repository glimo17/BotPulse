namespace BotPulse.Intelligence.Contracts.Tools;

/// <summary>Context passed to a tool invocation.</summary>
public sealed record ToolContext(
    IReadOnlyDictionary<string, string> Arguments,
    Guid? OrganizationId = null,
    Guid? UserId = null);

/// <summary>Result of a tool invocation.</summary>
public sealed record ToolResult(bool Success, string Output, string? Error = null);

/// <summary>
/// A bounded operation an agent may invoke to interact with BotPulse data.
/// Agents never access infrastructure directly — only through registered tools.
/// </summary>
public interface ITool
{
    /// <summary>Unique tool name used by agents to resolve it.</summary>
    string Name { get; }

    /// <summary>Human-readable description (also used for LLM tool selection).</summary>
    string Description { get; }

    /// <summary>Executes the tool. Every invocation is auditable.</summary>
    Task<ToolResult> InvokeAsync(ToolContext context, CancellationToken ct = default);
}
