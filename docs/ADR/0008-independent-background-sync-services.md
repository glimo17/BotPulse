# ADR-008: Servicios de Sincronización Background Independientes

## Status
Accepted

## Context
BotPulse necesita sincronizar datos desde el Orchestrator RPA hacia su base de datos local de forma continua: Jobs, Queue Items, Execution Logs y Métricas. Además, debe ejecutar el motor de evaluación de alertas periódicamente.

Estas tareas tienen características muy diferentes:
- `JobSync`: intervalo de 120s, batches de cientos de registros
- `QueueItemSync`: intervalo de 180s
- `LogSync`: intervalo de 60s, batches de 500 logs (alta frecuencia)
- `MetricsCollection`: intervalo de 300s, calcula agregaciones
- `AlertEvaluation`: intervalo de 60s, consulta providers on-demand

Un error en la sincronización de logs no debe afectar la sincronización de jobs. La configuración de un servicio no debe afectar a los demás. Cada servicio debe poder habilitarse o deshabilitarse de forma independiente.

## Decision
BotPulse implementa **5 servicios de sincronización independientes** coordinados por un `SynchronizationOrchestrator`:

- `JobSynchronizationService`
- `QueueItemSynchronizationService`
- `LogSynchronizationService`
- `MetricsCollectionService`
- `AlertEvaluationService`

Cada servicio implementa `ISynchronizationService` y gestiona su propio bucle basado en `PeriodicTimer`. Usa `SemaphoreSlim(1,1)` (single-flight) para evitar ejecuciones concurrentes del mismo servicio. Usa `IServiceScopeFactory` para crear scopes DI limpios en cada ejecución.

El `SynchronizationOrchestrator` arranca los servicios habilitados, expone `GetStatuses()` y soporta `TriggerAsync(serviceName)` para triggers manuales desde la API.

La configuración de cada servicio es independiente vía `IOptionsMonitor<SynchronizationOptions>`, permitiendo cambiar el intervalo en runtime sin reiniciar.

## Alternatives Considered

**Un mega-worker monolítico (todo en un IHostedService)**
Un único `IHostedService` que ejecuta todos los pasos en secuencia: JobSync → QueueItemSync → LogSync → MetricsCollection → AlertEvaluation. Más simple de implementar inicialmente. Problemas: un fallo en LogSync bloquea AlertEvaluation; no se puede configurar el intervalo de cada paso de forma independiente; no hay aislamiento de errores; el health check no puede distinguir qué servicio está degradado. Descartado.

**Cron externo (cron, Hangfire, Quartz.NET)**
Delegar la programación a un scheduler externo. Hangfire y Quartz.NET son robustos pero requieren infraestructura adicional (base de datos para persistencia de jobs). Para el scope de BotPulse, la complejidad de un scheduler externo no está justificada cuando `PeriodicTimer` + `IHostedService` cubre perfectamente el caso de uso. Descartado para MVP.

**Azure Functions / AWS Lambda (serverless)**
Ejecutar cada sincronización como una función serverless con trigger de tiempo. Aumentaría la complejidad de despliegue y eliminaría la portabilidad on-premises. Descartado por la dependencia de cloud.

## Consequences

**Positivas:**
- **Fault isolation**: un fallo en `LogSynchronizationService` no afecta a `JobSynchronizationService`. Los errores se capturan en el servicio correspondiente y no propagan al orquestador.
- **Configuración granular**: cada servicio tiene su propio `IntervalSeconds`, `BatchSize` y `Enabled`. Se puede deshabilitar `MetricsCollection` sin afectar la sincronización de logs.
- **Health check preciso**: el `SynchronizationHealthCheck` puede indicar exactamente qué servicio está degradado.
- **Triggers manuales**: un administrador puede forzar una sincronización puntual de Jobs sin esperar al próximo ciclo.
- **Escalabilidad independiente**: en el futuro, cada servicio podría migrarse a un proceso separado si el volumen lo requiere.

**Negativas:**
- Más código que un worker monolítico. Se mitiga con `SynchronizationServiceBase` que implementa el bucle común.
- El coordinador (`SynchronizationOrchestrator`) agrega una capa de indirección. El equipo debe entender el patrón.
- Si se ejecutan múltiples instancias del Worker, cada una ejecutará sus propios loops de sincronización, pudiendo causar escrituras duplicadas. Se mitiga con el diseño de upserts idempotentes en todos los repositorios.
