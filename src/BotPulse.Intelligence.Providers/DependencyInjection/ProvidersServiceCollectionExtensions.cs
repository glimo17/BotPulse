using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Configuration;
using BotPulse.Intelligence.Contracts.Vector;
using BotPulse.Intelligence.DependencyInjection;
using BotPulse.Intelligence.Providers.InMemory;
using BotPulse.Intelligence.Providers.Ollama;
using BotPulse.Intelligence.Providers.OpenAI;
using BotPulse.Intelligence.Providers.PgVector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BotPulse.Intelligence.Providers.DependencyInjection;

/// <summary>
/// Registers concrete Intelligence provider implementations (chat, embedding,
/// vector store) selected by the "Intelligence" configuration section.
/// This is the only place that references concrete provider types — the rest
/// of BotPulse only ever sees the Contracts abstractions (ADR-016 §4).
/// The raw embedding provider is registered as a keyed service so that
/// AddIntelligence can wrap it with caching without a circular resolution.
/// </summary>
public static class ProvidersServiceCollectionExtensions
{
    /// <summary>
    /// Binds IntelligenceOptions and registers the configured chat, embedding,
    /// and vector store providers. Call AddIntelligence() after this to get
    /// the cached, public IEmbeddingProvider / IChatCompletionProvider
    /// registrations.
    /// </summary>
    public static IServiceCollection AddIntelligenceProviders(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntelligenceOptions>(
            configuration.GetSection(IntelligenceOptions.SectionName));

        var section = configuration.GetSection(IntelligenceOptions.SectionName);
        var chatProvider = section["ChatProvider"] ?? "Ollama";
        var embeddingProvider = section["EmbeddingProvider"] ?? "Ollama";
        var vectorStore = section["VectorStore"] ?? "InMemory";

        RegisterChatProvider(services, chatProvider);
        RegisterEmbeddingProvider(services, embeddingProvider);
        RegisterVectorStore(services, vectorStore);

        return services;
    }

    private static void RegisterChatProvider(IServiceCollection services, string provider)
    {
        services.AddHttpClient();

        switch (provider)
        {
            case "OpenAI":
                services.AddKeyedScoped<IChatCompletionProvider>(
                    IntelligenceServiceKeys.RawChatProvider,
                    (sp, _) => new OpenAIChatCompletionProvider(
                        sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenAIChatCompletionProvider)),
                        sp.GetRequiredService<IOptions<IntelligenceOptions>>()));
                break;
            case "Ollama":
            default:
                services.AddKeyedScoped<IChatCompletionProvider>(
                    IntelligenceServiceKeys.RawChatProvider,
                    (sp, _) => new OllamaChatCompletionProvider(
                        sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OllamaChatCompletionProvider)),
                        sp.GetRequiredService<IOptions<IntelligenceOptions>>()));
                break;
        }
    }

    private static void RegisterEmbeddingProvider(IServiceCollection services, string provider)
    {
        services.AddHttpClient();

        switch (provider)
        {
            case "OpenAI":
                services.AddKeyedScoped<IEmbeddingProvider>(
                    IntelligenceServiceKeys.RawEmbeddingProvider,
                    (sp, _) => new OpenAIEmbeddingProvider(
                        sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenAIEmbeddingProvider)),
                        sp.GetRequiredService<IOptions<IntelligenceOptions>>()));
                break;
            case "Ollama":
            default:
                services.AddKeyedScoped<IEmbeddingProvider>(
                    IntelligenceServiceKeys.RawEmbeddingProvider,
                    (sp, _) => new OllamaEmbeddingProvider(
                        sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OllamaEmbeddingProvider)),
                        sp.GetRequiredService<IOptions<IntelligenceOptions>>()));
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
