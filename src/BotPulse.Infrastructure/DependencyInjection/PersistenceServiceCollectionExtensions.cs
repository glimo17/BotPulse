using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Infrastructure.Persistence;
using BotPulse.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotPulse.Infrastructure.DependencyInjection;

/// <summary>Extension methods for registering persistence services.</summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>Registers BotPulse persistence services (DbContext, repositories, UoW, audit).</summary>
    public static IServiceCollection AddBotPulsePersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException(
                "Connection string 'PostgreSQL' is required. Set ConnectionStrings__PostgreSQL environment variable.");

        services.AddDbContextPool<BotPulseDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(BotPulseDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        // Specialized repositories
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<IQueueItemRepository, QueueItemRepository>();
        services.AddScoped<ILogRepository, LogRepository>();
        services.AddScoped<IAlertRepository, AlertRepository>();
        services.AddScoped<IAlertRuleRepository, AlertRuleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDashboardLayoutRepository, DashboardLayoutRepository>();

        // Unit of Work + Audit
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        return services;
    }
}
