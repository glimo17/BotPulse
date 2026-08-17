using System.Globalization;
using System.Text;
using System.Text.Json;
using BotPulse.Intelligence.Contracts.Configuration;
using BotPulse.Intelligence.Contracts.Vector;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BotPulse.Intelligence.Providers.PgVector;

/// <summary>
/// PostgreSQL + pgvector implementation of IVectorStore. Owns its own table
/// (ai_knowledge), independent of the operational BotPulse schema (ADR-016 §3).
/// All queries are scoped by OrganizationId to enforce tenant isolation.
/// </summary>
public sealed class PgVectorStore : IVectorStore
{
    private readonly string _connectionString;

    public PgVectorStore(IOptions<IntelligenceOptions> options)
    {
        _connectionString = options.Value.PgVector.ConnectionString;
    }

    public async Task UpsertAsync(VectorRecord record, CancellationToken ct = default)
    {
        await UpsertBatchAsync(new[] { record }, ct).ConfigureAwait(false);
    }

    public async Task UpsertBatchAsync(IReadOnlyList<VectorRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0)
        {
            return;
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        foreach (var record in records)
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO ai_knowledge (id, source_type, content, embedding, metadata, organization_id, created_at_utc)
                VALUES (@id, @source_type, @content, @embedding::vector, @metadata::jsonb, @organization_id, NOW())
                ON CONFLICT (id) DO UPDATE SET
                    content = EXCLUDED.content,
                    embedding = EXCLUDED.embedding,
                    metadata = EXCLUDED.metadata
                """;

            cmd.Parameters.AddWithValue("id", record.Id);
            cmd.Parameters.AddWithValue("source_type", record.Metadata.GetValueOrDefault("sourceType", "Unknown"));
            cmd.Parameters.AddWithValue("content", record.Content);
            cmd.Parameters.AddWithValue("embedding", ToVectorLiteral(record.Embedding));
            cmd.Parameters.AddWithValue("metadata", JsonSerializer.Serialize(record.Metadata));
            cmd.Parameters.AddWithValue("organization_id", record.OrganizationId.HasValue
                ? (object)record.OrganizationId.Value
                : DBNull.Value);

            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(VectorQuery query, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        var sql = new StringBuilder("""
            SELECT id, content, metadata, 1 - (embedding <=> @embedding::vector) AS score
            FROM ai_knowledge
            WHERE 1=1
            """);

        if (query.OrganizationId.HasValue)
        {
            sql.Append(" AND organization_id = @organization_id");
        }

        sql.Append(" ORDER BY embedding <=> @embedding::vector LIMIT @topK");

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = sql.ToString();
        cmd.Parameters.AddWithValue("embedding", ToVectorLiteral(query.Embedding));
        cmd.Parameters.AddWithValue("topK", query.TopK);

        if (query.OrganizationId.HasValue)
        {
            cmd.Parameters.AddWithValue("organization_id", query.OrganizationId.Value);
        }

        var results = new List<VectorSearchResult>();
        await using var reader = await cmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var score = reader.GetDouble(3);
            if (score < query.MinScore)
            {
                continue;
            }

            var metadataJson = reader.GetString(2);
            var metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(metadataJson)
                            ?? new Dictionary<string, string>();

            results.Add(new VectorSearchResult(reader.GetString(0), reader.GetString(1), score, metadata));
        }

        return results;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM ai_knowledge WHERE id = @id";
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    /// <summary>Formats a float array as a pgvector literal: [0.1,0.2,...]</summary>
    private static string ToVectorLiteral(float[] embedding)
    {
        var sb = new StringBuilder("[");
        for (var i = 0; i < embedding.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(embedding[i].ToString(CultureInfo.InvariantCulture));
        }

        sb.Append(']');
        return sb.ToString();
    }
}
