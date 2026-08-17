using Microsoft.Extensions.DependencyInjection;

namespace BotPulse.Intelligence.DependencyInjection;

/// <summary>
/// Registers the Intelligence Platform core services (orchestration, pipelines).
/// Provider implementations are registered separately via AddIntelligenceProviders.
/// </summary>
public static class IntelligenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers core Intelligence services. Concrete AI/vector providers are
    /// added by the composition root through AddIntelligenceProviders.
    /// </summary>
    public static IServiceCollection AddIntelligence(this IServiceCollection services)
    {
        // Core services (KnowledgeBaseService, RagDiagnosticService, registries)
        // are registered in later milestones.
        return services;
    }
}
