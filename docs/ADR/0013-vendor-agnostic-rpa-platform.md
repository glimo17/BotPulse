# ADR-013: BotPulse como Plataforma RPA Vendor-Agnostic

## Status
Accepted

## Context
BotPulse nació con UiPath como el primer proveedor RPA a soportar. La decisión fundamental era: ¿construir un dashboard específico para UiPath que se extienda a otros vendors en el futuro, o construir una plataforma genérica desde el principio donde UiPath sea el primer implementador?

Construir específico-para-UiPath primero es más rápido en el corto plazo. Sin embargo, cuando llegue el momento de agregar Power Automate o Blue Prism, los conceptos de UiPath estarían baked-in en el modelo de dominio, en la API, en la base de datos y en la UI. La migración sería costosa.

Las organizaciones que usan BotPulse necesitan ver múltiples plataformas RPA en el mismo dashboard. En muchas empresas coexisten UiPath y Power Automate, o UiPath y Blue Prism.

## Decision
BotPulse es diseñado desde el primer día como una **plataforma de operaciones RPA vendor-agnostic**. UiPath es el primer proveedor soportado, pero la palabra "UiPath" no aparece en `BotPulse.Core`.

Consecuencias de esta decisión en el diseño:

**En el dominio (Core):**
- Los enums usan valores neutrales: `JobStatus.Running`, no `UiPathJobState.Running`.
- Los records de snapshot usan campos neutrales: `RobotSnapshot.Status` es `"Online"|"Offline"|"Idle"|"Busy"`, no los valores específicos de UiPath.
- El campo `ProviderName` en entidades como `Job` y `ExecutionLog` identifica el origen (`"UiPath"`, `"PowerAutomate"`) sin que el modelo de dominio dependa de ninguno de ellos.

**En las interfaces:**
- 7 interfaces granulares de provider (`IRobotProvider`, `IJobProvider`, etc.) definen el contrato en términos neutrales.
- `IProviderVersionNegotiator` abstrae la negociación de versiones.

**En los proyectos:**
- `BotPulse.Providers.UiPath` contiene todo lo específico de UiPath: DTOs de la API OData, mapeo a snapshots neutrales, OAuth2 client credentials.
- En el futuro: `BotPulse.Providers.PowerAutomate`, `BotPulse.Providers.BluePrism`, etc. son proyectos separados que implementan las mismas interfaces.

**En la activación:**
- La variable de entorno `RPA_PROVIDER=UiPath` (o equivalente) activa el proveedor. No hay cambios de código.

## Alternatives Considered

**Construir primero para UiPath, extender después**
Más rápido en el corto plazo: los DTOs de UiPath se usan directamente en el dominio, los endpoints de la API usan terminología de UiPath. Cuando llegue Power Automate, habría que refactorizar el modelo de dominio, la API, los repositorios y la UI para acomodar el segundo proveedor. Este refactor en un sistema en producción tiene un costo y riesgo mucho mayor que diseñar el modelo correcto desde el principio. Descartado.

**Abstraer solo la capa de HTTP (Facade sobre la API de UiPath)**
Crear un facade que traduzca entre la API de UiPath y algo "más limpio", pero sin un modelo de dominio verdaderamente neutral. El problema es que el facade tiende a fuga: los conceptos específicos de UiPath se cuelan en los servicios de aplicación. Descartado.

**Multi-provider desde el inicio con providers paralelos**
Soportar UiPath Y Power Automate desde el día 1. Más amicioso pero aumenta enormemente el scope del MVP. Descartado para MVP; el diseño vendor-agnostic garantiza que agregar el segundo provider en Fase 2/3 sea un esfuerzo incremental, no disruptivo.

## Consequences

**Positivas:**
- Agregar un nuevo proveedor RPA es crear un nuevo proyecto `BotPulse.Providers.<Vendor>`, implementar las interfaces, registrar en DI. Cero cambios en Core, API, Worker o UI.
- El modelo de dominio es estable: `JobStatus`, `RobotSnapshot`, `QueueItemStatus` no cambian al agregar un nuevo vendor.
- Las métricas, alertas y el dashboard son vendor-agnostic: un KPI de "success rate" agrega jobs de UiPath y Power Automate juntos.
- Los unit tests del Core son completamente independientes de cualquier vendor.

**Negativas:**
- Mayor esfuerzo inicial de abstracción. Definir snapshots neutrales, mapeos, y la capa de interfaces requiere más diseño por adelantado que simplemente usar los DTOs de UiPath directamente.
- Los mapeos entre DTOs de vendor y snapshots neutrales añaden código de traducción. Si UiPath tiene 30 campos en su DTO de Job y el snapshot neutral solo tiene 12, se pierde información específica del vendor. Se acepta este trade-off conscientemente; siempre se puede agregar campos a los snapshots si hay demanda.
- La abstracción puede no capturar perfectamente todas las capacidades de todos los vendors futuros. Se mitiga con el principio de evolucionar las interfaces según los needs reales de los providers (no diseñar para vendors hipotéticos).
