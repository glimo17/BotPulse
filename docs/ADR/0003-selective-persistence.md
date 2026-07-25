# ADR-003: Selective Persistence

## Status
Accepted

## Context
BotPulse interactúa con datos de múltiples tipos: robots, machines, processes, assets, jobs, queue items, logs, métricas y audit. No todos estos datos necesitan persistirse localmente con la misma justificación.

Hay una distinción fundamental:

- **Datos con valor histórico**: Jobs (historial de ejecuciones), Queue Items (procesamiento y retries), Execution Logs (diagnóstico), Métricas (análisis de tendencias), Audit Records (trazabilidad de seguridad). Estos datos cambian con el tiempo y la app necesita consultarlos con filtros, rangos de fechas y agregaciones que el Orchestrator externo no necesariamente expone.

- **Datos de configuración/topología**: Robots, Machines, Processes, Assets, Queues. Son la "foto actual" de la infraestructura RPA. Cambian poco y el Orchestrator siempre tiene la versión más fresca. Persistirlos localmente introduce sincronización compleja por beneficio mínimo.

## Decision
BotPulse persiste **solo los datos con valor histórico o analítico**:

- Jobs
- Queue Items
- Execution Logs
- Metric Points + Metric Rollups (hourly, daily)
- Audit Records
- Users (sincronizados desde el IdP al hacer login)
- Alerts y Alert Rules

Los datos de topología (**Robots, Machines, Processes, Assets, Queues**) se leen **on-demand** desde el proveedor RPA activo, con caché en memoria opcional y TTL configurable por tipo.

**No existen tablas `robots`, `machines`, `processes`, `assets`, `queues` en la base de datos de BotPulse.**

## Alternatives Considered

**Persistir todo (incluyendo topología)**
Tendría tablas para robots, machines, processes, assets y queues, sincronizadas continuamente. Beneficios: consultas más rápidas si el Orchestrator está caído. Problemas: el Worker de sincronización se vuelve mucho más complejo, hay riesgo de inconsistencia si la sincronización falla parcialmente, y el valor añadido frente al read-on-demand con caché es mínimo para los patrones de acceso de BotPulse. Descartado.

**No persistir nada (todo on-demand)**
La app consultaría el Orchestrator para todo en tiempo real. Extremadamente simple pero inapropiado: sin histórico de jobs no hay análisis de tendencias, sin logs persistidos no hay búsqueda histórica, sin métricas no hay KPIs. Descartado para los datos con valor histórico.

**Persistir solo un subconjunto diferente**
Por ejemplo, persistir robots pero no logs. Evaluado pero rechazado: los robots son pocos (decenas a cientos) y cambian poco, el overhead de sincronizarlos es bajo, pero el beneficio de tenerlos locales es mínimo dado que el acceso on-demand con caché de 2 minutos es suficiente para los patrones de uso del dashboard. Los logs, en cambio, pueden ser millones y requieren búsqueda histórica.

## Consequences

**Positivas:**
- El esquema de base de datos es más simple: 11 tablas en lugar de las 16+ que requeriría persistir todo.
- El Worker de sincronización es menos complejo: solo sincroniza los datos que cambian con el tiempo de formas que importan (nuevos jobs, nuevos logs, etc.).
- Los datos de topología siempre están frescos (no hay lag de sincronización para robots y machines).
- Menor riesgo de inconsistencias entre BotPulse y el Orchestrator para datos de topología.

**Negativas:**
- Si el Orchestrator externo no está disponible, los datos de robots/machines/processes no son accesibles (aunque los datos históricos sí lo están).
- Las queries que combinan datos on-demand (robots) con datos históricos (jobs) requieren lógica de join en Application Service, no en SQL.
- La caché en memoria no persiste entre reinicios; tras un restart se producen más llamadas al Orchestrator hasta que la caché se recaliente.
