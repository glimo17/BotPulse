using BotPulse.Core.Abstractions.Providers;
using BotPulse.Providers.UiPath.Common;
using BotPulse.Providers.UiPath.HealthChecks;
using BotPulse.Providers.UiPath.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace BotPulse.Providers.UiPath.DependencyInjection;

/// <summary>Extension methods for registering the UiPath RPA provider.</summary>
public static class UiPathProviderRegistration
{
    /// <summary>
    /// Registers UiPath provider services: options, HTTP client, OAuth2 token manager,
    /// version negotiator and all 7 granular provider interfaces.
    /// </summary>
    public static IServiceCollection AddUiPathProvider(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<UiPathOptions>(configuration.GetSection("UiPath"));
        services.AddSingleton<IValidateOptions<UiPathOptions>, UiPathOptionsValidator>();

        // Dedicated HttpClient for OAuth2 token requests (no circular dependency)
        services.AddHttpClient<UiPathOAuth2TokenManager>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<UiPathOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        // Typed HttpClient for OData API calls
        services.AddHttpClient<UiPathHttpClient>((sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<UiPathOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        services.AddSingleton<IProviderVersionNegotiator, UiPathVersionNegotiator>();

        // Register all 7 granular provider interfaces
        services.AddScoped<IRobotProvider, UiPathV1RobotProvider>();
        services.AddScoped<IJobProvider, UiPathV1JobProvider>();
        services.AddScoped<IQueueProvider, UiPathV1QueueProvider>();
        services.AddScoped<ILogProvider, UiPathV1LogProvider>();
        services.AddScoped<IAssetProvider, UiPathV1AssetProvider>();
        services.AddScoped<IMachineProvider, UiPathV1MachineProvider>();
        services.AddScoped<IProcessProvider, UiPathV1ProcessProvider>();

        return services;
    }

    /// <summary>
    /// Adds the UiPath OAuth2 reachability health check to the health checks builder.
    /// </summary>
    public static IHealthChecksBuilder AddUiPathHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "rpa-provider",
        params string[] tags)
    {
        return builder.AddCheck<RpaProviderHealthCheck>(name, tags: tags);
    }
}
