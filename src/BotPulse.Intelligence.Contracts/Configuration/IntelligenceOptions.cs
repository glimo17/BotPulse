namespace BotPulse.Intelligence.Contracts.Configuration;

/// <summary>
/// Strongly-typed configuration for the Intelligence Platform.
/// Bound from the "Intelligence" configuration section.
/// Provider selection is by name so implementations stay interchangeable.
/// </summary>
public sealed class IntelligenceOptions
{
    public const string SectionName = "Intelligence";

    /// <summary>Active chat provider: "Ollama" | "OpenAI" | "Anthropic".</summary>
    public string ChatProvider { get; set; } = "Ollama";

    /// <summary>Active embedding provider: "Ollama" | "OpenAI".</summary>
    public string EmbeddingProvider { get; set; } = "Ollama";

    /// <summary>Active vector store: "PgVector" | "InMemory".</summary>
    public string VectorStore { get; set; } = "InMemory";

    /// <summary>Max tokens of context injected into a prompt before truncation.</summary>
    public int MaxContextTokens { get; set; } = 4000;

    public OpenAIOptions OpenAI { get; set; } = new();
    public OllamaOptions Ollama { get; set; } = new();
    public PgVectorOptions PgVector { get; set; } = new();
}

/// <summary>OpenAI provider settings.</summary>
public sealed class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
}

/// <summary>Ollama (local) provider settings.</summary>
public sealed class OllamaOptions
{
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string ChatModel { get; set; } = "llama3";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}

/// <summary>pgvector store settings.</summary>
public sealed class PgVectorOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public int Dimensions { get; set; } = 1536;
}
