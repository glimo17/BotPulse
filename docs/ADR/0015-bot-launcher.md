# ADR-015: Bot Launcher — Ejecución de Bots Unattended desde BotPulse

## Status
Accepted

## Context
Los operadores RPA necesitan ejecutar procesos unattended de forma ad-hoc (lanzamientos de emergencia, re-ejecuciones manuales, pruebas en producción) sin acceder directamente al Orchestrator externo. Hoy deben:
1. Abrir el Orchestrator de UiPath (u otro vendor)
2. Navegar al proceso correcto
3. Configurar parámetros manualmente
4. Lanzar el job

BotPulse ya tiene `IJobProvider.StartJobAsync()` y `IProcessProvider.GetProcessParametersAsync()`. Los bloques fundamentales están en el Core. Falta la capa de UI.

## Decision
Se añade una vista `/launcher` en el frontend que expone un formulario para:
1. Seleccionar proceso (de `IProcessProvider`)
2. Seleccionar robot o dejar en "Automático" (de `IRobotProvider`)
3. Completar parámetros de entrada (de `IProcessProvider.GetProcessParametersAsync`)
4. Lanzar con un botón (llama a `POST /api/v1/jobs`)
5. Ver estado del job recién lanzado en un panel integrado

No se añaden endpoints nuevos al backend. Toda la funcionalidad ya existe en la API v1.

## Alternatives Considered

**Botón de lanzamiento desde la lista de Procesos (/processes)**
Más conveniente para usuarios que ya están explorando procesos. Sin embargo, mezcla la vista de consulta con la vista de acción, violando Single Responsibility en la UI. El Launcher dedicado permite mayor espacio para el formulario de parámetros y el panel de seguimiento.

**Job Scheduler integrado al Launcher**
Combinar lanzamiento ad-hoc con scheduling en la misma vista. Descartado para esta fase: el scheduling es un dominio más complejo (cron expressions, gestión de conflictos) que merece su propio spec (ver ADR-016 propuesto).

## Consequences

**Positivas:**
- Reduce el tiempo de lanzamiento ad-hoc de ~5 minutos (navegar al Orchestrator) a ~30 segundos.
- El operador no necesita permisos directos sobre el Orchestrator: solo necesita permisos en BotPulse.
- El panel de seguimiento post-lanzamiento reduce los cambios de contexto.

**Negativas:**
- Solo soporta procesos unattended. Los procesos attended corren en la máquina del usuario final y no pueden ser orquestados de esta forma.
- El Launcher no reemplaza el Orchestrator para gestión avanzada (schedules, priorities complejas, input queues). Es solo para lanzamientos rápidos.
