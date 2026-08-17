using BotPulse.Intelligence.Contracts.Vector;

namespace BotPulse.Intelligence.Contracts.Knowledge;

/// <summary>
/// Knowledge base facade over embedding generation and vector storage.
/// Indexes operational content and performs semantic search.
/// </summary>
public interface IKnowledgeBase
{
    /// <summary>Embeds and stores a knowledge item (idempotent by item Id).</summary>
    Task IndexAsync(KnowledgeItem item, CancellationToken ct = default);

    /// <summary>Semantic search over indexed knowledge, tenant-scoped.</summary>
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query,
        int topK = 5,
        Guid? organizationId = null,
        CancellationToken ct = default);
}
