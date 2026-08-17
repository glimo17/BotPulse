namespace BotPulse.Intelligence.Contracts.Vector;

/// <summary>A stored vector record with its source content and metadata.</summary>
public sealed record VectorRecord(
    string Id,
    float[] Embedding,
    string Content,
    IReadOnlyDictionary<string, string> Metadata,
    Guid? OrganizationId = null);

/// <summary>A similarity query against the vector store.</summary>
public sealed record VectorQuery(
    float[] Embedding,
    int TopK = 5,
    double MinScore = 0.7,
    Guid? OrganizationId = null,
    IReadOnlyDictionary<string, string>? MetadataFilter = null);

/// <summary>A single similarity search result.</summary>
public sealed record VectorSearchResult(
    string Id,
    string Content,
    double Score,
    IReadOnlyDictionary<string, string> Metadata);
