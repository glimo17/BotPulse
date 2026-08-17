namespace BotPulse.Intelligence.Contracts.Vector;

/// <summary>
/// Abstraction over a vector store. Implementations (pgvector, Pinecone, Qdrant,
/// in-memory) are interchangeable and selected by configuration.
/// All queries are tenant-scoped via OrganizationId to prevent cross-tenant leakage.
/// </summary>
public interface IVectorStore
{
    /// <summary>Inserts or updates a single vector record (idempotent by Id).</summary>
    Task UpsertAsync(VectorRecord record, CancellationToken ct = default);

    /// <summary>Inserts or updates a batch of vector records.</summary>
    Task UpsertBatchAsync(IReadOnlyList<VectorRecord> records, CancellationToken ct = default);

    /// <summary>Returns the most similar records to the query embedding.</summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(VectorQuery query, CancellationToken ct = default);

    /// <summary>Removes a record by Id.</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);
}
