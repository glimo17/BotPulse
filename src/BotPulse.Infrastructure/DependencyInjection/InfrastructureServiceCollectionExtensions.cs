using BotPulse.Core.Abstractions.Authentication;
using BotPulse.Core.Abstractions.Caching;
using BotPulse.Core.Abstractions.Notifications;
using BotPulse.Infrastructure.Authentication;
using BotPulse.Infrastructure.Caching;
using BotPulse.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BotPulse.Infrastructure.DependencyInjection;

/// <summary>Extension methods for registering all infrastructure services.</summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Registers cache, authentication providers, session token service and Argon2 hasher.
    /// The authentication provider is selected based on Authentication:Provider configuration.
    /// </summary>
    public static IServiceCollection AddBotPulseInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Cache
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // JWT
        services.Configure<JwtOptions>(configuration.GetSection("Jwt"));
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddSingleton<ISessionTokenService, JwtSessionTokenService>();

        // Password hasher (no longer needs DI injection - methods are static)
        // Argon2idPasswordHasher is used directly via static calls in LocalAuthenticationProvider

        // Pluggable authentication
        AddPluggableAuthentication(services, configuration);

        // Pluggable notification delivery
        AddPluggableNotificationDelivery(services, configuration);

        return services;
    }

    private static void AddPluggableAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var providerName = configuration["Authentication:Provider"]
            ?? throw new InvalidOperationException(
                "AUTHENTICATION_PROVIDER is required. Set Authentication__Provider environment variable. Valid values: Local, EntraID, LDAP.");

        switch (providerName)
        {
            case "Local":
                services.AddScoped<IAuthenticationProvider, LocalAuthenticationProvider>();
                break;
            case "EntraID":
                services.AddScoped<IAuthenticationProvider, EntraIdAuthenticationProvider>();
                break;
            case "LDAP":
                services.AddScoped<IAuthenticationProvider, LdapAuthenticationProvider>();
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported authentication provider: '{providerName}'. Valid values: Local, EntraID, LDAP.");
        }
    }

    private static void AddPluggableNotificationDelivery(IServiceCollection services, IConfiguration configuration)
    {
        var transport = configuration["Notifications:Transport"] ?? "SSE";
        switch (transport)
        {
            case "Polling":
                services.AddSingleton<PollingNotificationDelivery>();
                services.AddSingleton<INotificationDelivery>(sp => sp.GetRequiredService<PollingNotificationDelivery>());
                break;
            default: // SSE
                services.AddSingleton<SseNotificationDelivery>();
                services.AddSingleton<INotificationDelivery>(sp => sp.GetRequiredService<SseNotificationDelivery>());
                break;
        }
    }
}
