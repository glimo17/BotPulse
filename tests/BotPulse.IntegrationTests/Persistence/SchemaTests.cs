using BotPulse.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BotPulse.IntegrationTests.Persistence;

[Collection("PostgreSql")]
public sealed class SchemaTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task Database_ShouldHaveAllRequiredTables()
    {
        await using var ctx = fixture.CreateDbContext();
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync();

        var expectedTables = new[]
        {
            "jobs", "queue_items", "execution_logs", "metrics_raw", "metrics_rollups",
            "alerts", "alert_rules", "users", "dashboard_layouts", "audit_records"
        };

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
            """;

        var existingTables = new List<string>();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            existingTables.Add(reader.GetString(0));

        existingTables.Should().Contain(expectedTables);
    }

    [Fact]
    public async Task Jobs_ShouldHaveUniqueIndexOnProviderAndExternalId()
    {
        await using var ctx = fixture.CreateDbContext();
        var conn = ctx.Database.GetDbConnection();
        await conn.OpenAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT indexname FROM pg_indexes
            WHERE tablename = 'jobs' AND indexname = 'idx_jobs_provider_external_unique'
            """;

        var result = await cmd.ExecuteScalarAsync();
        result.Should().Be("idx_jobs_provider_external_unique");
    }
}
