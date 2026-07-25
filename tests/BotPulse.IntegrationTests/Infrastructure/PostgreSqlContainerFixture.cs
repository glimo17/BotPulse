using BotPulse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace BotPulse.IntegrationTests.Infrastructure;

/// <summary>
/// Manages a PostgreSQL Testcontainer for integration tests.
/// The container is shared across all tests in the same collection.
/// </summary>
public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:15-alpine")
        .WithDatabase("botpulse_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public BotPulseDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BotPulseDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;
        return new BotPulseDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations to the test database
        await using var ctx = CreateDbContext();
        await ctx.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

[CollectionDefinition("PostgreSql")]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture> { }
