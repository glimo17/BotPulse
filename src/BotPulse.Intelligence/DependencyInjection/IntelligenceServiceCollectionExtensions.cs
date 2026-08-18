using BotPulse.Intelligence.Caching;
using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Diagnostics;
using BotPulse.Intelligence.Contracts.Knowledge;
using BotPulse.Intelligence.Diagnostics;
using BotPulse.Intelligence.Knowledge;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BotPulse.Intelligence.DependencyInjection;

/// <summary>
/// Keys used to register the raw (uncached) providers so their caching
/// decorators can wrap them without triggering a self-referencing resolution.
/// </summary>
internal static class IntelligenceServiceKeys
{
    public const string RawEmbeddingProvider = "raw-embedding-provider";
    public const string RawChatProvider = "raw-chat-provider";
}

/// <summary>
/// Registers the Intelligence Platform core services (orchestration, pipelines).
/// Provider implementations are registered separately via AddIntelligenceProviders.
/// </summary>
public static class IntelligenceServiceCollectionExtensions
{
    /// <summary>
    /// Registers core Intelligence services and wraps the concrete chat and
    /// embedding providers (registered by AddIntelligenceProviders under
    /// keyed registrations) with caching (ADR-016 NFR-01).
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

        services.AddScoped<IChatCompletionProvider>(sp =>
        {
            var inner = sp.GetRequiredKeyedService<IChatCompletionProvider>(
                IntelligenceServiceKeys.RawChatProvider);
            var cache = sp.GetRequiredService<IMemoryCache>();
            return new CachedChatCompletionProvider(inner, cache);
        });

        services.AddScoped<IKnowledgeBase, KnowledgeBaseService>();
        services.AddScoped<IDiagnosticService, RagDiagnosticService>();

        // Agent/tool registries are registered in later milestones.
        return services;
    }
}
