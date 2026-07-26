using BotPulse.Core.Application.Alerts;
using BotPulse.Core.Application.Metrics;
using BotPulse.Infrastructure.DependencyInjection;
using BotPulse.Infrastructure.Logging;
using BotPulse.Providers.UiPath.DependencyInjection;
using BotPulse.Worker.Services;

var host = Host.CreateDefaultBuilder(args)
    .UseBotPulseSerilog()
    .ConfigureServices((context, services) =>
    {
        // Persistence (EF Core + PostgreSQL + all repositories)
        services.AddBotPulsePersistence(context.Configuration);

        // UiPath Provider (OAuth2 + typed HTTP clients + all 7 providers)
        services.AddUiPathProvider(context.Configuration);

        // Application services needed by MetricsCollectionService
        services.AddScoped<MetricsAggregationService>();

        // Synchronization service options — bind each service config individually
        // so IOptionsMonitor<SynchronizationOptions>.Get("JobSync") works
        services.Configure<SynchronizationOptions>("JobSync",
            o => context.Configuration.GetSection("Synchronization:JobSync").Bind(o));
        services.Configure<SynchronizationOptions>("QueueItemSync",
            o => context.Configuration.GetSection("Synchronization:QueueItemSync").Bind(o));
        services.Configure<SynchronizationOptions>("LogSync",
            o => context.Configuration.GetSection("Synchronization:LogSync").Bind(o));
        services.Configure<SynchronizationOptions>("MetricsCollection",
            o => context.Configuration.GetSection("Synchronization:MetricsCollection").Bind(o));
        services.Configure<SynchronizationOptions>("AlertEvaluation",
            o => context.Configuration.GetSection("Synchronization:AlertEvaluation").Bind(o));

        // Individual sync services (scoped-per-trigger via IServiceScopeFactory)
        services.AddSingleton<ISynchronizationService, JobSynchronizationService>();
        services.AddSingleton<ISynchronizationService, QueueItemSynchronizationService>();
        services.AddSingleton<ISynchronizationService, LogSynchronizationService>();
        services.AddSingleton<ISynchronizationService, MetricsCollectionService>();
        services.AddSingleton<ISynchronizationService, AlertEvaluationService>();

        // Alert Engine services
        services.AddScoped<AlertEngine>();
        services.AddScoped<EscalationEngine>();
        services.AddScoped<AlertAcknowledgmentService>();
        services.AddScoped<AlertRuleService>();

        // Orchestrator as IHostedService
        services.AddSingleton<SynchronizationOrchestrator>();
        services.AddHostedService(sp => sp.GetRequiredService<SynchronizationOrchestrator>());
    })
    .Build();

host.Run();
