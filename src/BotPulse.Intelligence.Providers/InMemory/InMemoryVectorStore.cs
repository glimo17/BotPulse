using System.Collections.Concurrent;
using BotPulse.Intelligence.Contracts.Vector;

namespace BotPulse.Intelligence.Providers.InMemory;

/// <summary>
/// In-memory vector store using cosine similarity. Intended for unit tests
/// and local development without an external vector database dependency.
/// </summary>
public sealed class InMemoryVectorStore : IVectorStore
{
    private readonly ConcurrentDictionary<string, VectorRecord> _records = new();

    public Task UpsertAsync(VectorRecord record, CancellationToken ct = default)
    {
        _records[record.Id] = record;
        return Task.CompletedTask;
    }

    public Task UpsertBatchAsync(IReadOnlyList<VectorRecord> records, CancellationToken ct = default)
    {
        foreach (var record in records)
        {
            _records[record.Id] = record;
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(VectorQuery query, CancellationToken ct = default)
    {
        var candidates = _records.Values.AsEnumerable();

        if (query.OrganizationId.HasValue)
        {
            candidates = candidates.Where(r => r.OrganizationId == query.OrganizationId);
        }

        if (query.MetadataFilter is { Count: > 0 })
        {
            candidates = candidates.Where(r => query.MetadataFilter.All(kv =>
                r.Metadata.TryGetValue(kv.Key, out var value) &&
                string.Equals(value, kv.Value, StringComparison.Ordinal)));
        }

        var results = candidates
            .Select(r => new VectorSearchResult(r.Id, r.Content, CosineSimilarity(query.Embedding, r.Embedding), r.Metadata))
            .Where(r => r.Score >= query.MinScore)
            .OrderByDescending(r => r.Score)
            .Take(query.TopK)
            .ToList();

        return Task.FromResult<IReadOnlyList<VectorSearchResult>>(results);
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        _records.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>Computes cosine similarity between two vectors of equal length.</summary>
    internal static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Vectors must have the same length to compute similarity.");
        }

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        if (magA == 0 || magB == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
