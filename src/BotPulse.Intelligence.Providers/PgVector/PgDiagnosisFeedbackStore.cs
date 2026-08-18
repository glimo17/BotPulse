using System.Text.Json;
using BotPulse.Intelligence.Contracts.Configuration;
using BotPulse.Intelligence.Contracts.Diagnostics;
using Microsoft.Extensions.Options;
using Npgsql;

namespace BotPulse.Intelligence.Providers.PgVector;

/// <summary>
/// PostgreSQL implementation of IDiagnosisFeedbackStore, backed by the
/// ai_diagnosis_feedback table owned by the Intelligence Platform (ADR-016 §3).
/// </summary>
public sealed class PgDiagnosisFeedbackStore : IDiagnosisFeedbackStore
{
    private readonly string _connectionString;

    public PgDiagnosisFeedbackStore(IOptions<IntelligenceOptions> options)
    {
        _connectionString = options.Value.PgVector.ConnectionString;
    }

    public async Task RecordAsync(DiagnosisFeedback feedback, CancellationToken ct = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO ai_diagnosis_feedback (job_id, diagnosis, validated, created_at_utc)
            VALUES (@job_id, @diagnosis::jsonb, @validated, NOW())
            """;

        cmd.Parameters.AddWithValue("job_id", feedback.JobId);
        cmd.Parameters.AddWithValue("diagnosis", JsonSerializer.Serialize(feedback.Diagnosis));
        cmd.Parameters.AddWithValue("validated", feedback.Validated);

        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}
