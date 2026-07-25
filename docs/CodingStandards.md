# BotPulse — Coding Standards

Este documento describe los estándares de código mandatorios para todos los proyectos del repositorio `BotPulse`. Toda contribución debe cumplirlos antes de ser considerada para merge.

---

## 1. Async/Await Everywhere

Todo I/O (base de datos, HTTP, sistema de archivos, colas) debe ser asíncrono.

**Reglas:**
- Prohibido usar `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` en código de producción.
- Todo método que realice I/O debe devolver `Task` o `Task<T>` y recibir un `CancellationToken` como último parámetro.
- Usar `.ConfigureAwait(false)` en código de librería (Core, Infrastructure, Providers). En código de API controller no es necesario.
- Prohibido bloquear el hilo de llamada de ninguna forma.

```csharp
// BIEN
public async Task<IReadOnlyList<RobotSnapshot>> GetRobotsAsync(CancellationToken ct)
{
    return await _provider.GetRobotsAsync(ct).ConfigureAwait(false);
}

// MAL — causa deadlocks
public IReadOnlyList<RobotSnapshot> GetRobots()
{
    return _provider.GetRobotsAsync(CancellationToken.None).Result; // PROHIBIDO
}
```

---

## 2. Dependency Injection Only

Todas las dependencias se resuelven mediante el contenedor DI.

**Reglas:**
- Prohibido usar Service Locator (`IServiceProvider` dentro de constructores o métodos de negocio).
- Prohibido el estado estático (`static` fields con datos compartidos).
- Prohibido el patrón Singleton implementado a mano. Usar `services.AddSingleton<T>()`.
- Toda clase con dependencias debe recibirlas por constructor.
- Los extension methods de registro DI viven en clases `XxxServiceCollectionExtensions`.

```csharp
// BIEN
public sealed class RobotQueryService
{
    private readonly IRobotProvider _provider;
    private readonly ICacheService _cache;

    public RobotQueryService(IRobotProvider provider, ICacheService cache)
    {
        _provider = provider;
        _cache = cache;
    }
}

// MAL
public sealed class RobotQueryService
{
    public async Task<IReadOnlyList<RobotSnapshot>> GetAsync(IServiceProvider sp)
    {
        var provider = sp.GetRequiredService<IRobotProvider>(); // Service Locator — PROHIBIDO
        return await provider.GetRobotsAsync(default);
    }
}
```

---

## 3. Thin Controllers

Los controllers de la API son delgados. Su única responsabilidad es traducir la petición HTTP al Application Service y devolver la respuesta apropiada.

**Reglas:**
- Prohibido incluir lógica de negocio en controllers.
- Prohibido acceder directamente a repositorios o DbContext desde un controller.
- El controller llama al Application Service, mapea el resultado a un DTO de respuesta y devuelve el código HTTP adecuado.
- La validación de entrada se hace con FluentValidation (o Data Annotations), no con lógica manual en el controller.

```csharp
// BIEN
[HttpPost]
[Authorize(Policy = Policies.JobActions)]
public async Task<IActionResult> StartJob(StartJobRequest dto, CancellationToken ct)
{
    var result = await _jobCommandService.StartAsync(dto, User, ct);
    return CreatedAtAction(nameof(GetJob), new { externalId = result.JobExternalId }, result);
}

// MAL — lógica de negocio en controller
[HttpPost]
public async Task<IActionResult> StartJob(StartJobRequest dto)
{
    var robot = await _dbContext.Jobs.FirstOrDefaultAsync(...); // acceso directo a DB — PROHIBIDO
    if (robot == null) return NotFound();
    // ... más lógica
}
```

---

## 4. No Direct Database Access Outside Infrastructure

Solo el proyecto `BotPulse.Infrastructure` puede acceder a `BotPulseDbContext`, Entity Framework Core y PostgreSQL directamente.

**Reglas:**
- `BotPulse.Core` no referencia `Microsoft.EntityFrameworkCore` ni ningún ORM.
- `BotPulse.Api` y `BotPulse.Worker` no pueden inyectar `BotPulseDbContext` directamente; deben usar interfaces de repositorio.
- Los repositorios especializados (`IJobRepository`, `IAuditRepository`, etc.) son la única puerta de acceso a persistencia para el código de Application.

---

## 5. No Vendor-Specific API Calls Outside Provider Projects

Toda comunicación con APIs externas de proveedores RPA (UiPath Orchestrator, Power Automate, Blue Prism, etc.) está confinada a los proyectos `BotPulse.Providers.*`.

**Reglas:**
- `BotPulse.Core` no puede referenciar SDKs de ningún vendor RPA.
- `BotPulse.Infrastructure` no puede realizar llamadas HTTP a Orchestrators externos.
- Los tipos específicos del vendor (DTOs de UiPath, modelos de Power Automate, etc.) son `internal` y no se exponen fuera de su proyecto.
- El Core solo conoce las interfaces granulares: `IRobotProvider`, `IJobProvider`, `IQueueProvider`, `ILogProvider`, `IAssetProvider`, `IMachineProvider`, `IProcessProvider`.

---

## 6. Strongly Typed Configuration

Todas las opciones de configuración se modelan como clases inmutables y se validan al arrancar.

**Reglas:**
- Usar `IOptions<T>` (o `IOptionsMonitor<T>` para recargas en runtime) para acceder a la configuración tipada.
- Prohibido `IConfiguration["clave"]` en código de negocio. Solo en el punto de composición DI.
- Toda clase de opciones debe tener un `IValidateOptions<T>` correspondiente (o `[Required]`, `[Range]`) que falle el arranque con mensaje descriptivo si falta un valor crítico.
- Las claves de secretos (contraseñas, tokens) nunca tienen valores por defecto. Si faltan, el arranque falla.

```csharp
public sealed class JwtOptions
{
    public string SigningKeyBase64 { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;

    [Range(15, 480)]
    public int ExpirationMinutes { get; init; } = 60;
}

public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKeyBase64))
            return ValidateOptionsResult.Fail("JWT_SIGNING_KEY is required and must not be empty.");
        if (Convert.FromBase64String(options.SigningKeyBase64).Length < 32)
            return ValidateOptionsResult.Fail("JWT_SIGNING_KEY must be at least 256 bits (32 bytes, 44 Base64 chars).");
        return ValidateOptionsResult.Success;
    }
}
```

---

## 7. Principios SOLID

El código de BotPulse debe adherirse a los principios SOLID:

- **S — Single Responsibility**: cada clase tiene una sola razón para cambiar.
- **O — Open/Closed**: las entidades están abiertas a extensión pero cerradas a modificación. Se extiende implementando nuevas interfaces, no modificando las existentes.
- **L — Liskov Substitution**: las implementaciones de una interfaz son intercambiables sin alterar el comportamiento del consumidor.
- **I — Interface Segregation**: interfaces pequeñas y enfocadas. Ejemplo: 7 interfaces granulares de provider en lugar de un `IRpaProvider` monolítico.
- **D — Dependency Inversion**: los módulos de alto nivel (Application) dependen de abstracciones (interfaces), nunca de implementaciones concretas.

---

## 8. Nullable Reference Types

Todos los proyectos deben tener `#nullable enable` activado.

**Reglas:**
- `<Nullable>enable</Nullable>` en todos los `.csproj` (o en `Directory.Build.props` global).
- Prohibido suprimir las advertencias de nullable con `#nullable disable` salvo en código generado automáticamente.
- Toda propiedad no-nullable de un modelo debe inicializarse con un valor o con `= default!` si la inicialización está garantizada por EF Core / DI.
- Los parámetros de métodos que puedan ser null deben marcarse como `T?`.

---

## 9. XML Documentation en APIs Públicas

Toda clase, interfaz, método, propiedad y evento `public` o `internal` con visibilidad significativa debe tener documentación XML.

**Reglas:**
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` está activado en `Directory.Build.props`.
- Los miembros no documentados generan una advertencia, y dado que `TreatWarningsAsErrors=true`, fallan el build.
- Como mínimo, un `<summary>` que describa el propósito. Para métodos complejos, agregar `<param>`, `<returns>` y `<exception>`.

```csharp
/// <summary>
/// Autentica las credenciales del usuario y devuelve el resultado de autenticación
/// con identidad enriquecida si tuvo éxito.
/// </summary>
/// <param name="request">Credenciales y parámetros adicionales según el proveedor.</param>
/// <param name="ct">Token de cancelación.</param>
/// <returns>Resultado de autenticación con claims o con razón de fallo.</returns>
/// <exception cref="AuthenticationException">Si hay un error irrecuperable en el proveedor.</exception>
Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct);
```

---

## 10. Structured Logging con Serilog

Toda salida de diagnóstico se produce mediante `ILogger<T>` con templates estructurados. Prohibido `Console.WriteLine`.

**Reglas:**
- Inyectar `ILogger<T>` en el constructor, nunca `Log.Logger` estático de Serilog directamente.
- Usar message templates con named properties, no interpolación de strings.
- No logear secretos (contraseñas, tokens, API keys). Usar `Serilog.Destructurama` para redactar campos sensibles.
- Asociar siempre el `CorrelationId` del contexto de la petición.
- Nivel de log apropiado: `Debug` para diagnóstico detallado, `Information` para eventos de negocio, `Warning` para situaciones degradadas pero recuperables, `Error` para fallos de operación, `Fatal` para errores que impiden el arranque.

```csharp
// BIEN
_logger.LogInformation("Sync completed {Service} in {ElapsedMs}ms. Processed {Count} items.",
    service.Name, elapsed.TotalMilliseconds, count);

// MAL
Console.WriteLine($"Sync completed: {service.Name}"); // PROHIBIDO

// MAL — string interpolation (pierde la estructura)
_logger.LogInformation($"Sync {service.Name} processed {count} items"); // EVITAR
```

---

## 11. Naming Conventions

Convenciones estándar de .NET:

| Elemento                      | Convención              | Ejemplo                         |
|-------------------------------|-------------------------|---------------------------------|
| Clases, Interfaces, Records   | `PascalCase`            | `JobSynchronizationService`     |
| Interfaces                    | Prefijo `I`             | `IJobProvider`                  |
| Métodos                       | `PascalCase`            | `GetRobotsAsync`                |
| Propiedades                   | `PascalCase`            | `ExternalJobId`                 |
| Variables locales             | `camelCase`             | `maxUpdatedAt`                  |
| Parámetros de método          | `camelCase`             | `cancellationToken`             |
| Private fields                | `_camelCase`            | `_jobRepository`                |
| Constantes                    | `PascalCase`            | `DefaultBatchSize`              |
| Enums y valores               | `PascalCase`            | `JobStatus.Running`             |
| Archivos                      | `PascalCase.cs`         | `RobotQueryService.cs`          |
| Proyectos                     | `PascalCase.PascalCase` | `BotPulse.Infrastructure`       |

Convenciones adicionales:
- Los métodos async deben terminar en `Async`: `GetJobsAsync`, `StartJobAsync`.
- Las interfaces de repositorio llevan el prefijo `I` y sufijo `Repository`: `IJobRepository`.
- Los options/settings llevan el sufijo `Options`: `JwtOptions`, `UiPathOptions`.

---

## 12. Unit Tests para Servicios de Dominio

Todo servicio de dominio y de Application debe tener unit tests.

**Reglas:**
- Framework: **xUnit** + **FluentAssertions** + **NSubstitute**.
- Los unit tests no tocan base de datos, red ni sistema de archivos. Toda dependencia externa se mockea con NSubstitute.
- Los tests se colocan en el proyecto `tests/BotPulse.UnitTests` con la misma estructura de carpetas que el código fuente.
- Convención de nombres: `[Método]_[Escenario]_[ResultadoEsperado]`.

```csharp
[Fact]
public async Task StartAsync_WhenProviderReturnsSuccess_ShouldPersistAuditRecord()
{
    // Arrange
    var provider = Substitute.For<IJobProvider>();
    provider.StartJobAsync(Arg.Any<StartJobRequest>(), Arg.Any<CancellationToken>())
            .Returns(new StartJobResult("ext-job-123"));
    var auditRepo = Substitute.For<IAuditRepository>();
    var sut = new JobCommandService(provider, auditRepo, Substitute.For<INotificationDelivery>());

    // Act
    await sut.StartAsync(new StartJobRequest("proc-1", null), new ClaimsPrincipal(), default);

    // Assert
    await auditRepo.Received(1).RecordAsync(
        Arg.Is<AuditRecord>(a => a.Action == "StartJob" && a.Outcome == "Success"),
        Arg.Any<CancellationToken>());
}
```

---

## 13. Property-Based Tests donde Aplique

Para invariantes universales que deben sostenerse para cualquier entrada válida, usar property-based tests.

**Framework:** FsCheck.Xunit o CsCheck. Mínimo 100 iteraciones por propiedad.

**Casos aplicables en BotPulse:**

| Propiedad                                  | Área                        |
|--------------------------------------------|-----------------------------|
| Deduplicación de alertas: no más de 1 alerta por (regla, recurso) dentro de la ventana | Alert Engine |
| Retry/backoff: suma de esperas acotada superiormente | Notification Router |
| JWT round-trip: `validate(issue(x)) == x`  | `JwtSessionTokenService`    |
| Invalidación de caché por prefijo: solo el subconjunto correcto es invalidado | `MemoryCacheService` |
| Upsert idempotente: `upsert(x) == upsert(upsert(x))` | `JobRepository`        |
| Agregación de métricas: `sum(buckets hourly) == sum(raw points)` | `MetricsAggregationService` |

---

## 14. Configuration Files No Contienen Secretos

Los archivos de configuración commiteados al repositorio nunca deben contener valores de secretos.

**Reglas:**
- `appsettings.json` y `appsettings.*.json` no contienen contraseñas, API keys, tokens ni signing keys.
- Los secretos se pasan siempre vía variables de entorno, User Secrets (desarrollo local) o un secret store (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).
- El archivo `.env.example` contiene nombres de variables sin valores. El `.env` real está en `.gitignore`.
- Si un secreto se commitea accidentalmente, debe ser rotado de inmediato (aunque haya sido eliminado del historial de git).

---

## Resumen Rápido (Checklist pre-PR)

- [ ] No hay `.Result`, `.Wait()` ni `GetAwaiter().GetResult()`.
- [ ] No hay `Console.WriteLine` ni `Debug.WriteLine`.
- [ ] Toda dependencia se inyecta por constructor.
- [ ] El controller no contiene lógica de negocio.
- [ ] Ningún acceso a `DbContext` fuera de `BotPulse.Infrastructure`.
- [ ] Ninguna llamada HTTP a vendor fuera de `BotPulse.Providers.*`.
- [ ] Toda clase pública tiene documentación XML.
- [ ] Nullable reference types: sin `null!` innecesario.
- [ ] Unit tests añadidos o actualizados para el código modificado.
- [ ] Ningún secreto en archivos de configuración.
- [ ] Build y tests pasan localmente (`dotnet build && dotnet test`).
