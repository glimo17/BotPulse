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

## Fase 3 — Cognitive & RAG Engine (ADR-016) 🚀

**Objetivo:** transformar BotPulse de una plataforma de monitoreo en una plataforma de operaciones inteligentes con diagnóstico asistido por IA, auto-reparación y búsqueda en lenguaje natural.

### Sub-fase 3A: Fundación de la Intelligence Platform

- `BotPulse.Intelligence` — nuevo proyecto con arquitectura completa de abstracciones
- Provider Pattern para LLM: soporte OpenAI, Anthropic, Ollama (modelos locales)
- Provider Pattern para vector store: PostgreSQL+pgvector (MVP), Pinecone/Qdrant (futuro)
- Provider Pattern para embeddings: OpenAI, Azure, local models
- Independencia total del Core: interfaces, no dependencias directas

### Sub-fase 3B: Knowledge Base & RAG Pipeline

- Indexación de múltiples fuentes: logs, alerts, runbooks, documentos, historial de incidentes
- Pipeline RAG para diagnóstico contextualizado (recuperación semántica + LLM)
- Feedback loop: vectorización de resoluciones validadas por operadores

### Sub-fase 3C: Agent Framework

- Agent Framework genérico: Diagnostic Agent, Monitoring Agent, Recommendation Agent, Self-Healing Agent, Optimization Agent
- Tool Framework: ReadExecutionLogs, QueryDatabase, RetrieveAlerts, SearchHistoricalExecutions
- Agent Memory: short-term y long-term memory para cada agent
- Human-in-the-Loop approval para operaciones críticas

### Sub-fase 3D: Prediction Engine

- Múltiples estrategias intercambiables: Statistical Models, LLM-assisted Predictions, Rule-based Detection
- Detección proactiva de anomalies antes de degradación de rendimiento
- Integration con Alert Engine existente (nuevo evaluador `AnomalyDetectionRule`)

### Sub-fase 3E: Búsqueda NL-to-Query

- Barra de comandos conversacional en el dashboard
- Traducción de consultas naturales a filtros de API: "fallos del robot financiero ayer" → query estructurado
- Historial de búsquedas y sugerencias contextuales

### Sub-fase 3F: UI & Integration

- Panel "Análisis de IA" por job fallido con: causa técnica, impacto, pasos de resolución
- Agent Framework UI para ejecutar y monitorear agents
- Streaming de resultados con SignalR para UX asíncrona

### Arquitectura de la Intelligence Platform

- **Decoupling Total:** Core no sabe qué LLM, embedding provider o vector store se usa
- **Independent Deployment:** eventualmente desplegable como servicio independiente
- **Independent Database:** propias tablas para embeddings, knowledge, prompts
- **Provider Pattern Everywhere:** `IChatCompletionProvider`, `IEmbeddingProvider`, `IVectorStore`, `IAgentFactory`, `IToolRegistry`, `IPredictionEngine`
- **Agent Framework:** Generic infrastructure, no single-purpose agents
- **Tool Framework:** Tools como first-class abstraction para interacción IA-plataforma

### Dependencias

- Requiere PostgreSQL 15+ con extensión `pgvector` habilitada (MVP)
- Requiere Fase 2 completada (RBAC) para tener roles de administrador y auditoría
- No requiere cambios en el Core existente — nuevo proyecto `BotPulse.Intelligence` referencia solo `BotPulse.Core`

---

## Fase 4 — Real-Time + Redis + Power Automate

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

## Fase 5 — Multi-Vendor + Multi-Tenant

**Objetivo:** plataforma de nivel enterprise con soporte para múltiples vendors y aislamiento multi-tenant.

### Nuevos Proveedores RPA

- `BotPulse.Providers.BluePrism` para Blue Prism Control Room
- `BotPulse.Providers.AutomationAnywhere` para Automation Anywhere 360

### Multi-Tenant

- Aislamiento de datos por tenant en persistencia (esquema o discriminador)
- Autenticación por tenant con IdPs independientes
- Dashboard y alertas configurables por tenant
- **Vector Space Isolation**: actualización del módulo Intelligence para particionado por `organization_id` en todas las tablas

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
| Nuevo proveedor de IA/LLM  | Nueva clase `XxxChatCompletionProvider : IChatCompletionProvider` + case en DI |
| Nuevo vector store          | Nueva clase `XxxVectorStore : IVectorStore` + case en DI |
| Nuevo tema visual          | Nuevo bloque `[data-theme="xxx"]` en variables.css + entrada en themes.ts    |
| Nuevo permiso              | Nueva constante en `PermissionCatalog` + política en DI + atributo en controller |
| Nuevo rol personalizado    | Crear via UI Admin → almacenado en DB, sin cambio de código |
| Nuevo tipo de Agent        | Nueva clase `XxxAgent : IAgent` + registro en AgentFactory |
| Nuevo tipo de Tool         | Nueva clase `XxxTool : ITool` + registro en ToolRegistry |
| Nuevo tipo de Prediction Engine | Nueva clase `XxxPredictionEngine : IPredictionEngine` + case en DI |
