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

- `BotPulse.Providers.UiPath` implementando las 7 interfaces granulares
- `BotPulse.Providers.Demo` — proveedor en memoria para desarrollo y demos sin Orchestrator (ADR-014)
- Selección de proveedor por configuración: `RPA_PROVIDER=Demo|UiPath`
- OAuth2 Client Credentials para UiPath

### KPI Dashboard Operacional (Spec: operational-kpi-dashboard)

- 9 KPIs calculados client-side: Success Rate, Jobs Volume, Avg Cycle Time, Robot Utilization, Fleet Availability, Queue Backlog, Exception Breakdown, MTTA, Critical Alerts
- 8 KPI cards en Dashboard (2 filas × 4) con color-coding y navegación clickable
- Exception Breakdown donut chart en Metrics page
- Datos de 4 queries paralelas: `/robots`, `/jobs`, `/queues`, `/alerts`

### Bot Launcher (Spec: bot-launcher)

- Vista `/launcher` para ejecutar procesos unattended con un botón
- Selección de proceso, robot (o Automático) y parámetros de entrada
- Panel de seguimiento con los últimos 5 jobs lanzados en la sesión
- Auto-refresh de status de jobs Running cada 10 segundos

### Motor Multitema (Spec: theming-engine)

- 4 temas: Dark (default), Light, Ocean (celeste), Pink (magenta)
- CSS Custom Properties + Tailwind theme extension
- Selector de tema en el Header con persistencia en localStorage
- Transiciones suaves (transition-colors duration-300)
- Independiente del backend — 100% frontend

### Autenticación

- `LocalAuthenticationProvider` con Argon2id
- JWT como session token post-autenticación (1h default)

### Sincronización Background

- `JobSynchronizationService`, `QueueItemSynchronizationService`, `LogSynchronizationService`, `MetricsCollectionService`
- `SynchronizationOrchestrator` con fault isolation

### Alert Engine

- 5 evaluadores: RobotOffline, QueueBacklog, JobsFailedInWindow, MachineOffline, ProcessExecutionTime
- Deduplicación y canal Log

### API REST

- Todos los endpoints bajo `/api/v1/`
- Swagger en `/swagger`, Health checks en `/health`
- SSE para notificaciones en tiempo real

### Deployment

- Docker Compose con 6 servicios: postgres, redis, api, worker, ui, reverse-proxy
- Dockerfiles multi-stage para API, Worker y UI

---

## Fase 2 — Enterprise Auth + Alert Engine Completo

**Objetivo:** autenticación empresarial real y motor de alertas completo con todos los canales.

### Autenticación

- `EntraIdAuthenticationProvider` (Microsoft Entra ID / Azure AD) con OIDC y PKCE
- `LdapAuthenticationProvider` con simple bind y mapeo de grupos
- Selección por `AUTHENTICATION_PROVIDER` sin cambios de código

### User Management & RBAC (Spec: user-management-rbac)

- 23 permisos granulares (Dashboard.View, Jobs.Execute, Roles.Update, etc.)
- 3 roles del sistema: Administrator, Operations Manager, Viewer
- Roles personalizados ilimitados (Finance Supervisor, Night Shift Operator, etc.)
- Gestión de usuarios: crear, editar, habilitar/deshabilitar, asignar roles
- Vista de administración de roles y permisos en la UI
- Caché de permisos por sesión (invalidación en < 5s)
- Preparación para folder-level security (scope en UserRoles)
- Independencia total del proveedor de autenticación

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

---

## Fase 5 — Cognitive & RAG Engine (ADR-016)

**Objetivo:** transformar BotPulse de una plataforma de monitoreo en una plataforma de operaciones inteligentes con diagnóstico asistido por IA, auto-reparación y búsqueda en lenguaje natural.

### Sub-fase 5A: Fundación RAG

- `BotPulse.Cognitive` — nuevo proyecto con `IAIService`, `IVectorSearchRepository`, `IEmbeddingProvider`
- PostgreSQL + pgvector para almacenamiento vectorial (extensión en la misma instancia)
- Pipeline de vectorización automática: errores de ejecución, resoluciones validadas, patrones de fallo
- Provider Pattern para LLM: soporte OpenAI, Anthropic, Ollama (modelos locales)
- Control de costos: truncamiento de logs, caché de embeddings, rate limiting de tokens

### Sub-fase 5B: Diagnóstico Asistido

- Panel "Análisis de IA" por job fallido con: causa técnica, impacto, pasos de resolución
- RAG pipeline: recuperación semántica (cosine similarity) → inyección de contexto → generación de diagnóstico
- Feedback loop: operadores validan/rechazan diagnósticos → vectorización de resoluciones validadas
- Memoria de aprendizaje continuo (el sistema mejora con cada resolución)

### Sub-fase 5C: Búsqueda NL-to-Query

- Barra de comandos conversacional en el dashboard
- Traducción de consultas naturales a filtros de API: "fallos del robot financiero ayer" → query estructurado
- Historial de búsquedas y sugerencias contextuales

### Sub-fase 5D: Self-Healing Bots (Nivel Máximo)

- Agente autónomo que detecta discrepancias de selectores (XPath/CSS) durante un fallo
- Generación de parches sugeridos con vista previa visual
- Flujo de aprobación humana obligatorio antes de aplicar (never fully autonomous)
- Integración con repositorio de procesos para propagar el fix

### Sub-fase 5E: Predicción de Anomalías

- Análisis estadístico de series temporales (Z-score, rolling averages) sobre métricas de ciclo
- Alertas proactivas antes de degradación: "el proceso X está tardando 3x más de lo normal"
- Integración con Alert Engine existente (nuevo evaluador `AnomalyDetectionRule`)

### Requisitos No Funcionales del Módulo Cognitivo

- **Desacoplamiento de LLM Providers**: `IAIService` con implementaciones para OpenAI, Anthropic, Ollama. Selección por configuración.
- **Control de Costos**: Truncamiento previo de logs (max 4K tokens por context window). Caché de embeddings generados. Rate limiting configurable.
- **Multi-Tenant Vector Space**: Particionado lógico por `organization_id` en las tablas de embeddings. El conocimiento de un cliente nunca se expone a otro.
- **Latencia**: Diagnósticos generados de forma asíncrona (no bloquea la vista del job). Target: < 10s para el primer resultado.

### Dependencias

- Requiere Fase 4 completada (Multi-Tenant) para el aislamiento vectorial por organización
- Requiere PostgreSQL 15+ con extensión `pgvector` habilitada
- No requiere cambios en el Core existente — nuevo proyecto `BotPulse.Cognitive` referencia solo `BotPulse.Core`

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
| Nuevo proveedor de IA/LLM  | Nueva clase `XxxAIService : IAIService` + case en DI                          |
| Nuevo vector store          | Nueva clase `XxxVectorSearchRepository : IVectorSearchRepository` + case en DI |
| Nuevo tema visual          | Nuevo bloque `[data-theme="xxx"]` en variables.css + entrada en themes.ts    |
| Nuevo permiso              | Nueva constante en `PermissionCatalog` + política en DI + atributo en controller |
| Nuevo rol personalizado    | Crear via UI Admin → almacenado en DB, sin cambio de código |
