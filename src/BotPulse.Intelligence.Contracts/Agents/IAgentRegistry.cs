namespace BotPulse.Intelligence.Contracts.Agents;

/// <summary>
/// Registry of available agents. New agent types can be registered without
/// modifying existing agents.
/// </summary>
public interface IAgentRegistry
{
    void Register(IAgent agent);
    IAgent? Resolve(string name);
    IReadOnlyCollection<IAgent> All { get; }
}
