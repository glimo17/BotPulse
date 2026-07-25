using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Infrastructure.Persistence;
using BotPulse.Infrastructure.Persistence.Repositories;
using BotPulse.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BotPulse.IntegrationTests.Persistence;

[Collection("PostgreSql")]
public sealed class AuditRepositoryTests(PostgreSqlContainerFixture fixture)
{
    [Fact]
    public async Task RecordAsync_ShouldPersistAuditEntry()
    {
        await using var ctx = fixture.CreateDbContext();
        var repo = new AuditRepository(ctx);

        var record = new AuditRecordData(
            UserId: "user-1", UserName: "alice", Action: "StartJob",
            ResourceType: "Job", ResourceId: "job-ext-1", Outcome: "Success",
            IpAddress: "127.0.0.1", CorrelationId: Guid.NewGuid().ToString());

        await repo.RecordAsync(record);

        var results = await repo.QueryAsync(new AuditQuery(UserId: "user-1", Action: "StartJob"));
        results.Should().Contain(r => r.UserId == "user-1" && r.Action == "StartJob");
    }

    [Fact]
    public async Task AuditRecord_ShouldBeAppendOnly_NoUpdateApi()
    {
        // Verify that IAuditRepository only exposes RecordAsync and QueryAsync (no Update/Delete)
        var methods = typeof(IAuditRepository).GetMethods()
            .Select(m => m.Name)
            .ToHashSet();

        methods.Should().Contain("RecordAsync");
        methods.Should().Contain("QueryAsync");
        methods.Should().NotContain("UpdateAsync");
        methods.Should().NotContain("DeleteAsync");
    }
}
