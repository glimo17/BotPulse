using BotPulse.Core.Abstractions.Providers;
using BotPulse.Providers.Demo.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BotPulse.Providers.Demo.DependencyInjection;

/// <summary>Extension methods for registering the Demo RPA provider.</summary>
public static class DemoProviderRegistration
{
    /// <summary>
    /// Registers the DemoProvider with all 7 granular provider interfaces.
    /// No external credentials required.
    /// </summary>
    public static IServiceCollection AddDemoProvider(this IServiceCollection services)
    {
        services.AddSingleton<DemoDataSeed>();

        services.AddScoped<IRobotProvider, DemoRobotProvider>();
        services.AddScoped<IJobProvider, DemoJobProvider>();
        services.AddScoped<IQueueProvider, DemoQueueProvider>();
        services.AddScoped<ILogProvider, DemoLogProvider>();
        services.AddScoped<IAssetProvider, DemoAssetProvider>();
        services.AddScoped<IMachineProvider, DemoMachineProvider>();
        services.AddScoped<IProcessProvider, DemoProcessProvider>();

        return services;
    }

    /// <summary>
    /// Adds the Demo provider health check (always Healthy).
    /// </summary>
    public static IHealthChecksBuilder AddDemoHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "rpa-provider",
        params string[] tags)
        => builder.AddCheck<DemoProviderHealthCheck>(name, tags: tags);
}
