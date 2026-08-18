using BotPulse.Intelligence.Caching;
using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Knowledge;
using BotPulse.Intelligence.Knowledge;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BotPulse.Intelligence.DependencyInjection;

/// <summary>
/// Key used to register the raw (uncached) embedding provider so that
/// CachedEmbeddingProvider can wrap it without triggering a self-referencing
/// resolution.
/// </summary>
internal static class IntelligenceServiceKeys
{
    public const string RawEmbeddingProvider = "raw-embedding-provider";
}

/// <summary>
/// Registers the Intelligence Platform core services (orchestration, pipelines).
/// Provider implementations are registered separately via AddIntelligenceProviders.
/// </summary>
public static class IntelligenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers core Intelligence services and wraps the concrete embedding
    /// provider (registered by AddIntelligenceProviders under a keyed
    /// registration) with caching (ADR-016 NFR-01).
    /// </summary>
    public static IServiceCollection AddIntelligence(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddScoped<IEmbeddingProvider>(sp =>
        {
            var inner = sp.GetRequiredKeyedService<IEmbeddingProvider>(
                IntelligenceServiceKeys.RawEmbeddingProvider);
            var cache = sp.GetRequiredService<IMemoryCache>();
            return new CachedEmbeddingProvider(inner, cache);
        });

        services.AddScoped<IKnowledgeBase, KnowledgeBaseService>();

        // RagDiagnosticService, agent/tool registries are registered in
        // later milestones.
        return services;
    }
}
