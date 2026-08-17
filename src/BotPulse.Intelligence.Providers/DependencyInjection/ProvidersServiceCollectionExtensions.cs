using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Configuration;
using BotPulse.Intelligence.Contracts.Vector;
using BotPulse.Intelligence.Providers.InMemory;
using BotPulse.Intelligence.Providers.Ollama;
using BotPulse.Intelligence.Providers.OpenAI;
using BotPulse.Intelligence.Providers.PgVector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotPulse.Intelligence.Providers.DependencyInjection;

/// <summary>
/// Registers concrete Intelligence provider implementations (chat, embedding,
/// vector store) selected by the "Intelligence" configuration section.
/// This is the only place that references concrete provider types — the rest
/// of BotPulse only ever sees the Contracts abstractions (ADR-016 §4).
/// </summary>
public static class ProvidersServiceCollectionExtensions
{
    /// <summary>
    /// Binds IntelligenceOptions and registers the configured embedding and
    /// vector store providers. Chat provider wiring is added in Milestone 4.
    /// </summary>
    public static IServiceCollection AddIntelligenceProviders(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntelligenceOptions>(
            configuration.GetSection(IntelligenceOptions.SectionName));

        var section = configuration.GetSection(IntelligenceOptions.SectionName);
        var embeddingProvider = section["EmbeddingProvider"] ?? "Ollama";
        var vectorStore = section["VectorStore"] ?? "InMemory";

        RegisterEmbeddingProvider(services, embeddingProvider);
        RegisterVectorStore(services, vectorStore);

        return services;
    }

    private static void RegisterEmbeddingProvider(IServiceCollection services, string provider)
    {
        switch (provider)
        {
            case "OpenAI":
                services.AddHttpClient<OpenAIEmbeddingProvider>();
                services.AddScoped<IEmbeddingProvider, OpenAIEmbeddingProvider>();
                break;
            case "Ollama":
            default:
                services.AddHttpClient<OllamaEmbeddingProvider>();
                services.AddScoped<IEmbeddingProvider, OllamaEmbeddingProvider>();
                break;
        }
    }

    private static void RegisterVectorStore(IServiceCollection services, string store)
    {
        switch (store)
        {
            case "PgVector":
                services.AddScoped<IVectorStore, PgVectorStore>();
                break;
            case "InMemory":
            default:
                services.AddSingleton<IVectorStore, InMemoryVectorStore>();
                break;
        }
    }
}
