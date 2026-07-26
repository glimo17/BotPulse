# ADR-014: Demo Provider para Desarrollo y Presentaciones sin Orchestrator

## Status
Accepted

## Context
BotPulse requiere credenciales de UiPath Orchestrator (OAuth2 ClientId/ClientSecret) para funcionar. Esto crea fricción en tres escenarios:

1. **Desarrollo local**: un developer que clona el repositorio no puede ver la UI funcionando sin credenciales productivas.
2. **Demos y presentaciones**: mostrar el producto a stakeholders requiere un entorno UiPath real, lo que expone datos productivos o requiere un tenant dedicado.
3. **CI/CD y tests de UI**: los tests de integración del frontend no deben depender de un Orchestrator externo para verificar que los componentes renderizan correctamente.

La arquitectura vendor-agnostic con interfaces granulares (ADR-002, ADR-013) hace trivial añadir un nuevo provider que implemente las mismas 7 interfaces sin tocar el Core.

## Decision
Se introduce `BotPulse.Providers.Demo`: un proyecto .NET que implementa las 7 interfaces granulares (`IRobotProvider`, `IJobProvider`, `IQueueProvider`, `ILogProvider`, `IAssetProvider`, `IMachineProvider`, `IProcessProvider`) con datos completamente en memoria.

El provider se activa con `RpaProvider=Demo` (o como default cuando la clave no está configurada). No requiere ninguna credencial externa.

La clase `DemoDataSeed` es un singleton que:
- Genera datos realistas al construirse (6 robots, 4 procesos de negocio, 80 jobs en 48h, 3 colas, 5 assets, logs por job)
- Simula cambios de estado cada 30 segundos mediante un Timer (robots rotando Idle↔Busy, jobs completándose, backlog fluctuando)

## Alternatives Considered

**WireMock / Mock Server HTTP**
Interceptar llamadas HTTP a UiPath con un servidor de mocks. Más realista en cuanto a transporte, pero requiere configurar un proceso externo y mantener fixtures de respuesta OData. Descartado por complejidad de setup.

**Datos estáticos JSON**
Leer fixtures de archivos JSON en disco. Simple pero sin simulación de tiempo real (el dashboard no se vería "vivo"). Descartado por experiencia de demo inferior.

**Tenant de UiPath Community**
Usar un tenant gratuito de UiPath Community Edition para demos. Requiere conexión a internet, credenciales reales en el repo de demos, y dependencia de disponibilidad del servicio externo. Descartado por fragilidad.

## Consequences

**Positivas:**
- Un developer puede clonar el repo, hacer `dotnet run` y ver el dashboard completamente funcional en 30 segundos, sin configurar nada.
- Las demos con stakeholders son reproducibles y no exponen datos productivos.
- Los tests de UI pueden usar el DemoProvider como fuente de datos determinista.
- El DemoProvider prueba implícitamente que las 7 interfaces granulares están correctamente definidas (si el Demo compila e implementa las interfaces, el contrato es correcto).

**Negativas:**
- Un developer podría olvidar cambiar a `RpaProvider=UiPath` en producción. Mitigado con: (1) log Warning al arrancar con Demo activo, (2) el health check reporta "Demo provider active" en `/health`, haciendo obvio el modo.
- Los datos de demo son ficticios. No sirven para testing de rendimiento ni para validar comportamiento con datos reales de UiPath.
- Hay que mantener el DemoDataSeed actualizado cuando se añaden nuevas interfaces o campos a los snapshots.
