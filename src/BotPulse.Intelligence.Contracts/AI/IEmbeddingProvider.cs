namespace BotPulse.Intelligence.Contracts.AI;

/// <summary>
/// Abstraction over an embedding generation provider.
/// Implementations are interchangeable and selected by configuration.
/// </summary>
public interface IEmbeddingProvider
{
    /// <summary>Provider identifier (e.g. "OpenAI", "Ollama").</summary>
    string Name { get; }

    /// <summary>Dimensionality of the embeddings produced by this provider.</summary>
    int Dimensions { get; }

    /// <summary>Generates an embedding for a single piece of text.</summary>
    Task<EmbeddingResult> EmbedAsync(string text, CancellationToken ct = default);

    /// <summary>Generates embeddings for a batch of texts.</summary>
    Task<IReadOnlyList<EmbeddingResult>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct = default);
}
