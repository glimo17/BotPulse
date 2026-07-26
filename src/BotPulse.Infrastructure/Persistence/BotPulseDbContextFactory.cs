using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BotPulse.Infrastructure.Persistence;

/// <summary>Design-time factory for EF Core migrations. Not used at runtime.</summary>
internal sealed class BotPulseDbContextFactory : IDesignTimeDbContextFactory<BotPulseDbContext>
{
    public BotPulseDbContext CreateDbContext(string[] args)
    {
        // Prefer environment variable set in the shell (e.g. from .env or CI).
        // Falls back to a local dev default that matches docker-compose.yml.
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__PostgreSQL")
            ?? "Host=localhost;Port=5432;Database=botpulse;Username=botpulse;Password=botpulse_dev_2024";

        var options = new DbContextOptionsBuilder<BotPulseDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new BotPulseDbContext(options);
    }
}
