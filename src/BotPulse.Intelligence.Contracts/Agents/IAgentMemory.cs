namespace BotPulse.Intelligence.Contracts.Agents;

/// <summary>
/// Short-term / long-term memory abstraction for an agent conversation or session.
/// </summary>
public interface IAgentMemory
{
    Task SaveAsync(string sessionId, string key, string value, CancellationToken ct = default);
    Task<string?> GetAsync(string sessionId, string key, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(string sessionId, CancellationToken ct = default);
}
