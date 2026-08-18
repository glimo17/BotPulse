using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Configuration;
using BotPulse.Intelligence.Contracts.Knowledge;
using BotPulse.Intelligence.Contracts.Vector;
using Microsoft.Extensions.Options;

namespace BotPulse.Intelligence.Knowledge;

/// <summary>
/// Default implementation of IKnowledgeBase. Embeds content and stores it in
/// the configured vector store. Indexing is idempotent — items are identified
/// by a deterministic Id, so re-indexing the same source item overwrites
/// rather than duplicates (Requisito 4.3).
/// </summary>
public sealed class KnowledgeBaseService : IKnowledgeBase
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IVectorStore _vectorStore;
    private readonly int _maxContextTokens;

    public KnowledgeBaseService(
        IEmbeddingProvider embeddingProvider,
        IVectorStore vectorStore,
        IOptions<IntelligenceOptions> options)
    {
        _embeddingProvider = embeddingProvider;
        _vectorStore = vectorStore;
        _maxContextTokens = options.Value.MaxContextTokens;
    }

    public async Task IndexAsync(KnowledgeItem item, CancellationToken ct = default)
    {
        var content = TokenTruncator.Truncate(item.Content, _maxContextTokens);
        var embedding = await _embeddingProvider.EmbedAsync(content, ct).ConfigureAwait(false);

        var metadata = new Dictionary<string, string>(item.Metadata)
        {
            ["sourceType"] = item.SourceType.ToString()
        };

        var record = new VectorRecord(
            Id: item.Id,
            Embedding: embedding.Vector,
            Content: content,
            Metadata: metadata,
            OrganizationId: item.OrganizationId);

        await _vectorStore.UpsertAsync(record, ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(
        string query, int topK = 5, Guid? organizationId = null, CancellationToken ct = default)
    {
        var embedding = await _embeddingProvider.EmbedAsync(query, ct).ConfigureAwait(false);

        var vectorQuery = new VectorQuery(
            Embedding: embedding.Vector,
            TopK: topK,
            OrganizationId: organizationId);

        return await _vectorStore.SearchAsync(vectorQuery, ct).ConfigureAwait(false);
    }
}
