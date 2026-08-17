namespace BotPulse.Intelligence.Contracts.Tools;

/// <summary>
/// Registry of available tools. New tools can be registered without modifying
/// the agent framework.
/// </summary>
public interface IToolRegistry
{
    void Register(ITool tool);
    ITool? Resolve(string name);
    IReadOnlyCollection<ITool> All { get; }
}
