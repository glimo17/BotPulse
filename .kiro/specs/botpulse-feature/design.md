# Design Document - BotPulse (RPA Operations Platform)

## Overview

BotPulse es una **plataforma agnóstica de operaciones RPA** diseñada para monitorear, gestionar, analizar y orquestar entornos RPA empresariales multi-vendor desde un único punto de control. A diferencia de un dashboard vinculado a un único proveedor, BotPulse se construye desde el primer día como una plataforma independiente del vendor, capaz de integrarse con múltiples ecosistemas de automatización.

**UiPath es solo el primer proveedor soportado.** La arquitectura del Core, el modelo de dominio y las abstracciones son deliberadamente genéricas: el Core nunca conoce a UiPath. Interactúa con interfaces granulares (`IRobotProvider`, `IJobProvider`, `IQueueProvider`, `ILogProvider`, `IAssetProvider`, `IMachineProvider`, `IProcessProvider`) que pueden ser implementadas por cualquier proveedor futuro (Power Automate, Blue Prism, Automation Anywhere, Automation 360, etc.) sin modificar la lógica de negocio.

El diseño enfatiza:

- **Vendor Independence**: Provider pattern granular. El Core nunca menciona vendors concretos.
- **Pluggable Everything**: Autenticación, caché, notificaciones en tiempo real, canales de alerta y proveedores RPA son intercambiables mediante DI.
- **Selective Persistence**: Solo se persiste lo que aporta valor histórico o analítico (Jobs, Queue Items, Logs, Métricas, Audit). El resto se lee on-demand.
- **Deployment Flexibility**: El mismo binario se despliega en Docker Compose, Azure App Service, Azure Container Apps, IIS Windows y Linux + Reverse Proxy sin cambios de código.
- **Enterprise-Ready Security**: Autenticación pluggable con Entra ID, LDAP y Local. JWT solo como session token. RBAC obligatorio. Audit log inmutable.
- **Operational First**: Alert Engine con reglas configurables, dashboards con widgets personalizables por usuario y actualizaciones en tiempo real.

Este documento describe el diseño técnico alineado con los 42 requisitos de `requirements.md`.

---

## Product Vision & Architectural Principles

### Product Vision

BotPulse aspira a convertirse en la **capa de operaciones estándar para entornos RPA multi-vendor**, ocupando el espacio de una plataforma neutral entre las herramientas de orquestación propietarias (UiPath Orchestrator, Power Automate Center, Blue Prism Control Room, Automation Anywhere Control Room) y las necesidades operacionales unificadas de las empresas.

**Roadmap de proveedores (informativo):**

| Fase | Proveedor RPA soportado |
|------|-------------------------|
| MVP  | UiPath                  |
| 2    | Power Automate          |
| 3    | Blue Prism              |
| 4    | Automation Anywhere     |

En cada fase se agrega un nuevo proyecto `BotPulse.Providers.<Vendor>` que implementa las interfaces granulares del Core. No se modifica el Core, ni la API, ni la UI, ni los servicios de sincronización.

### Principios Arquitectónicos

1. **Vendor-Agnostic Core**: La palabra "UiPath" (o cualquier otro vendor) NO aparece en `BotPulse.Core`. Todo acoplamiento vendor-específico vive en `BotPulse.Providers.<Vendor>`.
2. **Interface Segregation**: Provider pattern granular. Nada de un `IRpaProvider` monolítico. Cada capacidad tiene su interfaz.
3. **Dependency Inversion**: Los servicios de aplicación dependen de abstracciones (`IJobProvider`, `IAuthenticationProvider`, `ICacheService`, `INotificationDelivery`, `IAlertChannel`), nunca de implementaciones concretas.
4. **Selective Persistence**: Solo se persiste lo que agrega valor histórico. Todo lo demás es read-on-demand del proveedor.
5. **Configuration-Driven**: El binario es único. Todo el comportamiento específico del entorno se configura vía environment variables y `IConfiguration`.
6. **Async-First**: Todo I/O es asíncrono. No hay `.Result` ni `.Wait()` en el código.
7. **Observability**: Logs estructurados con correlation id, health checks diferenciados, métricas operacionales exportables.
8. **Fail Loud, Fail Early**: Errores de configuración crítica impiden el arranque con mensaje descriptivo.

---

## Architecture

### High-Level System Architecture

```
                                          Clients (Browser / API Consumers)
                                                       │
                                                     HTTPS
                                                       │
                                          ┌────────────▼────────────┐
                                          │   Reverse Proxy         │
                                          │   (nginx / traefik /    │
                                          │    IIS / Azure FrontDoor)│
                                          └────────────┬────────────┘
                                                       │
                        ┌──────────────────────────────┼──────────────────────────────┐
                        │                              │                              │
              ┌─────────▼──────────┐         ┌─────────▼──────────┐         ┌────────▼─────────┐
              │  BotPulse.Api      │         │  BotPulse.Api      │  ...    │  BotPulse.Worker │
              │  (Instance N)      │         │  (Instance N+1)    │         │  (IHostedServices)│
              │                    │         │                    │         │                  │
              │  Controllers/V1/   │         │  Controllers/V1/   │         │  JobSyncService  │
              │  Middleware        │         │  Middleware        │         │  QueueSyncService│
              │  Health Checks     │         │  Health Checks     │         │  LogSyncService  │
              │  INotificationDelivery       │  INotificationDelivery       │  MetricsService  │
              └─────────┬──────────┘         └─────────┬──────────┘         │  AlertEngine     │
                        │                              │                    └────────┬─────────┘
                        └──────────────┬───────────────┘                             │
                                       │                                             │
                        ┌──────────────▼───────────────────────────────────────────────┐
                        │                    BotPulse.Core (Domain + Application)      │
                        │                                                              │
                        │  Granular Provider Interfaces (IRobotProvider, IJobProvider, │
                        │  IQueueProvider, ILogProvider, IAssetProvider,               │
                        │  IMachineProvider, IProcessProvider)                         │
                        │                                                              │
                        │  IAuthenticationProvider, INotificationDelivery,             │
                        │  ICacheService, IAlertChannel, IProviderVersionNegotiator    │
                        └──────────────┬───────────────────────────────────────────────┘
                                       │
                ┌──────────────────────┼──────────────────────┬─────────────────────┐
                │                      │                      │                     │
       ┌────────▼─────────┐   ┌────────▼─────────┐  ┌────────▼──────────┐  ┌──────▼──────┐
       │ BotPulse.        │   │ BotPulse.        │  │  BotPulse.        │  │ BotPulse.   │
       │ Infrastructure   │   │ Providers.UiPath │  │  Providers.       │  │ Shared      │
       │                  │   │                  │  │  <FutureVendor>   │  │ (DTOs)      │
       │ EF Core, Repos,  │   │ UiPath v1        │  │  (Power Automate, │  └─────────────┘
       │ Cache impls,     │   │ UiPath v2        │  │   Blue Prism, ..) │
       │ Auth impls,      │   │ OAuth2 Client    │  │                   │
       │ Notification     │   │ Credentials      │  │                   │
       │ impls (SSE,      │   └──────────────────┘  └───────────────────┘
       │ Polling, ...)    │
       └────────┬─────────┘
                │
       ┌────────▼─────────┐         ┌──────────────────┐
       │  PostgreSQL      │         │  Redis           │
       │  (Persisted:     │         │  (Preparado;     │
       │  Jobs, Queue     │         │   no usado en    │
       │  Items, Logs,    │         │   MVP)           │
       │  Metrics, Audit) │         └──────────────────┘
       └──────────────────┘
```

**Puntos clave del diagrama:**

- La UI y los consumidores API hablan solo con el reverse proxy. El proxy enruta a instancias horizontales de `BotPulse.Api`.
- `BotPulse.Worker` es un proceso separado que hospeda `IHostedService`s independientes. Puede escalar horizontalmente o desplegarse en una sola instancia.
- El Core no depende ni conoce a ningún vendor RPA concreto. Solo depende de interfaces granulares.
- Los proveedores concretos (`BotPulse.Providers.UiPath`, etc.) son proyectos separados que implementan una o más interfaces granulares.
- Redis se provisiona en Docker Compose desde el día 1 pero no se usa en MVP (queda listo para caché distribuida, SignalR backplane, sesiones y rate limiting futuros).

### Solution Structure (.NET Projects)

```
BotPulse.sln
├── src/
│   ├── BotPulse.Api/                       # ASP.NET Core API (versionada)
│   │   ├── Controllers/
│   │   │   └── V1/
│   │   │       ├── RobotsController.cs
│   │   │       ├── MachinesController.cs
│   │   │       ├── ProcessesController.cs
│   │   │       ├── AssetsController.cs
│   │   │       ├── JobsController.cs
│   │   │       ├── QueuesController.cs
│   │   │       ├── LogsController.cs
│   │   │       ├── MetricsController.cs
│   │   │       ├── AlertsController.cs
│   │   │       ├── AlertRulesController.cs
│   │   │       ├── DashboardController.cs
│   │   │       ├── AuthController.cs
│   │   │       └── AdminController.cs
│   │   ├── Middleware/
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   ├── ErrorHandlerMiddleware.cs
│   │   │   ├── AuditMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   ├── HealthChecks/
│   │   │   ├── DatabaseHealthCheck.cs
│   │   │   ├── RpaProviderHealthCheck.cs
│   │   │   ├── SynchronizationHealthCheck.cs
│   │   │   └── CacheHealthCheck.cs
│   │   ├── Notifications/                  # INotificationDelivery hosts (SSE endpoint)
│   │   │   └── SseHub.cs
│   │   ├── Versioning/
│   │   │   └── ApiVersioningExtensions.cs
│   │   ├── Program.cs
│   │   ├── Startup.cs
│   │   └── appsettings.json
│   │
│   ├── BotPulse.Core/                      # Domain + Application (vendor-agnostic)
│   │   ├── Domain/
│   │   │   ├── Entities/                   # Solo entidades persistidas
│   │   │   │   ├── Job.cs
│   │   │   │   ├── QueueItem.cs
│   │   │   │   ├── ExecutionLog.cs
│   │   │   │   ├── MetricPoint.cs
│   │   │   │   ├── MetricRollup.cs
│   │   │   │   ├── AuditRecord.cs
│   │   │   │   ├── User.cs
│   │   │   │   ├── Alert.cs
│   │   │   │   ├── AlertRule.cs
│   │   │   │   └── DashboardLayout.cs
│   │   │   ├── ValueObjects/
│   │   │   │   ├── JobStatus.cs
│   │   │   │   ├── AlertSeverity.cs
│   │   │   │   ├── LogSeverity.cs
│   │   │   │   └── Role.cs
│   │   │   └── Events/
│   │   │       ├── JobStateChanged.cs
│   │   │       ├── AlertRaised.cs
│   │   │       └── QueueItemProcessed.cs
│   │   ├── Abstractions/                   # Interfaces granulares
│   │   │   ├── Providers/                  # Provider pattern granular
│   │   │   │   ├── IRobotProvider.cs
│   │   │   │   ├── IJobProvider.cs
│   │   │   │   ├── IQueueProvider.cs
│   │   │   │   ├── ILogProvider.cs
│   │   │   │   ├── IAssetProvider.cs
│   │   │   │   ├── IMachineProvider.cs
│   │   │   │   ├── IProcessProvider.cs
│   │   │   │   └── IProviderVersionNegotiator.cs
│   │   │   ├── Authentication/
│   │   │   │   ├── IAuthenticationProvider.cs
│   │   │   │   └── ISessionTokenService.cs
│   │   │   ├── Notifications/
│   │   │   │   ├── INotificationDelivery.cs
│   │   │   │   └── INotificationThrottler.cs
│   │   │   ├── Caching/
│   │   │   │   └── ICacheService.cs
│   │   │   ├── Alerts/
│   │   │   │   ├── IAlertChannel.cs
│   │   │   │   ├── IAlertRuleEvaluator.cs
│   │   │   │   └── IAlertDeduplicator.cs
│   │   │   ├── Persistence/
│   │   │   │   ├── IRepository.cs
│   │   │   │   ├── IUnitOfWork.cs
│   │   │   │   └── IAuditRepository.cs
│   │   │   └── Time/
│   │   │       └── ISystemClock.cs
│   │   ├── Application/
│   │   │   ├── Robots/
│   │   │   │   └── RobotQueryService.cs
│   │   │   ├── Machines/
│   │   │   │   └── MachineQueryService.cs
│   │   │   ├── Processes/
│   │   │   │   └── ProcessQueryService.cs
│   │   │   ├── Assets/
│   │   │   │   └── AssetQueryService.cs
│   │   │   ├── Jobs/
│   │   │   │   ├── JobQueryService.cs
│   │   │   │   └── JobCommandService.cs
│   │   │   ├── Queues/
│   │   │   │   ├── QueueQueryService.cs
│   │   │   │   └── QueueAnalyticsService.cs
│   │   │   ├── Logs/
│   │   │   │   └── LogQueryService.cs
│   │   │   ├── Metrics/
│   │   │   │   ├── MetricsQueryService.cs
│   │   │   │   └── MetricsAggregationService.cs
│   │   │   ├── Alerts/
│   │   │   │   ├── AlertEngine.cs
│   │   │   │   ├── NotificationRouter.cs
│   │   │   │   └── EscalationEngine.cs
│   │   │   ├── Dashboard/
│   │   │   │   └── DashboardConfigurationService.cs
│   │   │   └── Auth/
│   │   │       └── AuthenticationOrchestrator.cs
│   │   └── Exceptions/
│   │       ├── BotPulseException.cs
│   │       ├── ProviderException.cs
│   │       ├── AuthenticationException.cs
│   │       ├── AuthorizationException.cs
│   │       ├── ValidationException.cs
│   │       └── EntityNotFoundException.cs
│   │
│   ├── BotPulse.Infrastructure/            # Persistence + concrete adapters
│   │   ├── Persistence/
│   │   │   ├── BotPulseDbContext.cs
│   │   │   ├── Configurations/
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   ├── Authentication/
│   │   │   ├── EntraIdAuthenticationProvider.cs
│   │   │   ├── LdapAuthenticationProvider.cs
│   │   │   ├── LocalAuthenticationProvider.cs
│   │   │   └── JwtSessionTokenService.cs
│   │   ├── Caching/
│   │   │   ├── MemoryCacheService.cs          # MVP
│   │   │   ├── RedisCacheService.cs           # futuro
│   │   │   └── DistributedCacheService.cs     # futuro
│   │   ├── Notifications/
│   │   │   ├── SseNotificationDelivery.cs     # MVP
│   │   │   ├── PollingNotificationDelivery.cs # MVP
│   │   │   ├── SignalRNotificationDelivery.cs # futuro
│   │   │   └── WebSocketNotificationDelivery.cs # futuro
│   │   ├── Alerts/
│   │   │   ├── Channels/
│   │   │   │   ├── LogAlertChannel.cs
│   │   │   │   ├── EmailAlertChannel.cs
│   │   │   │   ├── SlackAlertChannel.cs
│   │   │   │   ├── TeamsAlertChannel.cs
│   │   │   │   └── WebhookAlertChannel.cs
│   │   │   └── Rules/
│   │   │       ├── RobotOfflineRule.cs
│   │   │       ├── QueueBacklogRule.cs
│   │   │       ├── JobsFailedInWindowRule.cs
│   │   │       ├── MachineOfflineRule.cs
│   │   │       └── ProcessExecutionTimeRule.cs
│   │   ├── Logging/
│   │   │   └── SerilogConfiguration.cs
│   │   └── DependencyInjection/
│   │       └── InfrastructureServiceCollectionExtensions.cs
│   │
│   ├── BotPulse.Providers.UiPath/          # Renombrado desde BotPulse.UiPath
│   │   ├── V1/                             # Implementación para UiPath API v1
│   │   │   ├── UiPathV1RobotProvider.cs
│   │   │   ├── UiPathV1JobProvider.cs
│   │   │   ├── UiPathV1QueueProvider.cs
│   │   │   ├── UiPathV1LogProvider.cs
│   │   │   ├── UiPathV1AssetProvider.cs
│   │   │   ├── UiPathV1MachineProvider.cs
│   │   │   └── UiPathV1ProcessProvider.cs
│   │   ├── V2/                             # Estructura preparada para v2
│   │   │   └── ...
│   │   ├── Common/
│   │   │   ├── UiPathHttpClient.cs
│   │   │   ├── UiPathOAuth2TokenManager.cs
│   │   │   ├── UiPathVersionNegotiator.cs   # IProviderVersionNegotiator
│   │   │   └── UiPathErrorTranslator.cs
│   │   ├── Models/                         # DTOs específicos de UiPath (privados)
│   │   └── DependencyInjection/
│   │       └── UiPathProviderRegistration.cs
│   │
│   ├── BotPulse.Worker/                    # Background sync services (IHostedService)
│   │   ├── Services/
│   │   │   ├── SynchronizationOrchestrator.cs
│   │   │   ├── JobSynchronizationService.cs
│   │   │   ├── QueueItemSynchronizationService.cs
│   │   │   ├── LogSynchronizationService.cs
│   │   │   ├── MetricsCollectionService.cs
│   │   │   └── AlertEvaluationService.cs
│   │   ├── HostedServices/
│   │   │   └── ScopedBackgroundService.cs
│   │   ├── HealthChecks/
│   │   │   └── SynchronizationServiceHealthCheck.cs
│   │   ├── Program.cs
│   │   └── appsettings.json
│   │
│   └── BotPulse.Shared/                    # DTOs contract compartidos entre capas
│       ├── Dtos/
│       ├── Constants/
│       └── Errors/
│
└── tests/
    ├── BotPulse.UnitTests/
    ├── BotPulse.IntegrationTests/
    └── BotPulse.PropertyTests/
```

### Dependency Flow (Clean Architecture)

```
BotPulse.Api ─────► BotPulse.Core ◄────── BotPulse.Infrastructure
                        ▲                          │
                        │                          │
BotPulse.Worker ────────┤                          ▼
                        │                    PostgreSQL, Redis, IdP,
                        │                    Serilog sinks, HTTP libs
BotPulse.Providers.UiPath (y futuros)
```

- `BotPulse.Core` no referencia a nadie externo excepto BCL y librerías utilitarias puras.
- `BotPulse.Providers.UiPath` referencia solo a `BotPulse.Core` y a librerías HTTP.
- `BotPulse.Infrastructure` referencia solo a `BotPulse.Core` y a implementaciones concretas (EF Core, Serilog, Redis client, Entra ID SDK, LDAP client).
- `BotPulse.Api` y `BotPulse.Worker` referencian a Core, Infrastructure y a los Providers (para registrar DI). Son los únicos ensamblados donde se resuelven las implementaciones concretas.

---

## Data Models and Entities

Solo las siguientes entidades se persisten localmente (ver Sección "Persistencia Selectiva"):

### Job

```csharp
public sealed class Job
{
    public long Id { get; private set; }
    public string ExternalJobId { get; private set; } = default!;
    public string ProviderName { get; private set; } = default!;   // "UiPath" | "PowerAutomate" | ...
    public string ProcessExternalId { get; private set; } = default!;
    public string RobotExternalId { get; private set; } = default!;
    public string? MachineExternalId { get; private set; }
    public JobStatus Status { get; private set; }                  // Pending, Running, Success, Failed, Stopped, Cancelled
    public DateTime StartTimeUtc { get; private set; }
    public DateTime? EndTimeUtc { get; private set; }
    public TimeSpan? Duration { get; private set; }
    public string? ErrorType { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long? RetryOfJobId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

### QueueItem

```csharp
public sealed class QueueItem
{
    public long Id { get; private set; }
    public string ExternalItemId { get; private set; } = default!;
    public string ProviderName { get; private set; } = default!;
    public string QueueName { get; private set; } = default!;
    public QueueItemStatus Status { get; private set; }            // New, InProgress, Success, Failed, Retried, Abandoned
    public int RetryCount { get; private set; }
    public DateTime? ProcessingStartUtc { get; private set; }
    public DateTime? ProcessingEndUtc { get; private set; }
    public string? OutputMetadataJson { get; private set; }
    public long? OriginalItemId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

### ExecutionLog

```csharp
public sealed class ExecutionLog
{
    public long Id { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public LogSeverity Severity { get; private set; }             // Debug, Info, Warn, Error, Fatal
    public string LoggerName { get; private set; } = default!;
    public string Message { get; private set; } = default!;
    public string? JobExternalId { get; private set; }
    public string? RobotExternalId { get; private set; }
    public string? ProcessExternalId { get; private set; }
    public string PropertiesJson { get; private set; } = "{}";
    public string ProviderName { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }
}
```

### MetricPoint / MetricRollup

```csharp
public sealed class MetricPoint
{
    public long Id { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string MetricName { get; private set; } = default!;    // "jobs.success", "jobs.failed", "queue.backlog"
    public double Value { get; private set; }
    public string DimensionsJson { get; private set; } = "{}";    // { robot, process, machine, queue }
    public string ProviderName { get; private set; } = default!;
}

public sealed class MetricRollup
{
    public long Id { get; private set; }
    public DateTime BucketStartUtc { get; private set; }
    public RollupGranularity Granularity { get; private set; }    // Hourly, Daily
    public string MetricName { get; private set; } = default!;
    public double Sum { get; private set; }
    public double Min { get; private set; }
    public double Max { get; private set; }
    public double Avg { get; private set; }
    public long Count { get; private set; }
    public string DimensionsJson { get; private set; } = "{}";
}
```

### AuditRecord

```csharp
public sealed class AuditRecord
{
    public long Id { get; private set; }
    public DateTime TimestampUtc { get; private set; }
    public string UserId { get; private set; } = default!;
    public string UserName { get; private set; } = default!;
    public string Action { get; private set; } = default!;        // "Login", "Logout", "StartJob", "UpdateAlertRule", ...
    public string ResourceType { get; private set; } = default!;
    public string? ResourceId { get; private set; }
    public string Outcome { get; private set; } = default!;       // "Success" | "Denied" | "Error"
    public string? IpAddress { get; private set; }
    public string? DetailsJson { get; private set; }
    public string CorrelationId { get; private set; } = default!;
}
```

### User

```csharp
public sealed class User
{
    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = default!;    // subject claim del IdP, o local user id
    public string UserName { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public Role Role { get; private set; }                        // Viewer, Operator, Administrator
    public string AuthProvider { get; private set; } = default!;  // "EntraID" | "LDAP" | "Local"
    public string? PasswordHash { get; private set; }             // solo Local
    public bool IsActive { get; private set; }
    public DateTime? LastLoginUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}
```

### Alert / AlertRule

```csharp
public sealed class AlertRule
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string RuleType { get; private set; } = default!;      // "RobotOffline", "QueueBacklog", ...
    public bool Enabled { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public string ParametersJson { get; private set; } = "{}";    // umbrales, ventanas
    public string ChannelsJson { get; private set; } = "[]";      // canales configurados
    public bool EscalationEnabled { get; private set; }
    public int EscalationTimeoutMinutes { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
}

public sealed class Alert
{
    public Guid Id { get; private set; }
    public Guid RuleId { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public DateTime RaisedAtUtc { get; private set; }
    public string ConditionDescription { get; private set; } = default!;
    public string AffectedResourceType { get; private set; } = default!;
    public string AffectedResourceId { get; private set; } = default!;
    public bool Acknowledged { get; private set; }
    public string? AcknowledgedBy { get; private set; }
    public DateTime? AcknowledgedAtUtc { get; private set; }
    public int EscalationLevel { get; private set; }
}
```

### DashboardLayout

```csharp
public sealed class DashboardLayout
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string WidgetsJson { get; private set; } = "[]";       // orden, tipos, settings
    public DateTime UpdatedAtUtc { get; private set; }
}
```

### Database Schema (PostgreSQL)

Solo tablas para datos persistidos. **No** existen tablas `robots`, `machines`, `processes`, `assets`, `queues` (esta metadata se lee on-demand del proveedor).

```sql
-- Users
CREATE TABLE users (
    id UUID PRIMARY KEY,
    external_id VARCHAR(255) NOT NULL,
    user_name VARCHAR(255) NOT NULL,
    email VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL,
    auth_provider VARCHAR(50) NOT NULL,
    password_hash VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT true,
    last_login_utc TIMESTAMPTZ,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (auth_provider, external_id)
);
CREATE UNIQUE INDEX idx_users_email ON users(email);

-- Jobs
CREATE TABLE jobs (
    id BIGSERIAL PRIMARY KEY,
    external_job_id VARCHAR(255) NOT NULL,
    provider_name VARCHAR(50) NOT NULL,
    process_external_id VARCHAR(255) NOT NULL,
    robot_external_id VARCHAR(255) NOT NULL,
    machine_external_id VARCHAR(255),
    status VARCHAR(50) NOT NULL,
    start_time_utc TIMESTAMPTZ NOT NULL,
    end_time_utc TIMESTAMPTZ,
    duration INTERVAL,
    error_type VARCHAR(255),
    error_message TEXT,
    retry_of_job_id BIGINT REFERENCES jobs(id),
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (provider_name, external_job_id)
);
CREATE INDEX idx_jobs_status ON jobs(status);
CREATE INDEX idx_jobs_start_time ON jobs(start_time_utc DESC);
CREATE INDEX idx_jobs_robot ON jobs(robot_external_id, start_time_utc DESC);
CREATE INDEX idx_jobs_process ON jobs(process_external_id, start_time_utc DESC);

-- Queue Items
CREATE TABLE queue_items (
    id BIGSERIAL PRIMARY KEY,
    external_item_id VARCHAR(255) NOT NULL,
    provider_name VARCHAR(50) NOT NULL,
    queue_name VARCHAR(255) NOT NULL,
    status VARCHAR(50) NOT NULL,
    retry_count INT NOT NULL DEFAULT 0,
    processing_start_utc TIMESTAMPTZ,
    processing_end_utc TIMESTAMPTZ,
    output_metadata_json JSONB,
    original_item_id BIGINT REFERENCES queue_items(id),
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (provider_name, external_item_id)
);
CREATE INDEX idx_queue_items_queue ON queue_items(queue_name, status);
CREATE INDEX idx_queue_items_status ON queue_items(status, updated_at_utc DESC);

-- Execution Logs
CREATE TABLE execution_logs (
    id BIGSERIAL PRIMARY KEY,
    timestamp_utc TIMESTAMPTZ NOT NULL,
    severity VARCHAR(20) NOT NULL,
    logger_name VARCHAR(255) NOT NULL,
    message TEXT NOT NULL,
    job_external_id VARCHAR(255),
    robot_external_id VARCHAR(255),
    process_external_id VARCHAR(255),
    properties_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    provider_name VARCHAR(50) NOT NULL,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX idx_logs_timestamp ON execution_logs(timestamp_utc DESC);
CREATE INDEX idx_logs_job ON execution_logs(job_external_id, timestamp_utc DESC);
CREATE INDEX idx_logs_severity ON execution_logs(severity, timestamp_utc DESC);

-- Metrics (raw + rollups)
CREATE TABLE metrics_raw (
    id BIGSERIAL PRIMARY KEY,
    timestamp_utc TIMESTAMPTZ NOT NULL,
    metric_name VARCHAR(100) NOT NULL,
    value DOUBLE PRECISION NOT NULL,
    dimensions_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    provider_name VARCHAR(50) NOT NULL
);
CREATE INDEX idx_metrics_raw ON metrics_raw(metric_name, timestamp_utc DESC);

CREATE TABLE metrics_hourly (
    id BIGSERIAL PRIMARY KEY,
    bucket_start_utc TIMESTAMPTZ NOT NULL,
    metric_name VARCHAR(100) NOT NULL,
    sum_value DOUBLE PRECISION NOT NULL,
    min_value DOUBLE PRECISION NOT NULL,
    max_value DOUBLE PRECISION NOT NULL,
    avg_value DOUBLE PRECISION NOT NULL,
    count_value BIGINT NOT NULL,
    dimensions_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    UNIQUE (bucket_start_utc, metric_name, dimensions_json)
);

CREATE TABLE metrics_daily (
    id BIGSERIAL PRIMARY KEY,
    bucket_start_utc DATE NOT NULL,
    metric_name VARCHAR(100) NOT NULL,
    sum_value DOUBLE PRECISION NOT NULL,
    min_value DOUBLE PRECISION NOT NULL,
    max_value DOUBLE PRECISION NOT NULL,
    avg_value DOUBLE PRECISION NOT NULL,
    count_value BIGINT NOT NULL,
    dimensions_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    UNIQUE (bucket_start_utc, metric_name, dimensions_json)
);

-- Alerts
CREATE TABLE alert_rules (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    rule_type VARCHAR(100) NOT NULL,
    enabled BOOLEAN NOT NULL DEFAULT true,
    severity VARCHAR(20) NOT NULL,
    parameters_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    channels_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    escalation_enabled BOOLEAN NOT NULL DEFAULT false,
    escalation_timeout_minutes INT NOT NULL DEFAULT 15,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE alerts (
    id UUID PRIMARY KEY,
    rule_id UUID NOT NULL REFERENCES alert_rules(id),
    severity VARCHAR(20) NOT NULL,
    raised_at_utc TIMESTAMPTZ NOT NULL,
    condition_description TEXT NOT NULL,
    affected_resource_type VARCHAR(100) NOT NULL,
    affected_resource_id VARCHAR(255) NOT NULL,
    acknowledged BOOLEAN NOT NULL DEFAULT false,
    acknowledged_by VARCHAR(255),
    acknowledged_at_utc TIMESTAMPTZ,
    escalation_level INT NOT NULL DEFAULT 0
);
CREATE INDEX idx_alerts_raised ON alerts(raised_at_utc DESC);
CREATE INDEX idx_alerts_ack ON alerts(acknowledged, severity);

-- Dashboard layouts
CREATE TABLE dashboard_layouts (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id),
    widgets_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id)
);

-- Audit log (append-only desde la aplicación)
CREATE TABLE audit_records (
    id BIGSERIAL PRIMARY KEY,
    timestamp_utc TIMESTAMPTZ NOT NULL,
    user_id VARCHAR(255) NOT NULL,
    user_name VARCHAR(255) NOT NULL,
    action VARCHAR(100) NOT NULL,
    resource_type VARCHAR(100) NOT NULL,
    resource_id VARCHAR(255),
    outcome VARCHAR(20) NOT NULL,
    ip_address VARCHAR(50),
    details_json JSONB,
    correlation_id VARCHAR(64) NOT NULL
);
CREATE INDEX idx_audit_timestamp ON audit_records(timestamp_utc DESC);
CREATE INDEX idx_audit_user ON audit_records(user_id, timestamp_utc DESC);
CREATE INDEX idx_audit_action ON audit_records(action, timestamp_utc DESC);
```

**Nota**: `audit_records` es append-only desde el punto de vista de la aplicación. No hay repositorio con `Update` ni `Delete`. La retención se gestiona con jobs de mantenimiento a nivel de infraestructura.

---

## Provider Architecture (Granular)

### Motivación

Un `IRpaProvider` monolítico obligaría a cualquier proveedor futuro a implementar todas las capacidades RPA, incluso las que no soporta. En su lugar, BotPulse define **interfaces granulares por capacidad**. Un proveedor concreto implementa solo aquellas que soporta. Esto respeta Interface Segregation Principle y permite composición.

### Core Provider Interfaces

Todas las interfaces viven en `BotPulse.Core/Abstractions/Providers/`.

```csharp
namespace BotPulse.Core.Abstractions.Providers;

public interface IRobotProvider
{
    Task<IReadOnlyList<RobotSnapshot>> GetRobotsAsync(CancellationToken ct);
    Task<RobotSnapshot?> GetRobotByIdAsync(string externalId, CancellationToken ct);
}

public interface IJobProvider
{
    Task<IReadOnlyList<JobSnapshot>> GetJobsAsync(JobQuery query, CancellationToken ct);
    Task<JobSnapshot?> GetJobByIdAsync(string externalId, CancellationToken ct);
    Task<StartJobResult> StartJobAsync(StartJobRequest request, CancellationToken ct);
    Task StopJobAsync(string externalId, CancellationToken ct);
    Task CancelJobAsync(string externalId, CancellationToken ct);
}

public interface IQueueProvider
{
    Task<IReadOnlyList<QueueSnapshot>> GetQueuesAsync(CancellationToken ct);
    Task<IReadOnlyList<QueueItemSnapshot>> GetQueueItemsAsync(QueueItemQuery query, CancellationToken ct);
}

public interface ILogProvider
{
    Task<IReadOnlyList<ExecutionLogSnapshot>> GetExecutionLogsAsync(LogQuery query, CancellationToken ct);
}

public interface IAssetProvider
{
    // Nunca expone el valor secreto del asset.
    Task<IReadOnlyList<AssetMetadata>> GetAssetsAsync(CancellationToken ct);
}

public interface IMachineProvider
{
    Task<IReadOnlyList<MachineSnapshot>> GetMachinesAsync(CancellationToken ct);
    Task<MachineSnapshot?> GetMachineByIdAsync(string externalId, CancellationToken ct);
}

public interface IProcessProvider
{
    Task<IReadOnlyList<ProcessSnapshot>> GetProcessesAsync(CancellationToken ct);
    Task<IReadOnlyList<ProcessParameter>> GetProcessParametersAsync(string processExternalId, CancellationToken ct);
}
```

Cada interfaz tiene su propio conjunto de DTOs (snapshots) en `BotPulse.Core/Abstractions/Providers/Models/`, definidos de forma neutral (sin referencias a UiPath o cualquier vendor). Ejemplos:

```csharp
public sealed record RobotSnapshot(
    string ExternalId,
    string Name,
    string Status,           // "Online", "Offline", "Idle", "Busy"
    string? MachineExternalId,
    string? LicenseType,
    DateTime LastHeartbeatUtc);

public sealed record JobSnapshot(
    string ExternalId,
    string ProcessExternalId,
    string RobotExternalId,
    string? MachineExternalId,
    string Status,           // "Pending", "Running", "Success", "Failed", "Stopped", "Cancelled"
    DateTime StartTimeUtc,
    DateTime? EndTimeUtc,
    TimeSpan? Duration,
    string? ErrorType,
    string? ErrorMessage);

public sealed record AssetMetadata(
    string ExternalId,
    string Name,
    string Type,             // "Credential", "Config", "GlobalConfig"
    string Scope,
    DateTime LastModifiedUtc);
// El valor secreto del asset NUNCA se expone en este DTO.
```

### UiPath Provider Implementation

`BotPulse.Providers.UiPath` es el primer implementador. Contiene una carpeta por versión (`V1/`, `V2/`) para soportar coexistencia de versiones de UiPath.

```csharp
namespace BotPulse.Providers.UiPath.V1;

internal sealed class UiPathV1JobProvider : IJobProvider
{
    private readonly UiPathHttpClient _http;
    private readonly UiPathOAuth2TokenManager _tokens;
    private readonly ILogger<UiPathV1JobProvider> _logger;

    public UiPathV1JobProvider(
        UiPathHttpClient http,
        UiPathOAuth2TokenManager tokens,
        ILogger<UiPathV1JobProvider> logger)
    {
        _http = http;
        _tokens = tokens;
        _logger = logger;
    }

    public async Task<IReadOnlyList<JobSnapshot>> GetJobsAsync(JobQuery query, CancellationToken ct)
    {
        var token = await _tokens.GetAccessTokenAsync(ct).ConfigureAwait(false);
        var response = await _http.GetOdataAsync<UiPathJobDto>(
            path: "/odata/Jobs",
            query: query.ToOdataFilter(),
            accessToken: token,
            ct: ct).ConfigureAwait(false);

        return response.Select(MapToSnapshot).ToList();
    }

    // ... otros métodos, mapeos y traducción de errores
}
```

Los tipos internos como `UiPathJobDto` viven en `BotPulse.Providers.UiPath/Models/` y **no salen** del ensamblado. La aplicación solo ve `JobSnapshot`.

### Provider Version Negotiation

BotPulse soporta múltiples versiones de un mismo proveedor. La selección se realiza durante el arranque.

```csharp
namespace BotPulse.Core.Abstractions.Providers;

public interface IProviderVersionNegotiator
{
    /// <summary>
    /// Consulta al proveedor su versión de API y devuelve un identificador
    /// que la Provider Factory usará para resolver la implementación.
    /// </summary>
    Task<ProviderVersion> NegotiateAsync(CancellationToken ct);
}

public sealed record ProviderVersion(string ProviderName, string VendorVersion, string SupportedImplementation);
```

Registro en DI:

```csharp
public static class UiPathProviderRegistration
{
    public static IServiceCollection AddUiPathProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UiPathOptions>(configuration.GetSection("UiPath"));
        services.AddHttpClient<UiPathHttpClient>();
        services.AddSingleton<UiPathOAuth2TokenManager>();
        services.AddSingleton<IProviderVersionNegotiator, UiPathVersionNegotiator>();

        // Registro tardío: la implementación concreta se elige en el arranque tras negotiate().
        services.AddSingleton<UiPathProviderFactory>();
        services.AddScoped<IRobotProvider>(sp => sp.GetRequiredService<UiPathProviderFactory>().CreateRobotProvider());
        services.AddScoped<IJobProvider>(sp => sp.GetRequiredService<UiPathProviderFactory>().CreateJobProvider());
        services.AddScoped<IQueueProvider>(sp => sp.GetRequiredService<UiPathProviderFactory>().CreateQueueProvider());
        services.AddScoped<ILogProvider>(sp => sp.GetRequiredService<UiPathProviderFactory>().CreateLogProvider());
        services.AddScoped<IAssetProvider>(sp => sp.GetRequiredService<UiPathProviderFactory>().CreateAssetProvider());
        services.AddScoped<IMachineProvider>(sp => sp.GetRequiredService<UiPathProviderFactory>().CreateMachineProvider());
        services.AddScoped<IProcessProvider>(sp => sp.GetRequiredService<UiPathProviderFactory>().CreateProcessProvider());
        return services;
    }
}
```

`UiPathProviderFactory` mantiene la versión negociada y devuelve la implementación adecuada (`V1` o `V2`).

Si no existe implementación compatible, el `IHostedService` de arranque cancela el startup con `ApplicationLifetime.StopApplication()` y logea un fatal (Requisito 8, criterio 3).

---

## Authentication & Authorization (Pluggable)

### IAuthenticationProvider Abstraction

```csharp
namespace BotPulse.Core.Abstractions.Authentication;

public interface IAuthenticationProvider
{
    string ProviderName { get; }

    /// <summary>
    /// Autentica credenciales y devuelve identidad enriquecida (subject, claims).
    /// </summary>
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct);

    /// <summary>
    /// Health check del proveedor (verifica conectividad al IdP).
    /// </summary>
    Task<bool> IsHealthyAsync(CancellationToken ct);
}

public sealed record AuthenticationRequest(
    string? UserName,
    string? Password,
    string? IdTokenFromExternalIdp,
    IReadOnlyDictionary<string, string> AdditionalParameters);

public sealed record AuthenticationResult(
    bool Succeeded,
    string? ExternalUserId,
    string? UserName,
    string? Email,
    IReadOnlyList<string> Roles,
    string? FailureReason);
```

### Concrete Providers

#### EntraIdAuthenticationProvider (Microsoft Entra ID / Azure AD)

- Implementa el flujo **Authorization Code + PKCE** con OpenID Connect.
- Descarga la configuración desde `{authority}/.well-known/openid-configuration`.
- Valida id_token con las JWKs del tenant, con `iss`, `aud`, `exp`, `nbf`.
- Mapea `role` claims desde grupos de Entra ID (configurable).

```csharp
internal sealed class EntraIdAuthenticationProvider : IAuthenticationProvider
{
    private readonly EntraIdOptions _options;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IEntraIdKeyCache _keyCache;
    private readonly ILogger<EntraIdAuthenticationProvider> _logger;

    public string ProviderName => "EntraID";

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct)
    {
        var idToken = request.IdTokenFromExternalIdp
            ?? throw new AuthenticationException("Entra ID requires id_token.");
        var principal = await _keyCache.ValidateAsync(idToken, _options, ct).ConfigureAwait(false);
        // Map claims to roles per configuration (group->role mapping).
        return new AuthenticationResult(
            Succeeded: true,
            ExternalUserId: principal.FindFirst("sub")?.Value,
            UserName: principal.FindFirst("preferred_username")?.Value,
            Email: principal.FindFirst("email")?.Value,
            Roles: MapGroupsToRoles(principal, _options),
            FailureReason: null);
    }
    // ...
}
```

#### LdapAuthenticationProvider

- Realiza `simple bind` contra el LDAP configurado.
- Búsqueda posterior con service account para obtener DN, email, grupos.
- Mapea grupos LDAP a roles internos (configurable).

#### LocalAuthenticationProvider (Desarrollo)

- Hash con **Argon2id** (preferido) o bcrypt con cost `>= 12`.
- Al arrancar en producción con este proveedor, logea `LogLevel.Warning`.
- Usuarios almacenados en la tabla `users` con `auth_provider = 'Local'`.

```csharp
internal sealed class LocalAuthenticationProvider : IAuthenticationProvider
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public string ProviderName => "Local";

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
            return new AuthenticationResult(false, null, null, null, Array.Empty<string>(), "Missing credentials");

        var user = await _users.FindByUserNameAsync(request.UserName, ct).ConfigureAwait(false);
        if (user is null || !user.IsActive || user.AuthProvider != "Local")
            return new AuthenticationResult(false, null, null, null, Array.Empty<string>(), "Invalid credentials");

        if (!_hasher.Verify(request.Password, user.PasswordHash!))
            return new AuthenticationResult(false, null, null, null, Array.Empty<string>(), "Invalid credentials");

        return new AuthenticationResult(true, user.ExternalId, user.UserName, user.Email,
            new[] { user.Role.ToString() }, null);
    }
    // ...
}
```

### DI Registration Based on Configuration

```csharp
public static IServiceCollection AddPluggableAuthentication(this IServiceCollection services, IConfiguration configuration)
{
    var providerName = configuration["Authentication:Provider"]
        ?? throw new InvalidOperationException("AUTHENTICATION_PROVIDER is required.");

    services.AddSingleton<IPasswordHasher, Argon2idPasswordHasher>();

    switch (providerName)
    {
        case "EntraID":
            services.Configure<EntraIdOptions>(configuration.GetSection("Authentication:EntraID"));
            services.AddSingleton<IAuthenticationProvider, EntraIdAuthenticationProvider>();
            break;
        case "LDAP":
            services.Configure<LdapOptions>(configuration.GetSection("Authentication:LDAP"));
            services.AddSingleton<IAuthenticationProvider, LdapAuthenticationProvider>();
            break;
        case "Local":
            services.AddSingleton<IAuthenticationProvider, LocalAuthenticationProvider>();
            break;
        default:
            throw new InvalidOperationException($"Unsupported authentication provider: {providerName}");
    }
    return services;
}
```

Añadir Okta, Auth0 o Google Workspace requiere solo crear una nueva clase `XxxAuthenticationProvider : IAuthenticationProvider` y un `case` en el switch. **El Core no cambia.**

### JWT como Session Token (Post-Auth Only)

El JWT en BotPulse no es un método de autenticación. Es el token de sesión que se emite **después** de que un `IAuthenticationProvider` valida al usuario.

```csharp
public interface ISessionTokenService
{
    string IssueToken(AuthenticationResult authenticated, string providerName);
    ClaimsPrincipal ValidateToken(string token);
}

internal sealed class JwtSessionTokenService : ISessionTokenService
{
    private readonly JwtOptions _options;

    public string IssueToken(AuthenticationResult authenticated, string providerName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, authenticated.ExternalUserId!),
            new(ClaimTypes.Name, authenticated.UserName!),
            new(ClaimTypes.Email, authenticated.Email ?? ""),
            new("auth_provider", providerName),
        };
        claims.AddRange(authenticated.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Convert.FromBase64String(_options.SigningKeyBase64));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    // ...
}
```

- La clave de firma se carga desde secret store, nunca hardcoded (Requisito 3, criterio 5).
- Expiración configurable entre 15 minutos y 8 horas, default 1 hora (Requisito 3, criterio 2).
- El validador rechaza tokens expirados/manipulados con HTTP 401 (Requisito 3, criterio 4).

### RBAC Implementation

Roles built-in: `Viewer`, `Operator`, `Administrator` (Requisito 4).

```csharp
public static class Policies
{
    public const string RequireOperator      = "RequireOperator";
    public const string RequireAdministrator = "RequireAdministrator";
    public const string ViewAssets           = "ViewAssets";
    public const string ManageAlertRules     = "ManageAlertRules";
    public const string JobActions           = "JobActions";
}

services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RequireOperator, p => p.RequireRole("Operator", "Administrator"));
    options.AddPolicy(Policies.RequireAdministrator, p => p.RequireRole("Administrator"));
    options.AddPolicy(Policies.ViewAssets, p => p.RequireRole("Administrator"));
    options.AddPolicy(Policies.ManageAlertRules, p => p.RequireRole("Administrator"));
    options.AddPolicy(Policies.JobActions, p => p.RequireRole("Operator", "Administrator"));
});
```

Cada decisión de autorización (grant/deny) se registra vía `AuditMiddleware`.

---

## Application Services

Los servicios de aplicación viven en `BotPulse.Core/Application/` y dependen únicamente de las abstracciones granulares. **No conocen a ningún vendor.**

### RobotQueryService (Read On-Demand)

```csharp
public sealed class RobotQueryService
{
    private readonly IRobotProvider _robots;
    private readonly ICacheService _cache;
    private readonly RobotCacheOptions _cacheOptions;

    public async Task<IReadOnlyList<RobotSnapshot>> GetRobotsAsync(bool forceRefresh, CancellationToken ct)
    {
        if (forceRefresh || !_cacheOptions.Enabled)
            return await _robots.GetRobotsAsync(ct).ConfigureAwait(false);

        var key = "robots.all";
        var cached = await _cache.GetAsync<IReadOnlyList<RobotSnapshot>>(key, ct).ConfigureAwait(false);
        if (cached is not null) return cached;

        var fresh = await _robots.GetRobotsAsync(ct).ConfigureAwait(false);
        await _cache.SetAsync(key, fresh, TimeSpan.FromSeconds(_cacheOptions.TtlSeconds), ct).ConfigureAwait(false);
        return fresh;
    }
}
```

Nunca persiste. Cumple Requisitos 9 y 10.

### JobCommandService (Job Actions)

```csharp
public sealed class JobCommandService
{
    private readonly IJobProvider _jobs;
    private readonly IAuditRepository _audit;
    private readonly INotificationDelivery _notifications;

    public async Task<StartJobResult> StartAsync(StartJobRequest request, ClaimsPrincipal user, CancellationToken ct)
    {
        try
        {
            var result = await _jobs.StartJobAsync(request, ct).ConfigureAwait(false);
            await _audit.RecordAsync(AuditRecord.For(user, "StartJob", "Job", result.JobExternalId, "Success"), ct)
                .ConfigureAwait(false);
            await _notifications.PublishAsync(new JobActionRequested(result.JobExternalId, user.UserName()), ct)
                .ConfigureAwait(false);
            return result;
        }
        catch (ProviderException ex)
        {
            await _audit.RecordAsync(AuditRecord.For(user, "StartJob", "Job", request.ProcessExternalId, "Error", ex.Message), ct)
                .ConfigureAwait(false);
            throw;
        }
    }
    // StopAsync, CancelAsync, RetryAsync análogos.
}
```

Cumple Requisito 18.

### Otros servicios de aplicación

Todos siguen el mismo patrón:

| Servicio                        | Interfaz(es) que consume            | Persistencia         |
|---------------------------------|-------------------------------------|----------------------|
| `RobotQueryService`             | `IRobotProvider`                    | No (read-on-demand)  |
| `MachineQueryService`           | `IMachineProvider`                  | No                   |
| `ProcessQueryService`           | `IProcessProvider`                  | No                   |
| `AssetQueryService`             | `IAssetProvider`                    | No                   |
| `JobQueryService`               | Repos locales                       | Sí (Jobs)            |
| `JobCommandService`             | `IJobProvider` + Audit              | Sí (audit)           |
| `QueueQueryService`             | `IQueueProvider` (metadata)         | No                   |
| `QueueAnalyticsService`         | Repos locales                       | Sí (QueueItems)      |
| `LogQueryService`               | Repos locales                       | Sí (ExecutionLogs)   |
| `MetricsQueryService`           | Repos locales                       | Sí (Metrics)         |
| `AlertEngine`                   | Repos + `IAlertChannel`             | Sí (Alerts)          |
| `DashboardConfigurationService` | Repo `DashboardLayout`              | Sí                   |
| `AuthenticationOrchestrator`    | `IAuthenticationProvider` + JWT     | Sí (audit + users)   |

---

## Background Synchronization Services (Independent)

### Motivación

Un mega-worker que hace todas las sincronizaciones en secuencia es frágil: un fallo en una tarea bloquea a las demás; escala mal; es difícil de configurar granularmente. BotPulse divide las sincronizaciones en servicios independientes coordinados por un `SynchronizationOrchestrator`.

### SynchronizationOrchestrator

```csharp
public sealed class SynchronizationOrchestrator : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReadOnlyList<ISynchronizationService> _services;
    private readonly ILogger<SynchronizationOrchestrator> _logger;

    public async Task StartAsync(CancellationToken ct)
    {
        foreach (var service in _services)
        {
            if (service.Options.Enabled)
                _ = service.StartAsync(ct);   // fire and forget; el servicio maneja su loop
            else
                _logger.LogInformation("Sync service {Name} disabled by configuration.", service.Name);
        }
    }

    public IReadOnlyList<SynchronizationServiceStatus> GetStatuses()
        => _services.Select(s => s.CurrentStatus).ToList();

    public Task StopAsync(CancellationToken ct)
        => Task.WhenAll(_services.Select(s => s.StopAsync(ct)));

    public async Task TriggerAsync(string serviceName, ClaimsPrincipal user, CancellationToken ct)
    {
        var svc = _services.First(s => s.Name == serviceName);
        await svc.RunOnceAsync(ct).ConfigureAwait(false);
        // Audit del trigger manual (Requisito 36, criterio 3).
    }
}
```

### Contract: ISynchronizationService

```csharp
public interface ISynchronizationService
{
    string Name { get; }
    SynchronizationOptions Options { get; }
    SynchronizationServiceStatus CurrentStatus { get; }

    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task RunOnceAsync(CancellationToken ct);      // trigger manual

    Task<bool> IsHealthyAsync(CancellationToken ct);
}

public sealed record SynchronizationOptions(bool Enabled, int IntervalSeconds, int BatchSize);

public sealed record SynchronizationServiceStatus(
    string Name,
    DateTime? LastRunUtc,
    string LastOutcome,
    DateTime? NextRunUtc,
    long ItemsProcessedLastRun,
    bool IsHealthy);
```

### Independent Services

Cada uno vive en `BotPulse.Worker/Services/` y ejecuta su propio bucle basado en `PeriodicTimer` y `IServiceScopeFactory` para obtener scopes limpios.

#### JobSynchronizationService

```csharp
public sealed class JobSynchronizationService : ISynchronizationService
{
    public string Name => "JobSync";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<JobSynchronizationService> _logger;
    private readonly SemaphoreSlim _cts = new(1, 1);

    public async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IJobProvider>();
        var repo = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var since = await repo.GetMaxUpdatedAtAsync(ct).ConfigureAwait(false);
        var jobs = await provider.GetJobsAsync(new JobQuery { UpdatedSinceUtc = since }, ct).ConfigureAwait(false);

        foreach (var snapshot in jobs)
            await repo.UpsertAsync(snapshot, ct).ConfigureAwait(false);

        await uow.SaveChangesAsync(ct).ConfigureAwait(false);
        // update status
    }
    // StartAsync/StopAsync gestionan el PeriodicTimer.
}
```

Análogos: `QueueItemSynchronizationService`, `LogSynchronizationService`, `MetricsCollectionService`, `AlertEvaluationService`.

### Concurrent Execution

Los servicios pueden ejecutarse en paralelo. Cada uno resuelve su propio scope, por lo que la coexistencia es segura. Un fallo en uno **no** detiene a los demás (Requisito 35, criterio 3).

### Configuration

```json
{
  "Synchronization": {
    "JobSync":       { "Enabled": true, "IntervalSeconds": 120, "BatchSize": 500 },
    "QueueItemSync": { "Enabled": true, "IntervalSeconds": 180, "BatchSize": 500 },
    "LogSync":       { "Enabled": true, "IntervalSeconds": 60,  "BatchSize": 500 },
    "MetricsCollection": { "Enabled": true, "IntervalSeconds": 300 },
    "AlertEvaluation":   { "Enabled": true, "IntervalSeconds": 60  }
  }
}
```

Vía environment variables:

```
SYNC_JOBS_ENABLED=true
SYNC_JOBS_INTERVAL_SECONDS=120
SYNC_QUEUE_ITEMS_INTERVAL_SECONDS=180
SYNC_LOGS_INTERVAL_SECONDS=60
SYNC_METRICS_INTERVAL_SECONDS=300
```

Cambios en runtime se propagan gracias a `IOptionsMonitor<SynchronizationOptions>` que cada servicio observa. Intervalos < 30 s se clampean a 30 s con warning (Requisito 37, criterio 2).

---

## Real-Time Notifications (Abstraction)

### Motivación

La UI debe recibir actualizaciones sin polling explícito, pero el transporte es intercambiable. En el MVP se usa **Polling** o **SSE** (configurable). Más adelante se agregan **SignalR** y **WebSockets**. La UI depende solo de la abstracción.

### INotificationDelivery Interface

```csharp
namespace BotPulse.Core.Abstractions.Notifications;

public interface INotificationDelivery
{
    Task PublishAsync(NotificationEvent evt, CancellationToken ct);

    IAsyncEnumerable<NotificationEvent> SubscribeAsync(
        NotificationSubscription subscription,
        CancellationToken ct);
}

public sealed record NotificationEvent(
    string EventType,           // "job.state.changed", "alert.raised", "queue.progress"
    string ResourceType,
    string ResourceId,
    string PayloadJson,
    DateTime TimestampUtc);

public sealed record NotificationSubscription(
    string UserId,
    IReadOnlyList<string> EventTypes,
    IReadOnlyList<string>? ResourceIds);
```

### Implementations

| Transporte           | Estado    | Notas                                          |
|----------------------|-----------|------------------------------------------------|
| Polling              | MVP       | HTTP GET `/api/v1/notifications/pull?since=X`  |
| Server-Sent Events   | MVP       | `text/event-stream` en `/api/v1/notifications/stream` |
| SignalR              | Futuro    | Usa Redis backplane cuando esté disponible     |
| WebSockets nativos   | Futuro    | Para clientes no-navegador                     |

```csharp
internal sealed class SseNotificationDelivery : INotificationDelivery
{
    private readonly ConcurrentDictionary<string, Channel<NotificationEvent>> _subscribers = new();
    // ...
}

internal sealed class PollingNotificationDelivery : INotificationDelivery
{
    private readonly INotificationBuffer _buffer;  // Buffer en memoria con TTL
    // ...
}
```

Registro:

```csharp
public static IServiceCollection AddPluggableNotifications(this IServiceCollection services, IConfiguration configuration)
{
    var transport = configuration["Notifications:Transport"] ?? "SSE";
    switch (transport)
    {
        case "SSE":      services.AddSingleton<INotificationDelivery, SseNotificationDelivery>(); break;
        case "Polling":  services.AddSingleton<INotificationDelivery, PollingNotificationDelivery>(); break;
        case "SignalR":  services.AddSingleton<INotificationDelivery, SignalRNotificationDelivery>(); break;
        default: throw new InvalidOperationException($"Unsupported notification transport: {transport}");
    }
    services.AddSingleton<INotificationThrottler, TokenBucketNotificationThrottler>();
    return services;
}
```

### Throttling and Coalescing

Requisito 34, criterio 3: no más de una entrega por recurso por segundo. Se implementa con `INotificationThrottler`:

```csharp
public interface INotificationThrottler
{
    /// <summary>
    /// Devuelve true si el evento debe entregarse, false si se coalesce con el previo.
    /// </summary>
    bool ShouldDeliver(NotificationEvent evt);
}
```

Un `TokenBucketNotificationThrottler` conserva el último evento por `(ResourceType, ResourceId)` con timestamp; si un nuevo evento llega antes del segundo, reemplaza al anterior y programa una entrega diferida.

### Reconnection Handler (Cliente)

La UI implementa reconexión con backoff exponencial (1, 2, 4, 8, ..., 30 s). El SDK cliente vive fuera del alcance de este documento, pero el contrato SSE incluye `retry:` para guiar al navegador.

---

## Caching (Abstraction)

### ICacheService Interface

```csharp
namespace BotPulse.Core.Abstractions.Caching;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class;
    Task RemoveAsync(string key, CancellationToken ct);
    Task InvalidatePatternAsync(string pattern, CancellationToken ct);
}
```

Los servicios de negocio dependen **exclusivamente** de esta abstracción (Restricción Arquitectónica 7).

### Implementations

| Implementación            | Estado  | Backing store                        |
|---------------------------|---------|---------------------------------------|
| `MemoryCacheService`      | MVP     | `IMemoryCache` proceso local          |
| `RedisCacheService`       | Futuro  | Redis via `StackExchange.Redis`       |
| `DistributedCacheService` | Futuro  | Cualquier `IDistributedCache`         |

```csharp
internal sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct) where T : class
        => Task.FromResult(_cache.TryGetValue(key, out T? v) ? v : null);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) where T : class
    {
        _cache.Set(key, value, ttl);
        _keys.TryAdd(key, 0);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct)
    {
        _cache.Remove(key);
        _keys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task InvalidatePatternAsync(string pattern, CancellationToken ct)
    {
        var toRemove = _keys.Keys.Where(k => k.StartsWith(pattern, StringComparison.Ordinal)).ToList();
        foreach (var k in toRemove)
        {
            _cache.Remove(k);
            _keys.TryRemove(k, out _);
        }
        return Task.CompletedTask;
    }
}
```

Cuando se migre a Redis, la única línea que cambia es la registrada en DI. Los servicios no cambian.

---

## Persistence Layer

### EF Core DbContext

Solo entidades persistidas.

```csharp
public sealed class BotPulseDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<QueueItem> QueueItems => Set<QueueItem>();
    public DbSet<ExecutionLog> ExecutionLogs => Set<ExecutionLog>();
    public DbSet<MetricPoint> MetricsRaw => Set<MetricPoint>();
    public DbSet<MetricRollup> MetricsHourly => Set<MetricRollup>();
    public DbSet<MetricRollup> MetricsDaily => Set<MetricRollup>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertRule> AlertRules => Set<AlertRule>();
    public DbSet<DashboardLayout> DashboardLayouts => Set<DashboardLayout>();
    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    public BotPulseDbContext(DbContextOptions<BotPulseDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BotPulseDbContext).Assembly);
    }
}
```

**Ausentes intencionalmente:** `Robots`, `Machines`, `Processes`, `Assets`, `Queues` (metadata).

### Repository Pattern + Unit of Work

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct);
    Task AddAsync(T entity, CancellationToken ct);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
}

// Repositorios especializados donde aporte valor (queries dominio-específicas):
public interface IJobRepository : IRepository<Job>
{
    Task<Job?> GetByExternalIdAsync(string provider, string externalId, CancellationToken ct);
    Task<DateTime?> GetMaxUpdatedAtAsync(CancellationToken ct);
    Task<PagedResult<Job>> QueryAsync(JobFilter filter, CancellationToken ct);
    Task UpsertAsync(JobSnapshot snapshot, CancellationToken ct);
}
```

`IAuditRepository` es **append-only**: solo expone `RecordAsync` (no Update ni Delete). La retención se ejecuta con jobs de mantenimiento a nivel de DB, no vía código de negocio.

### Migration Strategy

- EF Core Migrations gestionadas desde `BotPulse.Infrastructure`.
- Al arrancar `BotPulse.Api`, un `IHostedService` de migración (opcional, controlado por flag) ejecuta `db.Database.MigrateAsync()`.
- En despliegues productivos con múltiples réplicas, las migraciones se ejecutan **antes** del rollout, por CI/CD o por un job dedicado.
- No se permite `EnsureCreated()` en producción.

---

## Alert Engine

### AlertRule Model

Almacenado en `alert_rules`. Cada regla tiene:

- `RuleType`: identificador del evaluador (`RobotOffline`, `QueueBacklog`, `JobsFailedInWindow`, `MachineOffline`, `ProcessExecutionTime`).
- `ParametersJson`: umbrales y ventanas específicos del tipo.
- `ChannelsJson`: array de canales configurados (`["Log","Email","Slack"]`).
- `Enabled`, `Severity`, `EscalationEnabled`, `EscalationTimeoutMinutes`.

### Alert Engine Evaluator

```csharp
public interface IAlertRuleEvaluator
{
    string RuleType { get; }
    Task<IReadOnlyList<AlertCandidate>> EvaluateAsync(AlertRule rule, CancellationToken ct);
}

public sealed record AlertCandidate(
    string AffectedResourceType,
    string AffectedResourceId,
    string ConditionDescription);
```

Cada regla built-in tiene su evaluador:

- `RobotOfflineEvaluator`: consulta `IRobotProvider` on-demand y compara `LastHeartbeatUtc`.
- `QueueBacklogEvaluator`: consulta el repo de `QueueItems` (pendientes por cola).
- `JobsFailedInWindowEvaluator`: agrega `Jobs` de los últimos N minutos.
- `MachineOfflineEvaluator`: consulta `IMachineProvider` on-demand.
- `ProcessExecutionTimeEvaluator`: compara `Duration` de jobs con la expectativa configurada por proceso.

El `AlertEngine` orquesta:

```csharp
public sealed class AlertEngine
{
    private readonly IEnumerable<IAlertRuleEvaluator> _evaluators;
    private readonly IAlertRuleRepository _rules;
    private readonly IAlertDeduplicator _dedup;
    private readonly IAlertRepository _alerts;
    private readonly NotificationRouter _router;
    private readonly ISystemClock _clock;

    public async Task EvaluateAllAsync(CancellationToken ct)
    {
        var rules = await _rules.GetEnabledAsync(ct).ConfigureAwait(false);
        foreach (var rule in rules)
        {
            var evaluator = _evaluators.First(e => e.RuleType == rule.RuleType);
            var candidates = await evaluator.EvaluateAsync(rule, ct).ConfigureAwait(false);
            foreach (var c in candidates)
            {
                if (!_dedup.ShouldEmit(rule, c, _clock.UtcNow)) continue;
                var alert = Alert.Raise(rule, c, _clock.UtcNow);
                await _alerts.AddAsync(alert, ct).ConfigureAwait(false);
                await _router.DispatchAsync(alert, rule, ct).ConfigureAwait(false);
            }
        }
    }
}
```

### Notification Router

```csharp
public interface IAlertChannel
{
    string Name { get; }
    Task DeliverAsync(Alert alert, AlertRule rule, CancellationToken ct);
    Task<bool> IsHealthyAsync(CancellationToken ct);
}

public sealed class NotificationRouter
{
    private readonly IEnumerable<IAlertChannel> _channels;
    private readonly ILogger<NotificationRouter> _logger;

    public async Task DispatchAsync(Alert alert, AlertRule rule, CancellationToken ct)
    {
        var configured = ParseChannelsFrom(rule.ChannelsJson);
        var tasks = _channels
            .Where(c => configured.Contains(c.Name))
            .Select(c => DispatchWithRetryAsync(c, alert, rule, ct));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task DispatchWithRetryAsync(IAlertChannel channel, Alert alert, AlertRule rule, CancellationToken ct)
    {
        // Polly: exponential backoff, max 3 intentos (Requisito 30, criterio 4).
    }
}
```

### Channels (Log, Email, Slack, Teams, Webhook)

Cada canal es una clase separada en `BotPulse.Infrastructure/Alerts/Channels/`. Nuevos canales se agregan implementando `IAlertChannel` (Requisito 30, criterio 2), sin modificar el engine.

### Escalation Engine

```csharp
public sealed class EscalationEngine
{
    // Ejecutado por AlertEvaluationService en cada tick.
    public async Task EscalatePendingAsync(CancellationToken ct)
    {
        var pending = await _alerts.GetUnacknowledgedCriticalAsync(_clock.UtcNow, ct)
            .ConfigureAwait(false);

        foreach (var alert in pending)
        {
            var rule = await _rules.GetByIdAsync(alert.RuleId, ct).ConfigureAwait(false);
            if (!rule.EscalationEnabled) continue;
            var elapsed = _clock.UtcNow - alert.RaisedAtUtc;
            if (elapsed >= TimeSpan.FromMinutes(rule.EscalationTimeoutMinutes) && alert.EscalationLevel == 0)
                await EscalateAsync(alert, rule, level: 1, ct).ConfigureAwait(false);
            // Segundo nivel opcional, configurable.
        }
    }
}
```

---

## Dashboard Widgets

### Widget Types (Requisito 25)

| Widget                | Descripción                                                    |
|-----------------------|----------------------------------------------------------------|
| Robot Monitor         | Lista y estado on-demand de robots                             |
| Job Queue             | Últimos jobs con estado                                        |
| Queue Progress        | Barras de progreso por cola con backlog y processing rate      |
| Machine Health        | Estado on-demand de máquinas                                   |
| KPI Summary           | KPIs de alto nivel (Requisito 24)                              |
| Alerts                | Alertas activas y recientes                                    |
| Execution Timeline    | Timeline temporal de ejecuciones                               |

### Configuration Storage

Un registro por usuario en `dashboard_layouts.widgets_json`:

```json
[
  { "type": "KPISummary", "order": 0, "settings": { "refreshSeconds": 15 } },
  { "type": "JobQueue",   "order": 1, "settings": { "filter": { "status": "Failed" } } },
  { "type": "Alerts",     "order": 2, "settings": { "severity": ["Warning","Critical"] } }
]
```

### Role-Based Default Layouts (Requisito 26)

`DashboardInitializer` aplica un layout inicial en el primer login según el rol:

- **Viewer**: KPISummary + JobQueue + Alerts.
- **Operator**: KPISummary + RobotMonitor + JobQueue + QueueProgress + Alerts.
- **Administrator**: KPISummary + RobotMonitor + MachineHealth + JobQueue + QueueProgress + Alerts + ExecutionTimeline.

### Widget Permissions (Requisito 27)

Un `WidgetPermissionModel` centraliza el requerimiento mínimo por widget. `DashboardBuilder` filtra la oferta antes de mostrarla. `DashboardRenderer` re-verifica antes de renderizar.

---

## REST API (Versioned)

### API v1 Endpoints

Todos los endpoints viven bajo `/api/v1/*`. Ejemplos:

```
GET    /api/v1/robots
GET    /api/v1/robots/{externalId}
GET    /api/v1/machines
GET    /api/v1/processes
GET    /api/v1/processes/{externalId}/parameters
GET    /api/v1/assets                    [Administrator]
GET    /api/v1/jobs                      ?filter=...
POST   /api/v1/jobs                      [Operator|Administrator]  (start)
POST   /api/v1/jobs/{externalId}/stop    [Operator|Administrator]
POST   /api/v1/jobs/{externalId}/cancel  [Operator|Administrator]
POST   /api/v1/jobs/{externalId}/retry   [Operator|Administrator]
GET    /api/v1/queues
GET    /api/v1/queues/{name}/items
GET    /api/v1/logs                      ?jobId=&severity=&from=&to=
GET    /api/v1/metrics                   ?name=&from=&to=&granularity=
GET    /api/v1/alerts
POST   /api/v1/alerts/{id}/ack           [Operator|Administrator]
GET    /api/v1/alert-rules               [Administrator]
POST   /api/v1/alert-rules               [Administrator]
PUT    /api/v1/alert-rules/{id}          [Administrator]
GET    /api/v1/dashboard/layout
PUT    /api/v1/dashboard/layout
POST   /api/v1/auth/login
POST   /api/v1/auth/logout
GET    /api/v1/auth/me
GET    /api/v1/notifications/stream      (SSE)
GET    /api/v1/notifications/pull        ?since=
POST   /api/v1/admin/sync/{service}/trigger  [Administrator]
GET    /api/v1/admin/sync/status
```

### API Versioning Strategy

Usando `Microsoft.AspNetCore.Mvc.Versioning`:

```csharp
services.AddApiVersioning(o =>
{
    o.DefaultApiVersion = new ApiVersion(1, 0);
    o.AssumeDefaultVersionWhenUnspecified = false;
    o.ReportApiVersions = true;
    o.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("api-version"));
});

services.AddVersionedApiExplorer(o =>
{
    o.GroupNameFormat = "'v'VVV";
    o.SubstituteApiVersionInUrl = true;
});
```

Controllers:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class JobsController : ControllerBase { /* ... */ }
```

Cuando v2 exista, coexistirá con v1 durante al menos 6 meses (Requisito 38, criterio 3). Las respuestas de v1 comienzan a incluir `Deprecation` header cuando se anuncia el sunset.

### Error Handling Middleware

Formato de respuesta de error estandarizado:

```json
{
  "errorCode": "VALIDATION_ERROR",
  "message": "Field 'processId' is required.",
  "correlationId": "1ff34a09b8cd4a71a2f9",
  "timestamp": "2024-01-20T12:34:56.789Z",
  "details": [
    { "field": "processId", "issue": "required" }
  ]
}
```

Códigos mapeados:

| Excepción                     | HTTP | errorCode                   |
|-------------------------------|------|-----------------------------|
| `ValidationException`         | 400  | `VALIDATION_ERROR`          |
| `EntityNotFoundException`     | 404  | `NOT_FOUND`                 |
| `AuthenticationException`     | 401  | `UNAUTHENTICATED`           |
| `AuthorizationException`      | 403  | `FORBIDDEN`                 |
| `ProviderException`           | 502  | `PROVIDER_ERROR`            |
| otras                          | 500  | `INTERNAL_SERVER_ERROR`     |

### OpenAPI/Swagger Configuration

- Un documento por versión: `/swagger/v1/swagger.json`.
- UI en `/swagger`.
- Autenticación Bearer configurada para poder probar endpoints protegidos desde la UI.

---

## Health Checks

BotPulse expone tres endpoints diferenciados usando `Microsoft.Extensions.Diagnostics.HealthChecks`.

### Endpoints

| Endpoint            | Semántica                                                                 |
|---------------------|---------------------------------------------------------------------------|
| `/health`           | Vista agregada JSON con estado por dependencia y por sync service         |
| `/health/live`      | Liveness probe. 200 mientras el proceso esté vivo (no bloqueado)          |
| `/health/ready`     | Readiness probe. 200 solo si todas las dependencias críticas están sanas  |

### Health Check Implementations

```csharp
services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<RpaProviderHealthCheck>("rpa-provider", tags: new[] { "ready" })
    .AddCheck<CacheHealthCheck>("cache", tags: new[] { "ready" })
    .AddCheck<SynchronizationHealthCheck>("sync-orchestrator", tags: new[] { "ready" });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteJsonAsync
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,   // ninguno; siempre saludable si responde
    ResponseWriter = HealthResponseWriter.WriteMinimalAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = h => h.Tags.Contains("ready"),
    ResponseWriter = HealthResponseWriter.WriteJsonAsync
});
```

Cada Synchronization Service reporta su health individual vía `SynchronizationHealthCheck`, que consulta `SynchronizationOrchestrator.GetStatuses()`.

Ejemplo de respuesta de `/health`:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.023",
  "entries": {
    "database": { "status": "Healthy" },
    "rpa-provider": { "status": "Healthy", "data": { "provider": "UiPath", "version": "23.10" } },
    "cache": { "status": "Healthy", "data": { "impl": "MemoryCacheService" } },
    "sync-orchestrator": {
      "status": "Healthy",
      "data": {
        "JobSync":       { "lastRun": "...", "outcome": "Success", "processed": 42 },
        "QueueItemSync": { "lastRun": "...", "outcome": "Success", "processed": 15 },
        "LogSync":       { "lastRun": "...", "outcome": "Success", "processed": 512 },
        "MetricsCollection": { "lastRun": "...", "outcome": "Success" },
        "AlertEvaluation":   { "lastRun": "...", "outcome": "Success" }
      }
    }
  }
}
```

---

## Logging & Audit

### Serilog Configuration

```csharp
public static class SerilogConfig
{
    public static IHostBuilder UseBotPulseSerilog(this IHostBuilder builder)
        => builder.UseSerilog((ctx, cfg) =>
        {
            cfg.ReadFrom.Configuration(ctx.Configuration)
               .Enrich.FromLogContext()
               .Enrich.WithMachineName()
               .Enrich.WithEnvironmentName()
               .Enrich.WithProperty("Application", "BotPulse")
               .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {CorrelationId} {SourceContext}: {Message:lj}{NewLine}{Exception}")
               .WriteTo.File(
                    path: "logs/botpulse-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30);
        });
}
```

### Structured Logging Practices

- **Nunca** `Console.WriteLine`. Siempre `ILogger<T>` con message template.
- Cada request tiene un `CorrelationId` inyectado por `CorrelationIdMiddleware` y propagado en logs, notificaciones y audit.
- Cada sync service loguea `serviceName`, `startTime`, `duration`, `itemsProcessed`, `outcome`.
- Nivel configurable por namespace vía `appsettings.json`.

### Audit Log (Separate)

- `IAuditRepository` es distinto de `ILogger`.
- Persistido en tabla dedicada `audit_records`.
- Append-only desde la aplicación (sin API de update/delete).
- Retención configurable, default 24 meses (Requisito 42, criterio 4).
- El `AuditMiddleware` registra automáticamente eventos sensibles: login, logout, job actions, alert rule changes, asset access, configuration changes.

---

## Deployment

### Docker Compose (Primary)

```yaml
version: "3.9"

services:
  reverse-proxy:
    image: nginx:1.27-alpine
    container_name: botpulse-proxy
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./deploy/nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./deploy/nginx/certs:/etc/nginx/certs:ro
    depends_on:
      - api
    networks: [botpulse-net]

  api:
    build:
      context: .
      dockerfile: deploy/Dockerfile.Api
    container_name: botpulse-api
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__PostgreSQL: "Host=postgres;Port=5432;Database=botpulse;Username=botpulse;Password=${DB_PASSWORD}"
      Authentication__Provider: ${AUTHENTICATION_PROVIDER}
      Notifications__Transport: ${NOTIFICATION_TRANSPORT}
      Cache__Provider: ${CACHE_PROVIDER}
      Jwt__SigningKeyBase64: ${JWT_SIGNING_KEY}
      Jwt__Issuer: botpulse
      Jwt__Audience: botpulse-api
      UiPath__BaseUrl: ${UIPATH_BASE_URL}
      UiPath__Tenant: ${UIPATH_TENANT}
      UiPath__ClientId: ${UIPATH_CLIENT_ID}
      UiPath__ClientSecret: ${UIPATH_CLIENT_SECRET}
    depends_on:
      postgres: { condition: service_healthy }
      redis:    { condition: service_started }
    healthcheck:
      test: ["CMD", "curl", "-fsS", "http://localhost:8080/health/live"]
      interval: 30s
      timeout: 5s
      retries: 3
    networks: [botpulse-net]
    restart: unless-stopped

  worker:
    build:
      context: .
      dockerfile: deploy/Dockerfile.Worker
    container_name: botpulse-worker
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ConnectionStrings__PostgreSQL: "Host=postgres;Port=5432;Database=botpulse;Username=botpulse;Password=${DB_PASSWORD}"
      UiPath__BaseUrl: ${UIPATH_BASE_URL}
      UiPath__Tenant: ${UIPATH_TENANT}
      UiPath__ClientId: ${UIPATH_CLIENT_ID}
      UiPath__ClientSecret: ${UIPATH_CLIENT_SECRET}
      Synchronization__JobSync__IntervalSeconds: 120
      Synchronization__QueueItemSync__IntervalSeconds: 180
      Synchronization__LogSync__IntervalSeconds: 60
      Synchronization__MetricsCollection__IntervalSeconds: 300
    depends_on:
      postgres: { condition: service_healthy }
    networks: [botpulse-net]
    restart: unless-stopped

  postgres:
    image: postgres:15-alpine
    container_name: botpulse-db
    environment:
      POSTGRES_DB: botpulse
      POSTGRES_USER: botpulse
      POSTGRES_PASSWORD: ${DB_PASSWORD}
    volumes:
      - botpulse-postgres:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U botpulse -d botpulse"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks: [botpulse-net]

  redis:
    image: redis:7-alpine
    container_name: botpulse-redis
    # Provisionado desde el día 1 aunque no se use en MVP: preparación
    # para caché distribuida, SignalR backplane, sesiones y rate limiting.
    volumes:
      - botpulse-redis:/data
    networks: [botpulse-net]

networks:
  botpulse-net:
    driver: bridge

volumes:
  botpulse-postgres:
  botpulse-redis:
```

### Deployment Model Matrix

| Deployment                    | API                        | Worker                     | Proxy / Ingress          | DB / Cache                            |
|-------------------------------|----------------------------|----------------------------|--------------------------|---------------------------------------|
| Docker Compose (dev/prod)     | contenedor `api`           | contenedor `worker`        | nginx / traefik          | postgres + redis                      |
| Azure App Service             | App Service Plan (Linux)   | WebJob o App Service       | Azure Front Door / APIM  | Azure DB for PostgreSQL + Azure Cache |
| Azure Container Apps          | Container App              | Container App (jobs)       | Azure Front Door         | Azure DB for PostgreSQL + Azure Cache |
| IIS Windows                   | AppPool con ANCM           | Windows Service            | IIS / ARR                | PostgreSQL local o managed             |
| Linux + Reverse Proxy         | systemd service            | systemd service            | nginx / traefik          | PostgreSQL local o managed             |

**Regla clave:** el binario es el mismo. Todo lo específico del entorno se resuelve por configuración (env vars, appsettings.{Environment}.json, secret stores).

### Configuration Strategy

- Prioridad de fuentes (mayor a menor): environment variables, secrets provider (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault), `appsettings.{Environment}.json`, `appsettings.json`.
- Todas las opciones se validan al arrancar (`IValidateOptions<T>`), fallando fast si algo crítico falta.
- No se editan archivos empaquetados en la imagen entre entornos (Restricción Arquitectónica 6).

---

## Security Considerations

### Credential Management

- **JWT signing key**: cargada desde secret store (Base64), configurable, rotable.
- **RPA Provider secrets** (UiPath OAuth2 `ClientSecret`, LDAP bind password, Entra ID client secret): cargados desde secret store o env vars. Nunca en código fuente.
- **Local user passwords**: hashed con **Argon2id** (`t=3, m=64 MiB, p=1` o superior) o **bcrypt** con cost `>= 12`.
- Sensitive values (secrets, tokens) nunca se logean. `Serilog.Destructurama` para redactar campos marcados.

### Network Security

- HTTPS obligatorio en producción. Redirect HTTP → HTTPS + HSTS.
- Reverse proxy termina TLS. La comunicación intra-red se puede realizar por HTTP en red privada.
- CORS restrictivo: solo los orígenes explícitamente configurados.
- Rate limiting: preparado (habilitable en fase 3, apoyado en Redis).

### Data Validation

- Todos los DTOs de entrada validados con FluentValidation o Data Annotations.
- Consultas SQL siempre parametrizadas via EF Core. Nunca concatenación.
- Inputs a filtros de log/audit sanitizados para prevenir log forging (Serilog los estructura como propiedades, no como concatenación).

### Audit Trail

- Acciones sensibles (login/logout, job actions, alert rule changes, asset access, config changes) se persisten en `audit_records`.
- Cada registro incluye `correlationId` para trazar transversalmente.
- La retención es configurable (default 24m). No hay API de modificación.

---

## Architecture Decision Records (ADR)

Los ADR viven en `/docs/ADR/` como archivos `NNNN-title.md`. Cada ADR sigue el formato:

```
# ADR-XXX: Title

## Status
Accepted | Superseded | Deprecated

## Context
Situación que motiva la decisión.

## Decision
La decisión tomada.

## Alternatives Considered
Otras opciones evaluadas y por qué se descartaron.

## Consequences
Positivas y negativas.
```

### Listado de ADRs

| ID       | Título                                                     | Resumen                                                                                                    |
|----------|------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| ADR-001  | Clean Architecture                                         | Separación estricta Domain/Application/Infrastructure/Presentation. Dependencias unidireccionales al Dominio. |
| ADR-002  | Provider Pattern Granular (no monolítico)                  | Interfaces separadas por capacidad (`IRobotProvider`, `IJobProvider`, ...). ISP + DIP.                     |
| ADR-003  | Selective Persistence                                      | Solo se persisten Jobs, Queue Items, Logs, Metrics y Audit. El resto se lee on-demand.                     |
| ADR-004  | Docker as Primary Packaging                                | Docker Compose es el modelo primario. Todos los demás modelos usan el mismo binario.                       |
| ADR-005  | OAuth2 Client Credentials for UiPath                       | Autenticación al Orchestrator vía OAuth2 CC. Tokens cacheados y renovados automáticamente.                 |
| ADR-006  | JWT as Session Token Only                                  | JWT no es un método de autenticación. Es solo el session token emitido tras la validación del IdP.         |
| ADR-007  | PostgreSQL as Primary Database                             | PostgreSQL 15+ como base primaria. JSONB, particionado y disponibilidad amplia en clouds.                  |
| ADR-008  | Independent Background Synchronization Services            | Servicios de sync independientes coordinados por `SynchronizationOrchestrator`.                            |
| ADR-009  | Polling/SSE for MVP Real-Time (SignalR later)              | MVP usa Polling o SSE. SignalR/WebSockets se agregan más adelante detrás de la misma abstracción.          |
| ADR-010  | Repository Pattern with Unit of Work                       | Repositorios especializados + `IUnitOfWork` sobre EF Core. Facilita testing y aísla persistence.           |
| ADR-011  | Pluggable Authentication                                   | `IAuthenticationProvider` con implementaciones Entra ID, LDAP y Local. Selección por configuración.       |
| ADR-012  | API Versioning from Day 1                                  | Todos los endpoints bajo `/api/v1`. Coexistencia de versiones y política de deprecation.                   |
| ADR-013  | RPA Operations Platform (Vendor-Agnostic)                  | El producto es una plataforma agnóstica. UiPath es solo el primer proveedor.                               |

Cada ADR se materializa en un archivo separado durante la implementación. Los enlaces se agregarán a este documento cuando existan (ej. `docs/ADR/0002-provider-pattern-granular.md`).

---

## Coding Standards Summary

El detalle completo vive en `/docs/CodingStandards.md`. Resumen de reglas mandatorias:

- **Async/await everywhere**: prohibido `.Result` y `.Wait()`. Toda I/O es async. `CancellationToken` propagado.
- **Dependency Injection only**: sin service locators, sin estado estático. Toda dependencia se inyecta.
- **Thin Controllers**: los controllers son delgados. Sin business logic. Delegan al Application Service.
- **No direct database access outside Infrastructure**: solo `BotPulse.Infrastructure` toca EF Core y PostgreSQL.
- **No vendor-specific API calls outside Provider projects**: solo `BotPulse.Providers.<Vendor>` conoce la API del vendor.
- **Strongly typed configuration**: `IOptions<T>` con clases inmutables. Validación al arrancar (`IValidateOptions<T>`).
- **SOLID**: SRP, OCP, LSP, ISP, DIP obligatorios en revisión de código.
- **Nullable reference types enabled**: `#nullable enable` en todos los proyectos.
- **XML documentation** para todos los tipos y miembros públicos (`GenerateDocumentationFile=true`).
- **Structured logging con Serilog**. Nunca `Console.WriteLine`.
- **Naming conventions**: convenciones estándar de .NET (`PascalCase`, `camelCase`, `_camelCase` para private fields).
- **Unit tests** para todo servicio del dominio. **Property-based tests** en los flujos donde aplique (validación, invariantes de dedup de alertas, retry, agregación).
- **Configuration files** nunca contienen secretos. Los secretos vienen de env vars o secret stores.

---

## Scalability & Future Extensions

### Adding a New RPA Provider (por ejemplo Power Automate)

```
1. Crear proyecto BotPulse.Providers.PowerAutomate.
2. Implementar las interfaces granulares soportadas por Power Automate:
   - IRobotProvider, IJobProvider, ILogProvider, IProcessProvider (según capacidades).
3. Implementar OAuth2 / Managed Identity contra Power Automate.
4. Implementar IProviderVersionNegotiator si aplica.
5. Registrar en DI: services.AddPowerAutomateProvider(configuration).
6. Cambiar variable de entorno RPA_PROVIDER=PowerAutomate para conmutar.
7. No hay cambios en el Core, ni en la API, ni en la UI, ni en el Worker.
```

### Adding a New Authentication Provider (por ejemplo Okta)

```
1. Crear OktaAuthenticationProvider : IAuthenticationProvider.
2. Registrar el nuevo case en AddPluggableAuthentication.
3. Documentar variables de entorno en /docs/Deployment.md.
4. Cambiar AUTHENTICATION_PROVIDER=Okta para activarlo.
```

### Adding a New Alert Channel (por ejemplo PagerDuty)

```
1. Crear PagerDutyAlertChannel : IAlertChannel.
2. Registrar en DI.
3. La regla puede incluir "PagerDuty" en channelsJson.
```

### Adding a New Notification Transport (por ejemplo SignalR)

```
1. Implementar SignalRNotificationDelivery : INotificationDelivery.
2. Registrar el nuevo case en AddPluggableNotifications.
3. Provisionar backplane Redis (ya está en Docker Compose).
4. Cambiar NOTIFICATION_TRANSPORT=SignalR para activarlo.
5. La UI cambia solo su cliente SDK; la abstracción permanece.
```

---

## Testing Strategy

BotPulse combina tres niveles de testing.

### Unit Tests (`BotPulse.UnitTests`)

Enfocados en:

- Servicios de aplicación con dependencias mockeadas (`IRobotProvider`, `IAuthenticationProvider`, `ICacheService`, etc.).
- Validadores.
- Mappers y traducción de errores.
- Evaluadores de reglas de alerta y deduplicador.
- Lógica de rollup de métricas.
- `JwtSessionTokenService` (issue + validate).

Framework: **xUnit** + **FluentAssertions** + **NSubstitute**.

### Integration Tests (`BotPulse.IntegrationTests`)

- API end-to-end sobre `WebApplicationFactory` con Postgres en Testcontainers.
- Migraciones aplicadas al inicio.
- OAuth2 client credentials contra un stub HTTP.
- Autenticación (Local Provider) con hash real.
- Health checks (`/health`, `/health/live`, `/health/ready`) en distintos estados.
- Escenarios de sync end-to-end con `IJobProvider` reemplazado por un fake determinista.

### Property-Based Tests (`BotPulse.PropertyTests`)

Aplicabilidad: **BotPulse es una plataforma predominantemente integradora**. Muchos criterios de aceptación son de configuración, health checks, integración con APIs externas y UI — donde las property-based tests aportan poco valor. Sin embargo, hay áreas específicas donde PBT es útil:

- **Alert deduplicator**: para toda secuencia de candidatos dentro de la ventana de dedup, el número de alertas emitidas por (rule, resource) no supera 1.
- **Retry / exponential backoff**: para toda secuencia de fallos, la suma de esperas está acotada superiormente.
- **Metrics aggregation**: `sum(hourly buckets) == sum(raw points)` para toda serie temporal.
- **JWT round-trip**: `validate(issue(x)) == x` para cualquier `AuthenticationResult` válido.
- **Cache invalidation by prefix**: para todo set de claves con prefijo P, `InvalidatePatternAsync(P)` remueve exactamente ese subconjunto.
- **Job upsert idempotence**: `upsert(snapshot) == upsert(upsert(snapshot))` (misma fila resultante).

Framework: **FsCheck.Xunit** o **CsCheck**. Cada test configurado con mínimo 100 iteraciones y etiqueta que referencia la propiedad conceptual.

Para el resto de criterios (deployment, health checks, versionado de API, sync scheduling, UI widgets, entrega de notificaciones en tiempo real), el enfoque preferido es example-based unit tests + integration tests.

---

## Development Environment Setup

### Prerequisitos

- .NET 8.0 SDK
- Docker Desktop / Docker Engine + Docker Compose v2
- PostgreSQL client (`psql`) opcional para inspección
- IDE: Visual Studio 2022 17.8+, Rider 2023.3+ o VS Code con C# Dev Kit

### Setup Local

```
# Clonar repo
git clone https://github.com/your-org/BotPulse.git

# Copiar .env
cp .env.example .env
# Editar .env con secrets de UiPath, DB password, JWT key, etc.

# Levantar dependencias (Postgres + Redis)
docker compose -f docker-compose.yml up -d postgres redis

# Aplicar migraciones
dotnet ef database update --project src/BotPulse.Infrastructure --startup-project src/BotPulse.Api

# Ejecutar API
dotnet run --project src/BotPulse.Api

# Ejecutar Worker en otra terminal
dotnet run --project src/BotPulse.Worker

# API: https://localhost:5001
# Swagger: https://localhost:5001/swagger
# Health: https://localhost:5001/health
```

### Sugerencia para el usuario

Ejecutar los procesos de desarrollo (`dotnet run` API y Worker, `dotnet watch`, etc.) manualmente en terminales dedicadas. Estos comandos son long-running y no deben lanzarse desde herramientas de agente.

### Ejecución de tests

```
dotnet test tests/BotPulse.UnitTests
dotnet test tests/BotPulse.IntegrationTests
dotnet test tests/BotPulse.PropertyTests
```

Para test suite completo con coverage:

```
dotnet test --collect:"XPlat Code Coverage"
```

---

## Migration Path (MVP → Phases)

### Fase 1 – MVP

- `BotPulse.Providers.UiPath` implementando las 7 interfaces granulares.
- `LocalAuthenticationProvider` funcional para desarrollo. Rutas para configurar `EntraID` documentadas.
- Alert Engine con 5 reglas built-in y canal `Log`.
- Real-Time Notifications: SSE o Polling (configurable).
- `MemoryCacheService`.
- Dashboard con widgets básicos (KPI Summary, Job Queue, Robot Monitor, Alerts).
- API v1 completa.
- Health checks `/health`, `/health/live`, `/health/ready`.
- Deployment vía Docker Compose (con Redis provisionado pero no usado).

### Fase 2

- `EntraIdAuthenticationProvider` y `LdapAuthenticationProvider` operativos.
- Alert Engine completo con canales Email, Slack, Teams, Webhook y escalación automática.
- Widgets adicionales (Queue Progress, Machine Health, Execution Timeline).
- Layouts predefinidos por rol.
- Deployment en Azure App Service y Azure Container Apps documentado.

### Fase 3

- `BotPulse.Providers.PowerAutomate`.
- `RedisCacheService` operativo.
- `SignalRNotificationDelivery` operativo con backplane Redis.
- Rate limiting.

### Fase 4

- `BotPulse.Providers.BluePrism` y `BotPulse.Providers.AutomationAnywhere`.
- Multi-tenant (aislamiento por tenant en persistence + auth claims).
- Mobile dashboard.

---

## Document Version History

| Versión | Fecha       | Cambios                                                                                                    |
|---------|-------------|------------------------------------------------------------------------------------------------------------|
| 2.0     | 2024-01-20  | Reescritura completa. Alineado con requirements.md (42 requisitos). Incorporados los 12 cambios arquitectónicos de comentarios.md/designCOmments.md: autenticación pluggable, provider pattern granular, servicios de sync independientes, API versioning, health checks production-ready, persistencia selectiva, multi-deployment, notificaciones en tiempo real abstraídas, caché abstraído, ADRs, coding standards, visión de plataforma vendor-agnostic. |
| 1.0     | 2024-01-15  | Versión inicial (superseded).                                                                              |

---

**End of Design Document**
