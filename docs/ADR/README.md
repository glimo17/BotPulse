# Architecture Decision Records (ADR)

Este directorio contiene los Architecture Decision Records del proyecto BotPulse. Cada ADR documenta una decisión arquitectónica significativa: el contexto que la motivó, la decisión tomada, las alternativas consideradas y las consecuencias derivadas.

Los ADR son documentos vivos: se crean cuando se toma una decisión, se marcan como `Superseded` cuando son reemplazados y como `Deprecated` cuando dejan de ser relevantes. Nunca se eliminan.

---

## Índice

| ID       | Título                                                              | Status    |
|----------|---------------------------------------------------------------------|-----------|
| ADR-001  | [Clean Architecture](./0001-clean-architecture.md)                  | Accepted  |
| ADR-002  | [Provider Pattern Granular](./0002-provider-pattern-granular.md)    | Accepted  |
| ADR-003  | [Selective Persistence](./0003-selective-persistence.md)            | Accepted  |
| ADR-004  | [Docker as Primary Packaging](./0004-docker-primary-packaging.md)   | Accepted  |
| ADR-005  | [OAuth2 Client Credentials para UiPath](./0005-oauth2-client-credentials-uipath.md) | Accepted |
| ADR-006  | [JWT Solo como Session Token](./0006-jwt-session-token-only.md)     | Accepted  |
| ADR-007  | [PostgreSQL como Base de Datos Principal](./0007-postgresql-primary-database.md) | Accepted |
| ADR-008  | [Servicios de Sincronización Background Independientes](./0008-independent-background-sync-services.md) | Accepted |
| ADR-009  | [Polling/SSE para MVP Real-Time](./0009-polling-sse-mvp-realtime.md) | Accepted |
| ADR-010  | [Repository + Unit of Work](./0010-repository-unit-of-work.md)      | Accepted  |
| ADR-011  | [Autenticación Pluggable](./0011-pluggable-authentication.md)        | Accepted  |
| ADR-012  | [API Versioning desde el Día 1](./0012-api-versioning-day-one.md)   | Accepted  |
| ADR-013  | [Plataforma RPA Vendor-Agnostic](./0013-vendor-agnostic-rpa-platform.md) | Accepted |

---

## Plantilla Estándar

Al crear un nuevo ADR, usar la siguiente plantilla y asignar el siguiente número secuencial disponible.

```markdown
# ADR-XXX: Título

## Status
Accepted | Superseded by ADR-YYY | Deprecated

## Context
Situación que motiva la decisión. Qué problema se está resolviendo, qué fuerzas
o restricciones están en juego, qué opciones existen en el horizonte.

## Decision
La decisión tomada, descrita de forma concisa y afirmativa.

## Alternatives Considered
Otras opciones evaluadas y la razón por la que se descartaron.

## Consequences
Consecuencias positivas y negativas de la decisión. Trade-offs aceptados.
```

---

## Cómo crear un nuevo ADR

1. Copiar la plantilla anterior.
2. Nombrar el archivo `NNNN-titulo-en-kebab-case.md` con el siguiente número secuencial.
3. Completar todas las secciones.
4. Agregar una entrada en el índice de este README.
5. Si el ADR reemplaza a uno existente, marcar el anterior como `Superseded by ADR-NNN`.
