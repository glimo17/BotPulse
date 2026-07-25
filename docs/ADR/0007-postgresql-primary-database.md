# ADR-007: PostgreSQL como Base de Datos Principal

## Status
Accepted

## Context
BotPulse necesita persistir datos con patrones de acceso variados:
- Datos de series temporales (métricas, logs) con queries por rango de fechas y agregaciones.
- Datos JSON semiestructurados (dimensiones de métricas, parámetros de alertas, configuración de widgets).
- Datos relacionales con foreign keys (users → alerts → alert_rules).
- Audit records con alta tasa de inserción y baja tasa de lectura.

Se necesita una base de datos que:
- Sea open source, sin costos de licencia.
- Soporte JSONB para campos semiestructurados sin sacrificar indexabilidad.
- Tenga soporte de primera clase en los principales clouds (Azure, AWS, GCP).
- Sea compatible con EF Core y el ecosistema .NET.

## Decision
**PostgreSQL 15+** es la base de datos primaria de BotPulse.

Uso específico:
- **JSONB** para columnas como `properties_json` (execution logs), `dimensions_json` (métricas), `parameters_json` (alert rules), `channels_json` (alert rules), `widgets_json` (dashboard layouts). Permite indexar campos dentro del JSON si es necesario.
- **TIMESTAMPTZ** (timestamp with time zone) para todos los campos de tiempo, asegurando que los valores UTC se almacenen correctamente.
- **BIGSERIAL** para tablas con alta tasa de inserción (jobs, logs, métricas).
- **UUID** para entidades donde se prefiere un ID distribuido (users, alerts, alert_rules).
- **Unique constraints** compuestos para soportar upserts idempotentes (ej. `UNIQUE (provider_name, external_job_id)`).
- **Índices parciales** donde sea útil (ej. sobre `status` para filtrar solo registros activos).

## Alternatives Considered

**SQL Server (Microsoft SQL Server)**
Bien integrado con el ecosistema .NET y con soporte en Azure (Azure SQL). Sin embargo, tiene costos de licencia para on-premises y la edición Express tiene límites de tamaño. Para BotPulse, que aspira a ser open source y multi-cloud, la dependencia de una licencia propietaria es un obstáculo. Descartado.

**MongoDB**
Nativo para documentos JSON y flexible para datos semiestructurados. Sin embargo, el modelo de datos de BotPulse es predominantemente relacional con algunos campos JSON, no un modelo de documentos puro. MongoDB añadiría complejidad de transacciones cross-document y join operations que PostgreSQL maneja nativamente. Descartado.

**SQLite**
Excelente para desarrollo local y testing. Sin embargo, no es apropiado para producción con múltiples réplicas, escrituras concurrentes o volúmenes de datos importantes. Descartado para producción.

**CockroachDB**
PostgreSQL-compatible con escalabilidad horizontal nativa. Para el scope actual de BotPulse (monitoreo de un Orchestrator RPA), la escala horizontal de base de datos no es un requisito inmediato. CockroachDB añade complejidad operacional. Puede reconsiderarse en Fase 4 si hay necesidades multi-tenant globales.

## Consequences

**Positivas:**
- Open source: sin costos de licencia. Disponible en todos los clouds como servicio gestionado (Azure Database for PostgreSQL, Amazon RDS for PostgreSQL, Cloud SQL).
- JSONB permite almacenar datos semiestructurados sin sacrificar la capacidad de indexarlos y consultarlos.
- Excelente soporte en EF Core con el proveedor `Npgsql.EntityFrameworkCore.PostgreSQL`.
- `TIMESTAMPTZ` garantiza corrección temporal en entornos multi-zona.
- Particionado por rango de fechas disponible para tablas de alta volumen (logs, métricas) cuando sea necesario.

**Negativas:**
- No es la primera opción para equipos con background puramente Microsoft que están más familiarizados con SQL Server. La curva de aprendizaje es pequeña pero existe.
- El despliegue on-premises con alta disponibilidad (streaming replication, Patroni) requiere más conocimiento operacional que SQL Server con Always On (que tiene asistentes más amigables). Se mitiga con soluciones gestionadas en cloud.
- La sintaxis de algunos tipos específicos (como arrays nativos de PostgreSQL) no es portátil a otros RDBMS. BotPulse acepta este trade-off conscientemente.
