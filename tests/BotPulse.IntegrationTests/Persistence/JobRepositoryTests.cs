using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Infrastructure.Persistence;
using BotPulse.Infrastructure.Persistence.Repositories;
using BotPulse.IntegrationTests.Infrastructure;
using FluentAssertions;

namespace BotPulse.IntegrationTests.Persistence;

[Collection("PostgreSql")]
public sealed class JobRepositoryTests(PostgreSqlContainerFixture fixture)
{
    private static JobSnapshot MakeSnapshot(string id, string status = "Running") =>
        new(id, "proc-1", "robot-1", null, status,
            DateTime.UtcNow.AddMinutes(-10), null, null, null, null);

    private (IJobRepository repo, BotPulseDbContext ctx) CreateRepository()
    {
        var ctx = fixture.CreateDbContext();
        return (new JobRepository(ctx), ctx);
    }

    [Fact]
    public async Task UpsertAsync_NewJob_ShouldPersist()
    {
        var (repo, ctx) = CreateRepository();
        await using (ctx)
        {
            var snapshot = MakeSnapshot($"upsert-new-{Guid.NewGuid():N}");
            await repo.UpsertAsync(snapshot, "UiPath");
            await ctx.SaveChangesAsync();

            var saved = await repo.GetByExternalIdAsync("UiPath", snapshot.ExternalId);
            saved.Should().NotBeNull();
            saved!.ExternalJobId.Should().Be(snapshot.ExternalId);
        }
    }

    [Fact]
    public async Task UpsertAsync_ExistingJob_ShouldUpdate()
    {
        var (repo, ctx) = CreateRepository();
        await using (ctx)
        {
            var externalId = $"upsert-update-{Guid.NewGuid():N}";
            var initial = MakeSnapshot(externalId, "Running");
            await repo.UpsertAsync(initial, "UiPath");
            await ctx.SaveChangesAsync();

            var terminal = MakeSnapshot(externalId, "Success");
            await repo.UpsertAsync(terminal, "UiPath");
            await ctx.SaveChangesAsync();

            var saved = await repo.GetByExternalIdAsync("UiPath", externalId);
            saved!.Status.Value.Should().Be("Success");
        }
    }

    [Fact]
    public async Task UpsertAsync_IsIdempotent_SameSnapshotTwice()
    {
        var (repo, ctx) = CreateRepository();
        await using (ctx)
        {
            var externalId = $"idempotent-{Guid.NewGuid():N}";
            var snapshot = MakeSnapshot(externalId, "Running");

            await repo.UpsertAsync(snapshot, "UiPath");
            await ctx.SaveChangesAsync();
            await repo.UpsertAsync(snapshot, "UiPath");
            await ctx.SaveChangesAsync();

            var all = await repo.FindAllAsync(j => j.ExternalJobId == externalId && j.ProviderName == "UiPath");
            all.Should().HaveCount(1);
        }
    }

    [Fact]
    public async Task QueryAsync_FilterByStatus_ShouldReturnMatchingJobs()
    {
        var (repo, ctx) = CreateRepository();
        await using (ctx)
        {
            var prefix = Guid.NewGuid().ToString("N")[..8];
            await repo.UpsertAsync(MakeSnapshot($"{prefix}-1", "Success"), "UiPath");
            await repo.UpsertAsync(MakeSnapshot($"{prefix}-2", "Failed"), "UiPath");
            await repo.UpsertAsync(MakeSnapshot($"{prefix}-3", "Success"), "UiPath");
            await ctx.SaveChangesAsync();

            var (items, total) = await repo.QueryAsync(new JobFilter(Status: "Success"));
            items.Should().Contain(j => j.ExternalJobId.StartsWith(prefix) && j.Status.Value == "Success");
        }
    }

    [Fact]
    public async Task TerminalJob_ShouldNotBeUpdated()
    {
        var (repo, ctx) = CreateRepository();
        await using (ctx)
        {
            var externalId = $"terminal-{Guid.NewGuid():N}";
            var success = MakeSnapshot(externalId, "Success");
            await repo.UpsertAsync(success, "UiPath");
            await ctx.SaveChangesAsync();

            // Try to overwrite a terminal job with Running
            var running = MakeSnapshot(externalId, "Running");
            await repo.UpsertAsync(running, "UiPath");
            await ctx.SaveChangesAsync();

            var saved = await repo.GetByExternalIdAsync("UiPath", externalId);
            saved!.Status.Value.Should().Be("Success");
        }
    }
}
