# Tasks — DemoProvider

- [x] 1. Crear proyecto `BotPulse.Providers.Demo`
  - Crear directorio `src/BotPulse.Providers.Demo/`
  - Crear `BotPulse.Providers.Demo.csproj` con `TargetFramework=net8.0`, `Nullable=enable`, `ImplicitUsings=enable`, referencia a `BotPulse.Core` y paquetes: `Microsoft.Extensions.Logging.Abstractions`, `Microsoft.Extensions.Diagnostics.HealthChecks`, `Microsoft.Extensions.Options`
  - Crear `DemoOptions.cs` con clase vacía `DemoOptions` y constante `SectionName = "Demo"`
  - Verificar que el proyecto compila: `dotnet build src/BotPulse.Providers.Demo/BotPulse.Providers.Demo.csproj`

  **Acceptance criteria:** El proyecto compila sin errores. No referencia paquetes HTTP ni EF Core.

- [x] 2. Implementar `DemoDataSeed`
  - Crear `DemoDataSeed.cs` en `src/BotPulse.Providers.Demo/`
  - Implementar `IDisposable`
  - Generar en el constructor los datos de seed completos: 6 robots, 3 máquinas, 4 procesos con sus `ProcessParameter`, ~80 jobs distribuidos en 48h con distribución de estados (72% Success / 18% Failed / 7% Stopped / 3% Running), 3 colas con sus QueueItems (30 total), 5 assets, logs (8–15 por job)
  - Los jobs Failed deben tener `ErrorType` y `ErrorMessage` según el proceso asignado (ver diseño)
  - Implementar `System.Threading.Timer` con periodo de 30 segundos ejecutando: `RotateRobotStatus()`, `CompleteStaleRunningJobs()` (jobs Running > 2min), `FluctuateQueuePendingItems()` (±0-3 sin negativos), `MaybeAddNewRunningJob()` (50% probabilidad)
  - Toda mutación y lectura debe ocurrir dentro de `lock (_lock)`
  - Exponer métodos públicos de lectura: `GetRobots()`, `GetMachines()`, `GetProcesses()`, `GetProcessParameters(procId)`, `GetJobs()`, `GetQueues()`, `GetQueueItems()`, `GetLogs()`, `GetAssets()`
  - Exponer métodos de escritura: `StartJob(StartJobRequest)` (valida proceso existente, lanza `InvalidOperationException` si no existe, crea job Running, genera logs iniciales, retorna `StartJobResult`), `TransitionJob(externalId, newStatus)` (cambia estado solo si job existe y está en Running)
  - Verificar invariante: `TotalItems == ProcessedItems + FailedItems + PendingItems` en todas las colas tras toda mutación
  - Implementar `Dispose()` que dispone el Timer

  **Acceptance criteria:** `DemoDataSeed` construido en un test de instanciación retorna: exactamente 6 robots, 3 máquinas, 4 procesos, ≥ 75 jobs, 3 colas, 5 assets, ≥ 600 logs. Cada cola cumple `TotalItems == ProcessedItems + FailedItems + PendingItems`.

- [x] 3. Implementar los 7 providers
  - Crear `DemoRobotProvider.cs` implementando `IRobotProvider`: `GetRobotsAsync` delega a `_seed.GetRobots()`; `GetRobotByIdAsync` busca por ExternalId y retorna null si no existe
  - Crear `DemoMachineProvider.cs` implementando `IMachineProvider`: ídem patrón Robot
  - Crear `DemoProcessProvider.cs` implementando `IProcessProvider`: `GetProcessesAsync` y `GetProcessParametersAsync` (lista vacía para procesos desconocidos)
  - Crear `DemoAssetProvider.cs` implementando `IAssetProvider`: `GetAssetsAsync` delega a seed
  - Crear `DemoJobProvider.cs` implementando `IJobProvider`: `GetJobsAsync` aplica todos los filtros de `JobQuery` (UpdatedSinceUtc, Status, RobotExternalId, ProcessExternalId, Skip, Top); `GetJobByIdAsync` busca por ExternalId; `StartJobAsync` delega a `_seed.StartJob`; `StopJobAsync` y `CancelJobAsync` delegan a `_seed.TransitionJob`
  - Crear `DemoQueueProvider.cs` implementando `IQueueProvider`: `GetQueuesAsync` retorna todas; `GetQueueItemsAsync` filtra por QueueName si presente, aplica UpdatedSinceUtc y Top
  - Crear `DemoLogProvider.cs` implementando `ILogProvider`: `GetExecutionLogsAsync` filtra por JobExternalId, FromUtc, ToUtc y aplica Top
  - Todos los providers deben ser `internal sealed class` e inyectar `DemoDataSeed` por constructor
  - Todos los métodos retornan `Task.FromResult(...)` — sin async/await innecesario

  **Acceptance criteria:** Todos los providers compilan. `DemoJobProvider.GetJobsAsync(new JobQuery(Status: "Failed"))` retorna solo jobs con Status == "Failed". `DemoLogProvider` filtrando por un JobExternalId existente retorna solo logs de ese job.

- [x] 4. Implementar `DemoProviderHealthCheck` y `DemoProviderRegistration`
  - Crear `HealthChecks/DemoProviderHealthCheck.cs` como `internal sealed class` implementando `IHealthCheck`, retornando siempre `HealthCheckResult.Healthy("Demo provider active")`
  - Crear `DependencyInjection/DemoProviderRegistration.cs` con método de extensión `AddDemoProvider(this IServiceCollection)` que registra: `DemoDataSeed` como Singleton; los 7 providers como Scoped; retorna `IServiceCollection`
  - Crear método de extensión `AddDemoHealthCheck(this IHealthChecksBuilder, string name, params string[] tags)` que registra `DemoProviderHealthCheck`

  **Acceptance criteria:** `services.AddDemoProvider()` no lanza excepción en un `ServiceCollection` vacío. El health check retorna `Healthy` sin dependencias externas.

- [x] 5. Modificar `Program.cs` de API y Worker para selección condicional de provider
  - En `src/BotPulse.Api/Program.cs`:
    - Agregar `using BotPulse.Providers.Demo.DependencyInjection;`
    - Leer `var rpaProvider = builder.Configuration["RpaProvider"] ?? "Demo";` **antes** de registrar el provider
    - Reemplazar la llamada fija `builder.Services.AddUiPathProvider(...)` con el bloque condicional UiPath/Demo
    - Reemplazar `.AddUiPathHealthCheck(...)` con el bloque condicional de health check (UiPath o Demo según `rpaProvider`)
  - En `src/BotPulse.Worker/Program.cs`:
    - Agregar `using BotPulse.Providers.Demo.DependencyInjection;`
    - Leer `var rpaProvider = context.Configuration["RpaProvider"] ?? "Demo";`
    - Reemplazar la llamada fija con bloque condicional UiPath/Demo
  - Agregar `ProjectReference` a `BotPulse.Providers.Demo` en `BotPulse.Api.csproj` y `BotPulse.Worker.csproj`
  - Verificar que ambos proyectos compilan: `dotnet build src/BotPulse.Api` y `dotnet build src/BotPulse.Worker`

  **Acceptance criteria:** `dotnet build` de API y Worker pasa sin errores. Con `RpaProvider` no configurado, el DemoProvider se activa por defecto (sin requerir claves UiPath).

- [x] 6. Actualizar `.env.example`
  - Agregar sección `# ===== RPA PROVIDER =====` antes de la sección UiPath
  - Añadir `RPA_PROVIDER=Demo` con comentario: `# Values: Demo (default, sin credenciales) | UiPath`
  - Añadir nota aclarando que `RPA_PROVIDER=UiPath` requiere las claves `UiPath__*` configuradas

  **Acceptance criteria:** El archivo `.env.example` tiene la clave `RPA_PROVIDER=Demo` y el comentario es claro para un developer que lo ve por primera vez.

- [x] 7. Agregar `BotPulse.Providers.Demo` a la solución
  - Ejecutar `dotnet sln BotPulse.sln add src/BotPulse.Providers.Demo/BotPulse.Providers.Demo.csproj` o editando manualmente el `.sln`
  - Verificar que el proyecto aparece bajo la carpeta de solución `src` (NestedProjects section)
  - Ejecutar `dotnet build BotPulse.sln` para verificar que toda la solución compila

  **Acceptance criteria:** `dotnet build BotPulse.sln` pasa sin errores. El proyecto aparece en la carpeta `src` dentro del solution explorer.

- [x] 8. Crear ADR-014
  - Crear `docs/ADR/0014-demo-provider.md`
  - Contenido mínimo: título, fecha, estado (Accepted), contexto (necesidad de desarrollo/demo sin credenciales externas), decisión (proveedor en memoria activable por configuración), consecuencias (facilita onboarding y CI; no usar en producción), alternativas consideradas (WireMock, datos estáticos JSON)

  **Acceptance criteria:** El archivo existe y sigue el mismo formato que los ADRs existentes (0001–0013).

- [x] 9. Verificar que `docker compose config` sigue siendo válido
  - Revisar `docker-compose.yml` para confirmar que las variables de entorno `RpaProvider` o `RPA_PROVIDER` se pasan correctamente a los servicios `api` y `worker`
  - Si no están presentes, agregar `- RpaProvider=${RPA_PROVIDER:-Demo}` en la sección `environment` de ambos servicios
  - Ejecutar `docker compose config` y verificar que no hay errores de schema
  - Verificar que con `RPA_PROVIDER=Demo` en el `.env`, docker compose muestra `RpaProvider: Demo` en la config interpolada

  **Acceptance criteria:** `docker compose config` retorna exit code 0 sin warnings de variables indefinidas.

- [x] 10. Implementar property tests para las 7 propiedades de correctness del DemoProvider
  - Crear `tests/BotPulse.UnitTests/Providers/DemoProviderTests.cs`
  - Agregar `ProjectReference` a `BotPulse.Providers.Demo` en `BotPulse.UnitTests.csproj`
  - Implementar las 7 propiedades usando `FsCheck.Xunit` (`[Property]` attribute):

    **Propiedad 1 — Robots.UniqueNonEmptyExternalIds** `**Validates: Requirements REQ-2.3, REQ-3.2**`
    Verifica que todos los robots tienen ExternalId único, no vacío, y Status en el conjunto válido {"Idle", "Busy", "Online", "Offline"}.

    **Propiedad 2 — Jobs.StartJobReturnsValidId** `**Validates: Requirements REQ-7.1, REQ-7.2**`
    Para cada proceso conocido, `StartJobAsync` retorna un ExternalId no vacío y el job aparece en `GetJobsAsync()` con Status == "Running". Para proceso desconocido, lanza `InvalidOperationException`.

    **Propiedad 3 — Jobs.StopCancelAreIdempotent** `**Validates: Requirements REQ-7.3, REQ-7.4**`
    `StopJobAsync` y `CancelJobAsync` con cualquier string como argumento no lanzan excepción.

    **Propiedad 4 — Queues.TotalItemsInvariant** `**Validates: Requirements REQ-3.7**`
    Para toda `QueueSnapshot`, `TotalItems == ProcessedItems + FailedItems + PendingItems`.

    **Propiedad 5 — Logs.FilterByJobIdReturnsOnlyMatchingLogs** `**Validates: Requirements REQ-8.2**`
    Para cualquier JobExternalId en el seed, filtrar logs por ese ID retorna solo entradas donde `log.JobExternalId == id`.

    **Propiedad 6 — Machines.GetByIdReturnsNullForUnknown** `**Validates: Requirements REQ-9.2**`
    Para strings que no son IDs de máquinas conocidas, `GetMachineByIdAsync` retorna null.

    **Propiedad 7 — Processes.ExactlyFourPublishedWithSemver** `**Validates: Requirements REQ-3.4**`
    `GetProcessesAsync()` retorna exactamente 4 procesos, todos con `PublicationStatus == "Published"` y `Version` que cumple `\d+\.\d+\.\d+`.

  - Los tests deben instanciar `DemoDataSeed` directamente (sin timer activo o con timer detenido) para evitar mutaciones durante la ejecución del test
  - Ejecutar: `dotnet test tests/BotPulse.UnitTests --filter "Category=DemoProvider"`

  **Acceptance criteria:** Las 7 propiedades pasan. Ningún test usa mocks para el seed — se valida la implementación real.
