# ADR-002: Provider Pattern Granular

## Status
Accepted

## Context
BotPulse necesita integrarse con múltiples plataformas RPA. Cada plataforma expone capacidades diferentes: UiPath tiene Robots, Jobs, Queues, Assets, Machines y Processes. Power Automate tiene Flows y Runs pero no tiene el concepto de Machine. Blue Prism tiene Work Queues pero su modelo de Job es diferente.

Si se define una única interfaz `IRpaProvider` con todos los métodos posibles, cada implementación estaría obligada a implementar métodos que su plataforma no soporta, violando Interface Segregation Principle (ISP). Además, el consumidor tendría que conocer qué métodos son válidos para cada provider.

## Decision
BotPulse define **7 interfaces granulares**, una por capacidad:

```csharp
IRobotProvider      // Robots: GetRobotsAsync, GetRobotByIdAsync
IJobProvider        // Jobs: GetJobsAsync, StartJobAsync, StopJobAsync, CancelJobAsync
IQueueProvider      // Queues: GetQueuesAsync, GetQueueItemsAsync
ILogProvider        // Execution logs: GetExecutionLogsAsync
IAssetProvider      // Asset metadata: GetAssetsAsync (sin valores secretos)
IMachineProvider    // Machines: GetMachinesAsync, GetMachineByIdAsync
IProcessProvider    // Processes: GetProcessesAsync, GetProcessParametersAsync
```

Cada implementación de proveedor implementa solo las interfaces que su plataforma soporta. Los servicios de aplicación declaran como dependencia solo la(s) interfaz(ces) que necesitan. Un proveedor que no soporta `IAssetProvider` simplemente no lo registra.

## Alternatives Considered

**IRpaProvider monolítico**
Una única interfaz con todos los métodos. Simple de entender inicialmente pero viola ISP: los providers que no soportan ciertos métodos deben lanzar `NotImplementedException`. Los consumidores no pueden confiar en que un método exista. Descartado.

**SDK por vendor (cada provider expone su propio modelo)**
Que el código de aplicación conozca el tipo concreto del provider (`UiPathProvider`, `PowerAutomateProvider`) y castee según necesite. Viola completamente la inversión de dependencias y hace imposible cambiar el vendor activo por configuración. Descartado.

**Un único aggregate interface con métodos opcionales (por default)**
Interfaces con implementaciones por defecto que lanzan `NotImplementedException`. Similar al monolítico pero con menos boilerplate. Sigue sin poder distinguir en tiempo de compilación si un provider soporta una capacidad. Descartado.

## Consequences

**Positivas:**
- Cada interfaz es pequeña, enfocada y fácil de entender.
- Los tests de unidad solo necesitan mockear las interfaces que el servicio bajo test usa.
- Un provider puede implementar parcialmente la plataforma (ej. solo `IRobotProvider` y `IJobProvider`) sin que el código que no necesita lo demás se vea afectado.
- Agregar una nueva capacidad (ej. `IScheduleProvider`) no requiere modificar interfaces existentes.
- Cumple ISP y DIP del conjunto SOLID.

**Negativas:**
- Más interfaces en el código base. Un desarrollador nuevo debe entender que hay 7 interfaces de provider en lugar de 1.
- El registro DI es más verboso (7 `services.AddScoped<IXxxProvider>` en lugar de 1).
- Se mitiga con el método de extensión `AddUiPathProvider(configuration)` que encapsula el registro.
