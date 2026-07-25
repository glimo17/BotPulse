using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BotPulse.Infrastructure.Persistence;

/// <summary>Design-time factory for EF Core migrations. Not used at runtime.</summary>
internal sealed class BotPulseDbContextFactory : IDesignTimeDbContextFactory<BotPulseDbContext>
{
    public BotPulseDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BotPulseDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=botpulse_design;Username=postgres;Password=postgres")
            .Options;
        return new BotPulseDbContext(options);
    }
}
