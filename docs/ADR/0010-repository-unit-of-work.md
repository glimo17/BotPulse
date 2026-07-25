# ADR-010: Repository Pattern + Unit of Work

## Status
Accepted

## Context
BotPulse usa EF Core como ORM sobre PostgreSQL. Los servicios de aplicación en `BotPulse.Core` necesitan acceder a datos persistidos. Sin una capa de abstracción, los Application Services dependerían directamente de `BotPulseDbContext` y de `IQueryable<T>`, lo que:

- Viola Clean Architecture (el Core dependería de EF Core).
- Dificulta los unit tests (habría que mockear `DbContext` o usar InMemory database).
- Expone a los Application Services el acceso completo a la base de datos en lugar de solo las operaciones que conceptualmente deben hacer.

## Decision
BotPulse usa **Repository Pattern** con **Unit of Work** como capa de abstracción sobre EF Core.

Estructura:
- `IRepository<T>` en `BotPulse.Core/Abstractions/Persistence/`: operaciones genéricas (GetById, Find, Add, Update, Remove).
- Repositorios especializados por entidad (`IJobRepository`, `IAlertRepository`, etc.) con métodos de dominio específicos (ej. `UpsertAsync`, `GetMaxUpdatedAtAsync`, `QueryAsync(JobFilter)`).
- `IUnitOfWork` en Core: `SaveChangesAsync` y `BeginTransactionAsync`. Los Application Services llaman a `SaveChangesAsync` para confirmar un conjunto de operaciones como unidad atómica.
- `IAuditRepository` especial: **append-only**. Solo expone `RecordAsync` y consultas de lectura. No hay Update ni Delete.
- Las implementaciones concretas (`Repository<T>`, `JobRepository`, `UnitOfWork`) viven en `BotPulse.Infrastructure`.

```csharp
// Core solo conoce:
public interface IJobRepository : IRepository<Job>
{
    Task<Job?> GetByExternalIdAsync(string provider, string externalId, CancellationToken ct);
    Task<DateTime?> GetMaxUpdatedAtAsync(CancellationToken ct);
    Task<PagedResult<Job>> QueryAsync(JobFilter filter, CancellationToken ct);
    Task UpsertAsync(JobSnapshot snapshot, CancellationToken ct);
}

// Infrastructure implementa:
internal sealed class JobRepository : Repository<Job>, IJobRepository { /* ... */ }
```

## Alternatives Considered

**EF Core directo en servicios de Application**
Inyectar `BotPulseDbContext` directamente en los Application Services. Más simple en términos de código. Pero viola Clean Architecture: el Core necesitaría referenciar `Microsoft.EntityFrameworkCore`. Los unit tests necesitarían InMemory database o mocks complejos de DbContext. Descartado.

**CQRS puro con MediatR**
Separar completamente Commands y Queries con MediatR como message bus. Aporta beneficios de separación en sistemas de alta complejidad. Para BotPulse, la complejidad adicional de handlers, requests y el dispatcher no está justificada en el scope actual. Puede considerarse si el equipo crece y la base de código se expande significativamente. Descartado para MVP.

**Repositories sin Unit of Work**
Solo interfaces de repositorio, cada una con su propio SaveChanges. El problema es que cuando un Application Service necesita persistir en múltiples repositorios de forma atómica (ej. Job + AuditRecord), no hay forma de garantizar transaccionalidad sin UoW. Descartado.

## Consequences

**Positivas:**
- Los Application Services en `BotPulse.Core` no referencian EF Core ni ningún ORM. Son testeables con mocks de `IJobRepository`, `IAlertRepository`, etc.
- `IAuditRepository` append-only previene accidentalmente que el código de aplicación borre o modifique registros de auditoría.
- Los repositorios especializados exponen solo las operaciones que tienen sentido desde el dominio (ej. `GetMaxUpdatedAtAsync` para el sync service), no el acceso completo a la base de datos.
- Cambiar de EF Core a Dapper o a otro ORM solo requiere reimplementar los repositorios, no los Application Services.

**Negativas:**
- Una capa extra de abstracción que algunos consideran "over-engineering" para proyectos medianos con EF Core. El argumento "EF Core ya es un repositorio" es válido, pero no resuelve la dependencia del Core en EF Core.
- Los repositorios especializados requieren tiempo de diseño para determinar qué métodos exponer. Se mitiga siguiendo el principio de "agregar métodos cuando son necesarios" (YAGNI) en lugar de implementar toda la interfaz posible por adelantado.
