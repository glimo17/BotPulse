namespace BotPulse.Intelligence.Contracts.AI;

/// <summary>Role of a chat message participant.</summary>
public enum ChatRole
{
    System,
    User,
    Assistant
}

/// <summary>A single message in a chat completion request.</summary>
public sealed record ChatMessage(ChatRole Role, string Content);

/// <summary>Request for an LLM chat completion.</summary>
public sealed record ChatRequest(
    IReadOnlyList<ChatMessage> Messages,
    double Temperature = 0.2,
    int? MaxTokens = null);

/// <summary>Response from an LLM chat completion.</summary>
public sealed record ChatResponse(
    string Content,
    int PromptTokens,
    int CompletionTokens);

/// <summary>Result of an embedding generation.</summary>
public sealed record EmbeddingResult(
    float[] Vector,
    int Dimensions);
