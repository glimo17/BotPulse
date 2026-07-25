# BotPulse — Roadmap

Este roadmap es **informativo**. Describe las capacidades previstas por fase para orientar las decisiones de arquitectura y priorización. Los plazos concretos se gestionan en el backlog del proyecto.

---

## Visión del Producto

BotPulse aspira a ser la **capa de operaciones estándar para entornos RPA multi-vendor**: un punto de control unificado, independiente del vendor, que orquesta, monitorea y analiza robots, jobs, colas y métricas de múltiples plataformas de automatización.

La arquitectura se diseñó desde el primer día para soportar múltiples proveedores RPA sin modificar el Core, la API ni la UI.

---

## Fase 1 — MVP

**Objetivo:** plataforma operacional funcional con UiPath como primer proveedor RPA y autenticación local para desarrollo.

### Proveedor RPA

- `BotPulse.Providers.UiPath` implementando las 7 interfaces granulares:
  - `IRobotProvider` — lista y detalle de robots
  - `IJobProvider` — jobs, start/stop/cancel/retry
  - `IQueueProvider` — colas y queue items
  - `ILogProvider` — logs de ejecución
  - `IAssetProvider` — metadatos de assets (sin valores secretos)
  - `IMachineProvider` — máquinas
  - `IProcessProvider` — procesos y parámetros
- OAuth2 Client Credentials para autenticación contra UiPath Orchestrator
- Mock UiPath Server para desarrollo sin credenciales productivas

### Autenticación

- `LocalAuthenticationProvider` con Argon2id para entornos de desarrollo
- Estructura preparada para Entra ID y LDAP en Fase 2
- JWT como session token post-autenticación (1h default)

### Sincronización Background

- `JobSynchronizationService` (cada 120s)
- `QueueItemSynchronizationService` (cada 180s)
- `LogSynchronizationService` (cada 60s)
- `MetricsCollectionService` (cada 300s)
- `SynchronizationOrchestrator` con fault isolation entre servicios
- Trigger manual por API (`POST /api/v1/admin/sync/{service}/trigger`)

### Alert Engine

- 5 evaluadores de reglas: `RobotOffline`, `QueueBacklog`, `JobsFailedInWindow`, `MachineOffline`, `ProcessExecutionTime`
- Deduplicación de alertas por ventana configurable (default 5 min)
- Canal `Log` (siempre activo)
- Acknowledgment de alertas

### Dashboard y Widgets

- Widgets básicos: KPI Summary, Job Queue, Robot Monitor, Alerts
- Layout por usuario persistido en base de datos
- Layout inicial por rol (Viewer, Operator, Administrator)

### API REST

- Todos los endpoints bajo `/api/v1/`
- Versionado desde el día 1
- Swagger UI en `/swagger`
- Health checks en `/health`, `/health/live`, `/health/ready`
- Notificaciones en tiempo real vía SSE o Polling (configurable)

### Infraestructura

- `MemoryCacheService` para caché en proceso
- PostgreSQL con EF Core y migraciones
- Redis provisionado en Docker Compose pero no activo
- Serilog con sinks Console y File
- Logs estructurados con correlation ID

### Frontend MVP

- React + TypeScript con Vite
- Login, Dashboard, Robots, Jobs, Queues, Alerts
- Acciones contextuales sobre jobs según rol
- Real-time updates vía SSE (TanStack Query)

### Deployment

- Docker Compose como modelo primario
- Dockerfiles multi-stage para API, Worker, Frontend y Mock UiPath
- `.env.example` con todas las variables documentadas

---

## Fase 2 — Enterprise Auth + Alert Engine Completo

**Objetivo:** autenticación empresarial real y motor de alertas completo con todos los canales.

### Autenticación

- `EntraIdAuthenticationProvider` (Microsoft Entra ID / Azure AD) con OIDC y PKCE
- `LdapAuthenticationProvider` con simple bind y mapeo de grupos
- Selección por `AUTHENTICATION_PROVIDER` sin cambios de código

### Alert Engine

- Canales adicionales: Email (SMTP via MailKit), Slack (webhook), Teams (MessageCard), Webhook genérico con firma HMAC
- Motor de escalación automática (Critical sin ack → escala tras timeout configurable, default 15 min)
- Retención configurable de alertas (default 12 meses)

### Dashboard

- Widgets adicionales: Queue Progress, Machine Health, Execution Timeline
- Drag-and-drop para reordenar widgets
- Panel de configuración de widgets por usuario

### Deployment

- Guías detalladas para Azure App Service y Azure Container Apps
- Integración con Azure Key Vault para secrets
- ANCM (ASP.NET Core Module) para IIS Windows

---

## Fase 3 — Real-Time + Redis + Power Automate

**Objetivo:** notificaciones en tiempo real escalables y segundo proveedor RPA.

### Notificaciones

- `SignalRNotificationDelivery` con backplane Redis
- WebSockets para clientes no-navegador
- Rate limiting activado (Token Bucket por usuario/IP)

### Caché

- `RedisCacheService` operativo para caché distribuida
- Redis como backplane de SignalR y rate limiting

### Nuevo Proveedor RPA

- `BotPulse.Providers.PowerAutomate` implementando las interfaces soportadas por Power Automate
- Sin cambios en Core, API ni Worker

### Performance

- Particionado de tablas de logs y métricas por fecha
- Connection pooling optimizado con PgBouncer

---

## Fase 4 — Multi-Vendor + Multi-Tenant

**Objetivo:** plataforma de nivel enterprise con soporte para múltiples vendors y aislamiento multi-tenant.

### Nuevos Proveedores RPA

- `BotPulse.Providers.BluePrism` para Blue Prism Control Room
- `BotPulse.Providers.AutomationAnywhere` para Automation Anywhere 360

### Multi-Tenant

- Aislamiento de datos por tenant en persistencia (esquema o discriminador)
- Autenticación por tenant con IdPs independientes
- Dashboard y alertas configurables por tenant

### Mobile

- Dashboard responsive mejorado
- Progressive Web App (PWA) para acceso offline básico

---

## Consideraciones de Extensibilidad

La arquitectura permite agregar cualquiera de los siguientes elementos sin modificar el Core:

| Extensión                  | Mecanismo                                     |
|----------------------------|-----------------------------------------------|
| Nuevo proveedor RPA        | Nuevo proyecto `BotPulse.Providers.<Vendor>` implementando interfaces granulares |
| Nuevo proveedor de auth    | Nueva clase `XxxAuthenticationProvider : IAuthenticationProvider` + case en DI |
| Nuevo canal de alerta      | Nueva clase `XxxAlertChannel : IAlertChannel` + registro en DI |
| Nuevo transporte RT        | Nueva clase `XxxNotificationDelivery : INotificationDelivery` + case en DI |
| Nueva implementación caché | Nueva clase `XxxCacheService : ICacheService` + case en DI |
