# ADR-001: Clean Architecture

## Status
Accepted

## Context
BotPulse necesita integrase con múltiples proveedores RPA (UiPath, Power Automate, Blue Prism, Automation Anywhere), con diferentes proveedores de autenticación (Entra ID, LDAP, Local) y desplegarse en múltiples plataformas (Docker, Azure, IIS, Linux). La lógica de negocio no debe acoplarse con ninguna de estas implementaciones concretas.

Se requiere una arquitectura que:
- Permita cambiar implementaciones de infraestructura sin afectar la lógica de dominio.
- Sea testeable en aislamiento (unit tests sin necesidad de base de datos ni servicios externos).
- Escale en complejidad sin volverse difícil de mantener a medida que se agregan proveedores.
- Tenga una dirección de dependencias predecible que cualquier desarrollador pueda seguir.

## Decision
BotPulse adopta **Clean Architecture** con cuatro capas y dependencias unidireccionales hacia el centro:

```
Presentation (API, Worker)
      │
      ▼
Application (Application Services en BotPulse.Core)
      │
      ▼
Domain (Entities, Value Objects, Interfaces en BotPulse.Core)
      │
      ▼    ← solo abstracciones hacia afuera
Infrastructure (BotPulse.Infrastructure, BotPulse.Providers.*)
```

Reglas de dependencia:
- `BotPulse.Core` (Domain + Application) no referencia ningún proyecto externo excepto BCL.
- `BotPulse.Infrastructure` y `BotPulse.Providers.*` referencian a `BotPulse.Core` pero no entre sí.
- `BotPulse.Api` y `BotPulse.Worker` referencian a todo (son el punto de composición DI) pero no contienen lógica de negocio.
- Las interfaces de abstracción (provider, repository, cache, auth) se definen en el Core y se implementan en Infrastructure/Providers.

## Alternatives Considered

**N-Tier tradicional (Data / Business / Presentation)**
Más familiar para equipos que vienen de aplicaciones .NET clásicas. El problema es que la capa Business suele terminar referenciando directamente el ORM o los SDKs de vendor, creando acoplamiento que dificulta los tests y los cambios de implementación. Descartado por la necesidad de soportar múltiples vendors sin modificar la lógica central.

**MVC simple (sin separación en capas)**
Rápido para proyectos pequeños. Para BotPulse, con múltiples providers RPA, autenticación pluggable y sync services independientes, la ausencia de capas convertiría en código espagueti inevitable. Descartado.

**Arquitectura hexagonal (Ports & Adapters)**
Conceptualmente equivalente a Clean Architecture. Descartado en favor de Clean Architecture solo por terminología y porque el equipo tenía más experiencia con esta nomenclatura.

## Consequences

**Positivas:**
- La lógica de negocio en `BotPulse.Core` es testeable con mocks ligeros sin infraestructura real.
- Cambiar una implementación concreta (por ejemplo, de `MemoryCacheService` a `RedisCacheService`) no requiere modificar ningún servicio de aplicación.
- Agregar un nuevo proveedor RPA es crear un proyecto nuevo sin tocar el Core.
- La dirección de dependencias es predecible: los nuevos desarrolladores saben dónde poner cada tipo de código.

**Negativas:**
- Mayor estructura inicial comparado con enfoques más simples. Se necesitan más archivos y proyectos desde el principio.
- Las abstracciones (interfaces, records de snapshot) requieren mayor esfuerzo de diseño por adelantado.
- El team debe mantener la disciplina de no violar las reglas de dependencia. Se mitiga con `TreatWarningsAsErrors` y la estructura de proyectos .NET que hace imposible referenciar en sentido incorrecto.
