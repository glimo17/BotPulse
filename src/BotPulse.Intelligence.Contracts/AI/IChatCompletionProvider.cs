namespace BotPulse.Intelligence.Contracts.AI;

/// <summary>
/// Abstraction over an LLM chat completion provider.
/// Implementations (OpenAI, Anthropic, Ollama, Semantic Kernel) are interchangeable
/// and selected by configuration. No provider type crosses this boundary.
/// </summary>
public interface IChatCompletionProvider
{
    /// <summary>Provider identifier (e.g. "OpenAI", "Ollama").</summary>
    string Name { get; }

    /// <summary>Generates a full completion for the given request.</summary>
    Task<ChatResponse> CompleteAsync(ChatRequest request, CancellationToken ct = default);

    /// <summary>Streams the completion token-by-token for responsive UIs.</summary>
    IAsyncEnumerable<string> StreamAsync(ChatRequest request, CancellationToken ct = default);
}
