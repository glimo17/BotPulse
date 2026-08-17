using BotPulse.Intelligence.Contracts.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotPulse.Intelligence.Providers.DependencyInjection;

/// <summary>
/// Registers concrete Intelligence provider implementations (chat, embedding,
/// vector store) selected by the "Intelligence" configuration section.
/// This is the only place that references concrete provider types.
/// </summary>
public static class ProvidersServiceCollectionExtensions
{
    /// <summary>
    /// Binds IntelligenceOptions and registers the configured providers.
    /// Concrete provider wiring is added in later milestones.
    /// </summary>
    public static IServiceCollection AddIntelligenceProviders(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntelligenceOptions>(
            configuration.GetSection(IntelligenceOptions.SectionName));

        // Provider selection (chat / embedding / vector store) is wired
        // in Milestones 2-4 based on IntelligenceOptions.
        return services;
    }
}
