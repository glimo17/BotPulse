# Design — DemoProvider

## Resumen

El DemoProvider es un proveedor RPA completamente en memoria que implementa las 7 interfaces granulares de `BotPulse.Core`. No requiere credenciales, base de datos RPA ni conectividad externa. Se activa con `RpaProvider=Demo` (o por defecto cuando la clave no está presente).

Se referencia en **ADR-014** (`docs/ADR/0014-demo-provider.md`).

---

## Estructura del proyecto

```
src/BotPulse.Providers.Demo/
├── BotPulse.Providers.Demo.csproj
├── DemoOptions.cs
├── DemoDataSeed.cs
├── DemoRobotProvider.cs
├── DemoJobProvider.cs
├── DemoQueueProvider.cs
├── DemoLogProvider.cs
├── DemoMachineProvider.cs
├── DemoProcessProvider.cs
├── DemoAssetProvider.cs
├── HealthChecks/
│   └── DemoProviderHealthCheck.cs
└── DependencyInjection/
    └── DemoProviderRegistration.cs
```

**Proyecto de tests:**

```
tests/BotPulse.UnitTests/
└── Providers/
    └── DemoProviderTests.cs   ← property tests FsCheck
```

---

## BotPulse.Providers.Demo.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\BotPulse.Core\BotPulse.Core.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" />
    <PackageReference Include="Microsoft.Extensions.Options" />
  </ItemGroup>
</Project>
```

Solo depende de `BotPulse.Core`. No usa HTTP, EF Core ni Polly.

---

## DemoOptions.cs

Clase de opciones vacía reservada para extensión futura. Por ahora no hay parámetros configurables para Demo.

```csharp
namespace BotPulse.Providers.Demo;

/// <summary>Placeholder de opciones para el DemoProvider. Reservado para configuración futura.</summary>
public sealed class DemoOptions
{
    public const string SectionName = "Demo";
}
```

---

## DemoDataSeed.cs

El singleton central. Se construye una sola vez y mantiene todas las colecciones en memoria.

### Responsabilidades

1. Generar los datos de seed al construirse (llamado desde el constructor).
2. Exponer propiedades de acceso thread-safe a las colecciones.
3. Ejecutar mutaciones de simulación en tiempo real mediante un `System.Threading.Timer`.
4. Implementar `IDisposable` para disponer el timer.

### Colecciones internas

```csharp
private readonly object _lock = new();
private readonly List<RobotSnapshot> _robots;
private readonly List<MachineSnapshot> _machines;
private readonly List<ProcessSnapshot> _processes;
private readonly List<JobSnapshot> _jobs;
private readonly List<QueueSnapshot> _queues;
private readonly List<QueueItemSnapshot> _queueItems;
private readonly List<ExecutionLogSnapshot> _logs;
private readonly List<AssetMetadata> _assets;
private readonly List<ProcessParameter>[] _processParams; // indexed by proc position
private readonly Timer _timer;
private readonly Random _rng = new(42); // seed fijo para reproducibilidad inicial
```

Toda mutación va dentro de `lock (_lock)`. Las lecturas también van con lock para garantizar snapshots consistentes (el Timer puede mutar durante una lectura de lista).

### Datos de seed — Robots

```
ExternalId  Name            Status   MachineExternalId  LicenseType   LastHeartbeatUtc
robot-01    ROBOT-PROD-01   Idle     machine-01         Unattended    UtcNow - 1min
robot-02    ROBOT-PROD-02   Idle     machine-01         Unattended    UtcNow - 2min
robot-03    ROBOT-PROD-03   Idle     machine-02         Unattended    UtcNow - 3min
robot-04    ROBOT-PROD-04   Online   machine-02         Attended      UtcNow - 30s
robot-05    ROBOT-PROD-05   Online   machine-03         Attended      UtcNow - 45s
robot-06    ROBOT-PROD-06   Offline  machine-03         Unattended    UtcNow - 3h
```

### Datos de seed — Máquinas

```
ExternalId  Name        Status       LastHeartbeatUtc   ConnectedRobotCount
machine-01  SRV-BOT-01  Available    UtcNow - 1min      2
machine-02  SRV-BOT-02  Available    UtcNow - 2min      2
machine-03  SRV-BOT-03  Unavailable  UtcNow - 3h        2
```


### Datos de seed — Procesos

```
ExternalId  Name                      Version  PublicationStatus  CompatibleRobotCount  Description (ES)
proc-01     AP_InvoiceProcessing      2.3.1    Published          6                     Procesa facturas de proveedores automáticamente desde el portal SAP
proc-02     HR_OnboardingAutomation   1.5.0    Published          4                     Automatiza el onboarding de nuevos empleados en RRHH y Active Directory
proc-03     FIN_ReconciliationBot     3.1.2    Published          6                     Reconcilia transacciones financieras diarias contra el libro mayor
proc-04     IT_TicketRouter           1.0.4    Published          2                     Clasifica y asigna tickets de soporte de ServiceNow automáticamente
```

### Datos de seed — ProcessParameters por proceso

```
proc-01 (AP_InvoiceProcessing):
  - InvoiceDateFrom  DateTime  IsRequired=true   DefaultValue=null
  - BatchSize        Int32     IsRequired=false  DefaultValue="50"
  - DryRun           Boolean   IsRequired=false  DefaultValue="false"

proc-02 (HR_OnboardingAutomation):
  - EmployeeId       String    IsRequired=true   DefaultValue=null
  - Department       String    IsRequired=false  DefaultValue="General"

proc-03 (FIN_ReconciliationBot):
  - ReportDate       DateTime  IsRequired=true   DefaultValue=null
  - SendEmailReport  Boolean   IsRequired=false  DefaultValue="true"

proc-04 (IT_TicketRouter):
  - QueueName        String    IsRequired=false  DefaultValue="TIER1"
  - MaxTickets       Int32     IsRequired=false  DefaultValue="100"
```

### Datos de seed — Jobs

Generar 80 jobs distribuidos en las últimas 48 horas. Algoritmo de generación:

```
base = DateTime.UtcNow - 48h
Para i = 0..79:
  horasAtras = random(0, 48)
  startTime = base + TimeSpan.FromHours(horasAtras)
  proceso = procesos[i % 4]           // distribución round-robin entre los 4
  robot   = robots[i % 5]             // evita robot-06 (Offline) en jobs históricos
  
  // Distribuir estados: 72% Success, 18% Failed, 7% Stopped, 3% Running
  // Para los 3% Running: solo si startTime > UtcNow - 10min (jobs recientes)
  estado = DistribuirEstado(i)
  
  Si estado == "Success" o "Failed" o "Stopped":
    duracion = TimeSpan.FromMinutes(random(1, 45))
    endTime = startTime + duracion
  Si estado == "Running":
    duracion = null, endTime = null, startTime = UtcNow - random(30s, 90s)
  
  Si estado == "Failed":
    errorType = i % 2 == 0 ? "BusinessException" : "SystemException"
    errorMessage = MensajeAcordeProceso(proceso, errorType)
  
  ExternalId = $"job-{i+1:D3}"
```

Mensajes de error por proceso y tipo:
- AP_InvoiceProcessing + BusinessException: `"Factura duplicada detectada: número INV-{random} ya procesado"`
- AP_InvoiceProcessing + SystemException: `"Timeout al conectar con SAP BAPI BAPI_INCOMINGINVOICE_CREATE"`
- HR_OnboardingAutomation + BusinessException: `"Empleado {id} ya existe en Active Directory"`
- HR_OnboardingAutomation + SystemException: `"Error al crear buzón Exchange: servicio no disponible"`
- FIN_ReconciliationBot + BusinessException: `"Diferencia de reconciliación supera umbral: EUR {amount}"`
- FIN_ReconciliationBot + SystemException: `"Fallo de conexión a base de datos contabilidad: timeout"`
- IT_TicketRouter + BusinessException: `"Ticket {id} no tiene categoría asignable a ningún equipo"`
- IT_TicketRouter + SystemException: `"API ServiceNow retornó 503 Service Unavailable"`


### Datos de seed — Colas

```
ExternalId  Name              ProcessedItems  FailedItems  PendingItems  TotalItems
queue-01    FacturasEntrada   620             74           28            722
queue-02    PagosRecurrentes  245             7            5             257
queue-03    SolicitudesHR     95              5            3             103
```

`TotalItems = ProcessedItems + FailedItems + PendingItems` (invariante siempre mantenida).

### Datos de seed — QueueItems

Generar 30 `QueueItemSnapshot` distribuidos entre las 3 colas (10 por cola):

```
ExternalItemId  = $"qi-{cola}-{n:D2}"
QueueName       = nombre de la cola
Status          = "Processed" (7), "Failed" (2), "InProgress" (1) por cola
RetryCount      = 0 para Processed, 1-3 para Failed
ProcessingStartUtc/EndUtc = coherentes con Status y timestamps recientes
OriginalExternalItemId = null para ítems sin retry, o referencia para retries
OutputMetadataJson = "{}" o JSON simple para Processed
```

### Datos de seed — Assets

```
ExternalId  Name                    Type        Scope   LastModifiedUtc
asset-01    SAP_ServiceAccount      Credential  Global  UtcNow - 5d
asset-02    Exchange_Credentials    Credential  Robot   UtcNow - 10d
asset-03    SAP_BaseUrl             Text        Global  UtcNow - 30d
asset-04    HR_SystemEndpoint       Text        Global  UtcNow - 15d
asset-05    AlertEmail_Recipients   Text        Global  UtcNow - 2d
```

### Datos de seed — Logs

Para cada job generado, crear entre 8 y 15 `ExecutionLogSnapshot`. Plantilla de mensajes por proceso:

**AP_InvoiceProcessing** (LoggerName: `"AP.InvoiceProcessing"`):
1. Info — `"Iniciando procesamiento de facturas pendientes"`
2. Info — `"Conectando a SAP sistema de cuentas por pagar"`
3. Info — `"Facturas encontradas en bandeja: {n}"`
4. Info — `"Procesando factura {id} de proveedor {vendor}"`
5. Info — `"Factura {id} validada exitosamente"`
6. Info — `"Importe total procesado: EUR {amount}"`
7. Info — `"Cerrando sesión SAP"`
8. Info — `"Procesamiento completado. Facturas: {ok} OK, {err} errores"`
+ Si Failed: Warning → `"Advertencia: {n} facturas requieren revisión manual"` y Error → mensaje de error

**HR_OnboardingAutomation** (LoggerName: `"HR.Onboarding"`):
1. Info — `"Iniciando proceso de onboarding para empleado {id}"`
2. Info — `"Creando cuenta de usuario en Active Directory"`
3. Info — `"Asignando grupos de seguridad según departamento {dept}"`
4. Info — `"Configurando buzón de correo Exchange"`
5. Info — `"Provisionando acceso a sistemas corporativos"`
6. Info — `"Enviando email de bienvenida a {email}"`
7. Info — `"Onboarding completado satisfactoriamente"`
+ Si Failed: Error → mensaje de error

**FIN_ReconciliationBot** (LoggerName: `"FIN.Reconciliation"`):
1. Info — `"Iniciando reconciliación para fecha {date}"`
2. Info — `"Descargando extracto bancario: {n} transacciones"`
3. Info — `"Cargando libro mayor contable"`
4. Info — `"Comparando {n} registros"`
5. Warning — `"Se encontraron {n} diferencias menores (< EUR 1)"`
6. Info — `"Diferencias dentro del umbral permitido"`
7. Info — `"Generando informe de reconciliación"`
8. Info — `"Reconciliación completada. Coincidencias: {pct}%"`

**IT_TicketRouter** (LoggerName: `"IT.TicketRouter"`):
1. Info — `"Conectando a cola ServiceNow {queue}"`
2. Info — `"Tickets pendientes de clasificación: {n}"`
3. Info — `"Analizando ticket {id}: {title}"`
4. Info — `"Ticket {id} clasificado como {category}, asignando a {team}"`
5. Info — `"Enviando notificación al equipo {team}"`
6. Info — `"Tickets procesados: {n}"`


### Timer de simulación (cada 30 segundos)

```csharp
private void OnTimerTick(object? _)
{
    lock (_lock)
    {
        RotateRobotStatus();
        CompleteStaleRunningJobs();
        FluctuateQueuePendingItems();
        MaybeAddNewRunningJob();
    }
}

private void RotateRobotStatus()
{
    // Toma el primer robot Idle, lo cambia a Busy y actualiza heartbeat.
    // Si no hay Idle, toma el primer Busy y lo vuelve Idle.
    var idleRobot = _robots.FirstOrDefault(r => r.Status == "Idle");
    if (idleRobot != null)
        MutateRobot(idleRobot, "Busy");
    else
    {
        var busyRobot = _robots.FirstOrDefault(r => r.Status == "Busy");
        if (busyRobot != null) MutateRobot(busyRobot, "Idle");
    }
}
// Nota: RobotSnapshot es record immutable → reemplazar el ítem en _robots con
// un nuevo record con el campo mutado (robots[i] = robots[i] with { Status = s, LastHeartbeatUtc = now })

private void CompleteStaleRunningJobs()
{
    var cutoff = DateTime.UtcNow.AddMinutes(-2);
    for (int i = 0; i < _jobs.Count; i++)
    {
        var j = _jobs[i];
        if (j.Status == "Running" && j.StartTimeUtc < cutoff)
        {
            var success = _rng.NextDouble() < 0.9;
            var endTime = DateTime.UtcNow;
            _jobs[i] = j with
            {
                Status = success ? "Success" : "Failed",
                EndTimeUtc = endTime,
                Duration = endTime - j.StartTimeUtc,
                ErrorType = success ? null : (_rng.Next(2) == 0 ? "BusinessException" : "SystemException"),
                ErrorMessage = success ? null : "Error durante simulación de ejecución"
            };
        }
    }
}

private void FluctuateQueuePendingItems()
{
    for (int i = 0; i < _queues.Count; i++)
    {
        var q = _queues[i];
        int delta = _rng.Next(-3, 4); // -3 a +3
        int newPending = Math.Max(0, q.PendingItems + delta);
        int newTotal = q.ProcessedItems + q.FailedItems + newPending;
        _queues[i] = q with { PendingItems = newPending, TotalItems = newTotal };
    }
}

private void MaybeAddNewRunningJob()
{
    if (_rng.NextDouble() >= 0.5) return;
    var proc = _processes[_rng.Next(_processes.Count)];
    var availableRobots = _robots.Where(r => r.Status is "Idle" or "Online").ToList();
    if (availableRobots.Count == 0) return;
    var robot = availableRobots[_rng.Next(availableRobots.Count)];
    var job = new JobSnapshot(
        ExternalId: $"job-sim-{Guid.NewGuid():N}",
        ProcessExternalId: proc.ExternalId,
        RobotExternalId: robot.ExternalId,
        MachineExternalId: robot.MachineExternalId,
        Status: "Running",
        StartTimeUtc: DateTime.UtcNow,
        EndTimeUtc: null,
        Duration: null,
        ErrorType: null,
        ErrorMessage: null);
    _jobs.Add(job);
    // Generar logs iniciales para este job
    _logs.AddRange(GenerateInitialLogs(job));
}
```

### Exposición thread-safe de colecciones

Todos los métodos públicos del seed devuelven copias de lista:

```csharp
public IReadOnlyList<RobotSnapshot> GetRobots()
{
    lock (_lock) return _robots.ToList();
}
// Ídem para GetMachines(), GetProcesses(), GetJobs(), GetQueues(),
// GetQueueItems(), GetLogs(), GetAssets(), GetProcessParameters(procId)
```

---

## DemoRobotProvider.cs

```csharp
internal sealed class DemoRobotProvider : IRobotProvider
{
    private readonly DemoDataSeed _seed;
    public DemoRobotProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<RobotSnapshot>> GetRobotsAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetRobots());

    public Task<RobotSnapshot?> GetRobotByIdAsync(string externalId, CancellationToken ct = default)
    {
        var robot = _seed.GetRobots().FirstOrDefault(r => r.ExternalId == externalId);
        return Task.FromResult(robot);
    }
}
```

---

## DemoJobProvider.cs

```csharp
internal sealed class DemoJobProvider : IJobProvider
{
    private readonly DemoDataSeed _seed;
    public DemoJobProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<JobSnapshot>> GetJobsAsync(JobQuery query, CancellationToken ct = default)
    {
        var jobs = _seed.GetJobs().AsEnumerable();
        if (query.UpdatedSinceUtc.HasValue)
            jobs = jobs.Where(j => j.StartTimeUtc >= query.UpdatedSinceUtc ||
                                   j.EndTimeUtc >= query.UpdatedSinceUtc);
        if (!string.IsNullOrEmpty(query.Status))
            jobs = jobs.Where(j => j.Status == query.Status);
        if (!string.IsNullOrEmpty(query.RobotExternalId))
            jobs = jobs.Where(j => j.RobotExternalId == query.RobotExternalId);
        if (!string.IsNullOrEmpty(query.ProcessExternalId))
            jobs = jobs.Where(j => j.ProcessExternalId == query.ProcessExternalId);
        jobs = jobs.Skip(query.Skip);
        if (query.Top > 0)
            jobs = jobs.Take(query.Top);
        return Task.FromResult<IReadOnlyList<JobSnapshot>>(jobs.ToList());
    }

    public Task<JobSnapshot?> GetJobByIdAsync(string externalId, CancellationToken ct = default)
    {
        var job = _seed.GetJobs().FirstOrDefault(j => j.ExternalId == externalId);
        return Task.FromResult(job);
    }

    public Task<StartJobResult> StartJobAsync(StartJobRequest request, CancellationToken ct = default)
        => Task.FromResult(_seed.StartJob(request));

    public Task StopJobAsync(string externalId, CancellationToken ct = default)
    {
        _seed.TransitionJob(externalId, "Stopped");
        return Task.CompletedTask;
    }

    public Task CancelJobAsync(string externalId, CancellationToken ct = default)
    {
        _seed.TransitionJob(externalId, "Cancelled");
        return Task.CompletedTask;
    }
}
```

`DemoDataSeed.StartJob(request)` valida que el proceso exista y crea el job en memoria.  
`DemoDataSeed.TransitionJob(externalId, newStatus)` cambia estado solo si el job existe y está en `"Running"`.


---

## DemoQueueProvider.cs

```csharp
internal sealed class DemoQueueProvider : IQueueProvider
{
    private readonly DemoDataSeed _seed;
    public DemoQueueProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<QueueSnapshot>> GetQueuesAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetQueues());

    public Task<IReadOnlyList<QueueItemSnapshot>> GetQueueItemsAsync(
        QueueItemQuery query, CancellationToken ct = default)
    {
        var items = _seed.GetQueueItems().AsEnumerable();
        if (!string.IsNullOrEmpty(query.QueueName))
            items = items.Where(qi => qi.QueueName == query.QueueName);
        if (query.UpdatedSinceUtc.HasValue)
            items = items.Where(qi => qi.ProcessingStartUtc >= query.UpdatedSinceUtc ||
                                      qi.ProcessingEndUtc >= query.UpdatedSinceUtc);
        if (query.Top > 0)
            items = items.Take(query.Top);
        return Task.FromResult<IReadOnlyList<QueueItemSnapshot>>(items.ToList());
    }
}
```

---

## DemoLogProvider.cs

```csharp
internal sealed class DemoLogProvider : ILogProvider
{
    private readonly DemoDataSeed _seed;
    public DemoLogProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<ExecutionLogSnapshot>> GetExecutionLogsAsync(
        LogQuery query, CancellationToken ct = default)
    {
        var logs = _seed.GetLogs().AsEnumerable();
        if (!string.IsNullOrEmpty(query.JobExternalId))
            logs = logs.Where(l => l.JobExternalId == query.JobExternalId);
        if (query.FromUtc.HasValue)
            logs = logs.Where(l => l.TimestampUtc >= query.FromUtc);
        if (query.ToUtc.HasValue)
            logs = logs.Where(l => l.TimestampUtc <= query.ToUtc);
        if (query.Top > 0)
            logs = logs.Take(query.Top);
        return Task.FromResult<IReadOnlyList<ExecutionLogSnapshot>>(logs.ToList());
    }
}
```

---

## DemoMachineProvider.cs

```csharp
internal sealed class DemoMachineProvider : IMachineProvider
{
    private readonly DemoDataSeed _seed;
    public DemoMachineProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<MachineSnapshot>> GetMachinesAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetMachines());

    public Task<MachineSnapshot?> GetMachineByIdAsync(string externalId, CancellationToken ct = default)
    {
        var machine = _seed.GetMachines().FirstOrDefault(m => m.ExternalId == externalId);
        return Task.FromResult(machine);
    }
}
```

---

## DemoProcessProvider.cs

```csharp
internal sealed class DemoProcessProvider : IProcessProvider
{
    private readonly DemoDataSeed _seed;
    public DemoProcessProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<ProcessSnapshot>> GetProcessesAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetProcesses());

    public Task<IReadOnlyList<ProcessParameter>> GetProcessParametersAsync(
        string processExternalId, CancellationToken ct = default)
        => Task.FromResult(_seed.GetProcessParameters(processExternalId));
}
```

---

## DemoAssetProvider.cs

```csharp
internal sealed class DemoAssetProvider : IAssetProvider
{
    private readonly DemoDataSeed _seed;
    public DemoAssetProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<AssetMetadata>> GetAssetsAsync(CancellationToken ct = default)
        => Task.FromResult(_seed.GetAssets());
}
```

---

## HealthChecks/DemoProviderHealthCheck.cs

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BotPulse.Providers.Demo.HealthChecks;

internal sealed class DemoProviderHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
        => Task.FromResult(HealthCheckResult.Healthy("Demo provider active"));
}
```

Siempre retorna Healthy. No requiere ninguna dependencia externa.


---

## DependencyInjection/DemoProviderRegistration.cs

```csharp
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Providers.Demo.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BotPulse.Providers.Demo.DependencyInjection;

public static class DemoProviderRegistration
{
    /// <summary>
    /// Registra el DemoProvider en memoria con los 7 providers granulares.
    /// No requiere configuración de credenciales externas.
    /// </summary>
    public static IServiceCollection AddDemoProvider(this IServiceCollection services)
    {
        services.AddSingleton<DemoDataSeed>();

        services.AddScoped<IRobotProvider, DemoRobotProvider>();
        services.AddScoped<IJobProvider, DemoJobProvider>();
        services.AddScoped<IQueueProvider, DemoQueueProvider>();
        services.AddScoped<ILogProvider, DemoLogProvider>();
        services.AddScoped<IAssetProvider, DemoAssetProvider>();
        services.AddScoped<IMachineProvider, DemoMachineProvider>();
        services.AddScoped<IProcessProvider, DemoProcessProvider>();

        return services;
    }

    /// <summary>
    /// Añade el health check del DemoProvider (siempre Healthy).
    /// </summary>
    public static IHealthChecksBuilder AddDemoHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "rpa-provider",
        params string[] tags)
        => builder.AddCheck<DemoProviderHealthCheck>(name, tags: tags);
}
```

---

## Cambios en Program.cs (API)

Reemplazar la línea `builder.Services.AddUiPathProvider(builder.Configuration);` con:

```csharp
// RPA Provider — selección por configuración
var rpaProvider = builder.Configuration["RpaProvider"] ?? "Demo";
if (rpaProvider.Equals("UiPath", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddUiPathProvider(builder.Configuration);
}
else
{
    builder.Services.AddDemoProvider();
}
```

Y para el health check, reemplazar `.AddUiPathHealthCheck("rpa-provider", "ready")` con:

```csharp
var hcBuilder = builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("PostgreSQL") ?? string.Empty,
               name: "database", tags: ["ready"]);

if (rpaProvider.Equals("UiPath", StringComparison.OrdinalIgnoreCase))
    hcBuilder.AddUiPathHealthCheck("rpa-provider", "ready");
else
    hcBuilder.AddDemoHealthCheck("rpa-provider", "ready");
```

Agregar using al inicio:

```csharp
using BotPulse.Providers.Demo.DependencyInjection;
```

---

## Cambios en Program.cs (Worker)

Reemplazar `services.AddUiPathProvider(context.Configuration);` con:

```csharp
var rpaProvider = context.Configuration["RpaProvider"] ?? "Demo";
if (rpaProvider.Equals("UiPath", StringComparison.OrdinalIgnoreCase))
    services.AddUiPathProvider(context.Configuration);
else
    services.AddDemoProvider();
```

Agregar using:

```csharp
using BotPulse.Providers.Demo.DependencyInjection;
```

**Nota sobre el Worker con Demo:** Los servicios de sincronización (`JobSynchronizationService`, `QueueItemSynchronizationService`, `LogSynchronizationService`) invocan los providers vía DI. Con DemoProvider, las llamadas retornan datos en memoria y los workers escribirán esos datos en PostgreSQL — comportamiento correcto para pruebas de extremo a extremo.

---

## Cambios en .env.example

Añadir sección antes de `# ===== UIPATH PROVIDER =====`:

```dotenv
# ===== RPA PROVIDER =====
# Values: Demo (default, no credentials needed) | UiPath
RPA_PROVIDER=Demo
```

La clave de env `RPA_PROVIDER` se mapea a `RpaProvider` en la configuración .NET mediante el separador por defecto de las variables de entorno del host (doble guion bajo para secciones, pero `RpaProvider` es clave raíz, por lo que `RPA_PROVIDER` puede necesitar configurarse también como `RpaProvider` directamente). Usar `environment:` en docker-compose:

```yaml
- RpaProvider=${RPA_PROVIDER:-Demo}
```

Verificar que `docker-compose.yml` pasa `RpaProvider` a los contenedores API y Worker.


---

## Cambios en BotPulse.sln

Añadir el nuevo proyecto al solution file con un nuevo GUID:

```
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "BotPulse.Providers.Demo",
    "src\BotPulse.Providers.Demo\BotPulse.Providers.Demo.csproj",
    "{NUEVO-GUID-AQUI}"
EndProject
```

El proyecto debe anidarse en la carpeta de solución `src`:

```
{NUEVO-GUID-AQUI} = {827E0CD3-B72D-47B6-A68D-7590B98EB39B}
```

Además, `BotPulse.Api.csproj` y `BotPulse.Worker.csproj` deben agregar:

```xml
<ProjectReference Include="..\BotPulse.Providers.Demo\BotPulse.Providers.Demo.csproj" />
```

---

## Cambios en BotPulse.UnitTests.csproj

Añadir referencia al nuevo proyecto de providers:

```xml
<ProjectReference Include="..\..\src\BotPulse.Providers.Demo\BotPulse.Providers.Demo.csproj" />
```

---

## ADR-014

Crear `docs/ADR/0014-demo-provider.md` con la decisión de introducir un proveedor Demo en memoria para habilitar desarrollo y demos sin credenciales externas.

---

## Property Tests (FsCheck + xUnit)

Archivo: `tests/BotPulse.UnitTests/Providers/DemoProviderTests.cs`

Framework: **FsCheck.Xunit** (ya presente en `Directory.Packages.props`).

Las 7 propiedades a verificar corresponden a los 7 providers. Todas usan una única instancia de `DemoDataSeed` construida sin timer activo (o con timer de 0 período), para evitar mutaciones durante los tests.

### Propiedad 1 — RobotProvider: todos los robots tienen ExternalId único y no vacío

```
**Validates: Requirements REQ-2.3, REQ-3.2**
Para cualquier lista de robots retornada por GetRobotsAsync():
  - Ningún ExternalId es null o vacío
  - Todos los ExternalId son distintos
  - Todos los Status son uno de: "Idle", "Busy", "Online", "Offline"
```

### Propiedad 2 — JobProvider: StartJob siempre retorna un ExternalId válido

```
**Validates: Requirements REQ-7.1, REQ-7.2**
Para cualquier ProcessExternalId conocido en el seed:
  - StartJobAsync retorna StartJobResult con ExternalId no vacío
  - El job aparece en GetJobsAsync() con Status == "Running"
Para ProcessExternalId desconocido:
  - StartJobAsync lanza InvalidOperationException
```

### Propiedad 3 — JobProvider: StopJob y CancelJob son idempotentes

```
**Validates: Requirements REQ-7.3, REQ-7.4**
Llamar StopJobAsync o CancelJobAsync con cualquier string (incluido id inexistente)
no lanza excepción.
```

### Propiedad 4 — QueueProvider: TotalItems == ProcessedItems + FailedItems + PendingItems

```
**Validates: Requirements REQ-3.7**
Para toda QueueSnapshot retornada por GetQueuesAsync():
  TotalItems == ProcessedItems + FailedItems + PendingItems
```

### Propiedad 5 — LogProvider: filtro por JobExternalId retorna solo logs de ese job

```
**Validates: Requirements REQ-8.2**
Para cualquier JobExternalId existente en el seed:
  GetExecutionLogsAsync(new LogQuery(JobExternalId: id)) retorna solo logs
  donde log.JobExternalId == id
```

### Propiedad 6 — MachineProvider: GetMachineByIdAsync retorna null para IDs desconocidos

```
**Validates: Requirements REQ-9.2**
Para cualquier string que NO sea "machine-01", "machine-02", "machine-03":
  GetMachineByIdAsync retorna null sin lanzar excepción
```

### Propiedad 7 — ProcessProvider: GetProcessesAsync retorna exactamente 4 procesos con versiones semver válidas

```
**Validates: Requirements REQ-3.4**
GetProcessesAsync() retorna exactamente 4 procesos.
Cada Version cumple el patrón semver: \d+\.\d+\.\d+
Todos los PublicationStatus son "Published".
```

---

## Diagrama de dependencias

```
BotPulse.Api ──────────────────────┐
BotPulse.Worker ──────────────────┬┤
                                  ││
                     ┌────────────▼▼──────────────┐
                     │  BotPulse.Providers.Demo   │
                     │  (DemoProviderRegistration) │
                     └──────────────┬─────────────┘
                                    │ depends on
                     ┌──────────────▼─────────────┐
                     │       BotPulse.Core         │
                     │  (7 interfaces + models)    │
                     └────────────────────────────┘
```

No hay dependencia circular. `BotPulse.Providers.Demo` solo referencia `BotPulse.Core`.

---

## Consideraciones de seguridad

- El DemoProvider no debe activarse en producción. La selección por `RpaProvider=Demo` como default solo aplica en ausencia de configuración explícita.
- Los datos de assets de tipo `Credential` solo exponen metadata (`AssetMetadata`), nunca valores secretos — idéntico al contrato de `IAssetProvider` en producción.
- No hay persistencia de los datos en memoria fuera del proceso. Al reiniciar el contenedor, los datos se regeneran.

---

## Decisiones de diseño destacadas

| Decisión | Alternativa considerada | Razón elegida |
|---|---|---|
| `lock` sobre `ConcurrentDictionary` | `ConcurrentDictionary` por colección | Los records inmutables hacen que el reemplazo de ítems sea más legible con lock. El contention es mínimo (timer cada 30s). |
| Timer en el seed, no en un `IHostedService` | `BackgroundService` separado | Simplifica el registro DI y evita depender de `IHostedService` que requeriría `IHost` en Worker. |
| Providers como `Scoped`, seed como `Singleton` | Todo Singleton | Consistente con el patrón UiPath. Los providers son baratos y sin estado propio. |
| `Task.FromResult` en todos los métodos | `ValueTask` | Consistencia con las interfaces existentes que retornan `Task`. |
