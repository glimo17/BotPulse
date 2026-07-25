# Documento de Requisitos - BotPulse (RPA Operations Platform)

## Introducción

BotPulse es una **plataforma agnóstica de operaciones RPA** (Robotic Process Automation) diseñada para el monitoreo, gestión, análisis y orquestación centralizada de entornos RPA empresariales. A diferencia de un dashboard vinculado a un único proveedor, BotPulse se construye desde el primer día como una plataforma independiente del vendor, capaz de integrarse con múltiples ecosistemas de automatización.

**UiPath es el primer proveedor soportado**, pero la arquitectura, el modelo de dominio, los nombres de conceptos y las interfaces del Core son deliberadamente genéricos. El Core de BotPulse nunca conoce a UiPath: interactúa con abstracciones (`IRobotProvider`, `IJobProvider`, `IQueueProvider`, etc.) que pueden ser implementadas por cualquier proveedor futuro (Power Automate, Blue Prism, Automation Anywhere, Automation 360, etc.) sin modificar la lógica de negocio.

El objetivo del producto es proporcionar a los equipos de operaciones RPA una plataforma unificada que ofrezca:

- Visibilidad operacional en tiempo real de trabajos, colas, robots, máquinas y procesos
- Gestión de ciclos de vida de trabajos (iniciar, detener, cancelar, reintentar)
- Motor de alertas configurable con múltiples canales de notificación
- Dashboard con widgets personalizables por usuario y rol
- Análisis histórico de ejecuciones y métricas operacionales
- Autenticación empresarial pluggable (Entra ID, LDAP, Local)
- Despliegue flexible en múltiples entornos (Docker, Azure, IIS, Linux)

## Glosario

- **BotPulse**: Plataforma agnóstica de operaciones RPA para monitoreo, gestión y análisis multi-vendor
- **RPA (Robotic Process Automation)**: Automatización de procesos empresariales mediante robots de software
- **RPA Provider (Proveedor RPA)**: Componente que implementa una o más interfaces del Core para integrar un vendor RPA específico (UiPath, Power Automate, Blue Prism, Automation Anywhere, etc.)
- **RPA Vendor**: Fabricante o producto de la plataforma RPA que se integra a BotPulse
- **Core**: Núcleo de dominio y aplicación de BotPulse, independiente de cualquier proveedor concreto
- **IRobotProvider / IJobProvider / IQueueProvider / ILogProvider / IAssetProvider / IMachineProvider / IProcessProvider**: Interfaces granulares que definen operaciones específicas del dominio RPA. Un proveedor concreto (ej. UiPath) puede implementar una o varias de estas interfaces
- **Provider Version**: Identificador de versión de un proveedor concreto (por ejemplo, UiPath Provider v1, UiPath Provider v2), utilizado para negociar la compatibilidad de API
- **Authentication Provider**: Componente que implementa `IAuthenticationProvider` para autenticar usuarios contra un backend específico (Entra ID, LDAP, Local, etc.)
- **IAuthenticationProvider**: Interfaz de abstracción para proveedores de autenticación pluggables
- **RBAC (Role-Based Access Control)**: Sistema de autorización basado en roles obligatorio en BotPulse
- **Role**: Conjunto de permisos aplicado a un usuario (Viewer, Operator, Administrator)
- **JWT (JSON Web Token)**: Token firmado utilizado exclusivamente como token de sesión post-autenticación
- **Robot**: Entidad ejecutora de procesos en el RPA Provider. Se lee on-demand
- **Machine (Máquina)**: Host físico o virtual donde se ejecutan robots. Se lee on-demand
- **Process (Proceso)**: Definición de automatización publicada en el RPA Provider. Se lee on-demand
- **Asset (Activo)**: Recurso configurado en el RPA Provider (credencial, texto, configuración). Se lee on-demand y nunca se persiste
- **Job (Trabajo)**: Instancia de ejecución de un Process por un Robot. Se persiste localmente para análisis histórico
- **Queue (Cola)**: Estructura de trabajo pendiente gestionada por el RPA Provider
- **Queue Item (Elemento de Cola)**: Unidad de trabajo individual dentro de una Queue. Se persiste localmente
- **Execution Log**: Registro estructurado de eventos de una ejecución específica. Se persiste localmente
- **Metric (Métrica)**: Dato cuantitativo derivado de las operaciones RPA (tasa de éxito, throughput, duración media). Se persiste localmente
- **Audit Record**: Registro inmutable de acciones sensibles ejecutadas por usuarios (accesos, cambios de configuración, acciones sobre jobs). Se persiste localmente
- **Read On-Demand**: Patrón de lectura directa contra el RPA Provider en el momento en que se solicita el dato, sin persistencia local
- **Persisted Data**: Datos almacenados localmente en la base de datos de BotPulse para consulta histórica y análisis
- **Background Worker**: Servicio de fondo que ejecuta múltiples Synchronization Services de forma independiente
- **Synchronization Service**: Servicio autónomo responsable de sincronizar un único tipo de dato (JobSynchronizationService, QueueItemSynchronizationService, LogSynchronizationService, MetricsCollectionService)
- **SynchronizationOrchestrator**: Componente que coordina el scheduling y activación/desactivación de los Synchronization Services
- **Alert Engine**: Motor de reglas que evalúa condiciones sobre datos operacionales y genera alertas
- **Alert Rule**: Regla configurable que define condiciones para disparar una alerta (umbral, ventana temporal, severidad)
- **Alert**: Notificación generada por el Alert Engine cuando una regla se cumple
- **Alert Channel**: Canal de entrega de alertas (Log, Email, Slack, Teams, Webhook)
- **Severity**: Nivel de criticidad de una alerta (Info, Warning, Critical)
- **Acknowledgment**: Acción de un usuario que marca una alerta como reconocida
- **Escalation**: Mecanismo automático de re-notificación cuando una alerta crítica no es reconocida
- **Widget**: Componente visual configurable del Dashboard (Robot Monitor, Job Queue, Queue Progress, Machine Health, KPI Summary, Alerts, Execution Timeline)
- **Dashboard Layout**: Configuración de widgets, orden y ajustes visuales guardada por usuario
- **Real-Time Notification**: Actualización enviada al cliente sin necesidad de recarga o polling explícito por parte del usuario final
- **INotificationDelivery**: Interfaz de abstracción para la entrega de notificaciones en tiempo real (SSE, Polling, SignalR, WebSockets)
- **ICacheService**: Interfaz de abstracción para el servicio de caché (In-Memory, Redis, Distributed Cache)
- **API v1**: Primera versión pública de la API REST de BotPulse, expuesta bajo `/api/v1`
- **OAuth2 Client Credentials**: Flujo OAuth2 utilizado por el UiPath Provider para autenticarse contra UiPath Orchestrator
- **Health Check**: Endpoint que expone el estado operacional del sistema (`/health`, `/health/live`, `/health/ready`)
- **Structured Logging**: Registro de eventos con contexto estructurado mediante Serilog
- **Configuration Provider**: Fuente de configuración externa (environment variables, appsettings.json, secret store)

## Visión de Producto

BotPulse aspira a convertirse en la **capa de operaciones estándar para entornos RPA multi-vendor**, ocupando el espacio de una plataforma neutral entre las herramientas de orquestación propietarias (UiPath Orchestrator, Power Automate Center, Blue Prism Control Room, Automation Anywhere Control Room) y las necesidades operacionales unificadas de las empresas.

**Principios de producto:**

1. **Vendor-Agnostic Core**: El dominio y las reglas de negocio del Core desconocen cualquier vendor específico. La palabra "UiPath" no aparece en el Core.
2. **Provider Pluggability**: Nuevos proveedores RPA se incorporan implementando interfaces granulares, sin modificar el Core.
3. **Selective Persistence**: Solo se persiste lo que aporta valor histórico o analítico. El resto se lee bajo demanda del proveedor.
4. **Operational First**: El producto prioriza visibilidad, alertas y control operacional sobre features cosméticas.
5. **Enterprise-Ready Deployment**: El mismo código se despliega en Docker Compose, Azure App Service, Azure Container Apps, IIS Windows y Linux con Reverse Proxy sin modificaciones.
6. **Extensible Alerting**: El Alert Engine es un módulo de primer nivel, no una funcionalidad accesoria.
7. **Configurable Experience**: El Dashboard es personalizable por usuario mediante widgets.

**Roadmap de proveedores (informativo):**

- Fase 1 (MVP): UiPath
- Fase 2: Power Automate
- Fase 3: Blue Prism, Automation Anywhere

## Requisitos Funcionales

---

### Sección 1: Autenticación e Identidad (Pluggable)

#### Requisito 1: Abstracción del Proveedor de Autenticación

**Historia de Usuario:** Como arquitecto de la plataforma, quiero que la autenticación esté abstraída detrás de una interfaz, para poder soportar múltiples proveedores de identidad sin modificar el Core.

##### Criterios de Aceptación

1. THE **Core Application** SHALL depend on the `IAuthenticationProvider` abstraction and SHALL NOT reference any concrete authentication implementation directly.
2. WHEN the application resolves authentication, THE **Dependency Injection Container** SHALL provide the configured `IAuthenticationProvider` implementation based on `AUTHENTICATION_PROVIDER` configuration value.
3. WHERE a new authentication provider is added (Okta, Auth0, Google Workspace), THE **Core Application** SHALL integrate the new provider without any modification to the Core project.
4. IF no authentication provider is configured, THEN THE **Startup Handler** SHALL log a critical error and prevent application start.

---

#### Requisito 2: Proveedores de Autenticación Soportados

**Historia de Usuario:** Como administrador del sistema, quiero elegir entre Microsoft Entra ID, LDAP/Active Directory o autenticación local, para adaptar BotPulse al modelo de identidad de mi organización.

##### Criterios de Aceptación

1. THE **BotPulse Platform** SHALL provide out-of-the-box implementations of `IAuthenticationProvider` for: Microsoft Entra ID (Azure AD), LDAP/Active Directory, and Local Authentication.
2. WHERE the configured provider is `EntraID`, THE **EntraIdAuthenticationProvider** SHALL authenticate users using OpenID Connect against the configured Azure AD tenant.
3. WHERE the configured provider is `LDAP`, THE **LdapAuthenticationProvider** SHALL authenticate users against the configured LDAP or Active Directory server using bind operations.
4. WHERE the configured provider is `Local`, THE **LocalAuthenticationProvider** SHALL authenticate users against a local user store using password hashing with a modern algorithm (Argon2id or bcrypt with cost >= 12).
5. WHEN the `Local` provider is active, THE **Startup Handler** SHALL log a warning indicating that local authentication is intended for development environments only.

---

#### Requisito 3: JWT como Token de Sesión

**Historia de Usuario:** Como usuario autenticado, quiero recibir un token de sesión firmado que me permita acceder a los endpoints sin volver a autenticarme, para operar con fluidez.

##### Criterios de Aceptación

1. WHEN authentication succeeds through any `IAuthenticationProvider`, THE **Session Token Service** SHALL issue a JWT containing user identifier, role claims and expiration.
2. THE **JWT Session Token** SHALL have a configurable expiration between 15 minutes and 8 hours, with a default of 1 hour.
3. WHEN a request is received with a JWT, THE **JWT Validator** SHALL verify the signature, issuer, audience and expiration before granting access.
4. IF a JWT is invalid, expired or tampered, THEN THE **JWT Validator** SHALL return HTTP 401 Unauthorized.
5. THE **JWT Signing Key** SHALL be loaded from a secret configuration provider and SHALL NOT be hardcoded in source code.

---

### Sección 2: Autorización (RBAC)

#### Requisito 4: Roles y Permisos Granulares

**Historia de Usuario:** Como administrador de seguridad, quiero asignar roles con permisos granulares a los usuarios, para aplicar el principio de menor privilegio.

##### Criterios de Aceptación

1. THE **Authorization Service** SHALL support at minimum three built-in roles: `Viewer`, `Operator`, `Administrator`.
2. THE **Viewer Role** SHALL grant read-only access to dashboards, jobs, queues, logs and metrics.
3. THE **Operator Role** SHALL grant all Viewer permissions plus job actions (start, stop, cancel, retry) and alert acknowledgment.
4. THE **Administrator Role** SHALL grant all Operator permissions plus configuration management, user management, alert rule configuration and access to audit records.
5. WHEN a user attempts an action, THE **Authorization Service** SHALL evaluate the required permission against the user's role and deny with HTTP 403 Forbidden if not authorized.
6. WHEN an authorization decision is made, THE **Audit Logger** SHALL record the user, action, resource and decision.

---

#### Requisito 5: Restricción de Acceso a Recursos Sensibles

**Historia de Usuario:** Como responsable de seguridad, quiero que operaciones sensibles como el listado de assets estén restringidas por rol, para evitar exposición de configuraciones críticas.

##### Criterios de Aceptación

1. WHERE an endpoint returns Asset metadata, THE **Authorization Service** SHALL require the Administrator role.
2. WHERE an endpoint modifies Alert Rules, THE **Authorization Service** SHALL require the Administrator role.
3. WHERE an endpoint performs a job action (start, stop, cancel, retry), THE **Authorization Service** SHALL require at least the Operator role.
4. WHEN a Viewer attempts an unauthorized action, THE **Authorization Service** SHALL return HTTP 403 Forbidden and log the attempt as a security event.

---

### Sección 3: Integración con RPA Provider

#### Requisito 6: Interfaces Granulares del Proveedor RPA

**Historia de Usuario:** Como arquitecto, quiero interfaces separadas para cada aspecto del proveedor RPA, para mejorar testabilidad, mantenibilidad y permitir implementaciones parciales.

##### Criterios de Aceptación

1. THE **Core Application** SHALL define the following granular interfaces: `IRobotProvider`, `IJobProvider`, `IQueueProvider`, `ILogProvider`, `IAssetProvider`, `IMachineProvider`, `IProcessProvider`.
2. THE **Core Application** SHALL NOT define a single monolithic `IRpaProvider` interface.
3. WHERE a concrete vendor implementation is created (e.g., UiPath), THE **Vendor Provider** SHALL implement one or more of the granular interfaces as appropriate.
4. WHEN business services need provider functionality, THE **Business Services** SHALL depend only on the specific interface required, not on a broader aggregate.
5. WHERE a vendor does not support a specific capability, THE **Dependency Injection Container** SHALL either bind a null-object implementation or fail startup with a descriptive error, based on configuration.

---

#### Requisito 7: UiPath como Primer Proveedor Soportado

**Historia de Usuario:** Como responsable del MVP, quiero que UiPath sea el primer proveedor operativo, cubriendo las interfaces necesarias mediante OAuth2 Client Credentials.

##### Criterios de Aceptación

1. THE **UiPath Provider** SHALL implement `IRobotProvider`, `IJobProvider`, `IQueueProvider`, `ILogProvider`, `IAssetProvider`, `IMachineProvider` and `IProcessProvider`.
2. WHEN the UiPath Provider initializes, THE **OAuth2 Client** SHALL authenticate against UiPath Orchestrator using the OAuth2 Client Credentials flow with Client ID and Client Secret loaded from a secret configuration provider.
3. WHEN an access token is obtained, THE **Token Manager** SHALL cache the token in memory and refresh it before expiration.
4. IF token refresh fails, THEN THE **UiPath Provider** SHALL attempt full re-authentication and log the event.
5. IF re-authentication fails, THEN THE **UiPath Provider** SHALL mark itself as unhealthy so `/health/ready` reports a degraded state.
6. WHEN any UiPath REST call is executed, THE **HTTP Client** SHALL include the OAuth2 access token in the `Authorization` header and set a configurable timeout with a default of 30 seconds.

---

#### Requisito 8: Negociación de Versión de Proveedor

**Historia de Usuario:** Como platform owner, quiero que BotPulse negocie automáticamente la versión de API soportada por el proveedor, para soportar múltiples versiones de UiPath sin cambios de código en el consumidor.

##### Criterios de Aceptación

1. WHEN the UiPath Provider connects, THE **Provider Version Negotiator** SHALL query the vendor for its supported API version.
2. WHEN the vendor version is obtained, THE **Provider Factory** SHALL select the compatible provider implementation (e.g., UiPath Provider v1 or v2) among those registered.
3. IF no compatible provider implementation exists for the vendor version, THEN THE **Startup Handler** SHALL log a critical error and prevent application start.
4. WHEN a provider version is selected, THE **Configuration Store** SHALL record the negotiated version for the lifetime of the session.
5. WHILE a session is active, THE **Provider Factory** SHALL NOT switch versions dynamically.

---

### Sección 4: Robots (Read On-Demand)

#### Requisito 9: Consulta de Robots Bajo Demanda

**Historia de Usuario:** Como operador, quiero ver los robots del proveedor directamente y actualizados, sin depender de una sincronización previa.

##### Criterios de Aceptación

1. WHEN the Robots view is requested, THE **Robot Service** SHALL invoke `IRobotProvider.GetRobotsAsync()` against the configured RPA Provider.
2. THE **Robot Service** SHALL NOT persist Robot entities in the BotPulse database.
3. WHERE optional short-lived caching is enabled, THE **Cache Service** SHALL cache the robot list for a configurable duration between 30 and 300 seconds, with a default of 120 seconds.
4. WHEN a manual refresh is requested by the user, THE **Robot Service** SHALL bypass any cache and fetch fresh data from the provider.
5. IF the RPA Provider returns an error, THEN THE **Robot Service** SHALL return an error response with a diagnostic code and log the failure.

---

#### Requisito 10: Visualización de Robots

**Historia de Usuario:** Como operador, quiero visualizar los robots con su estado y máquina asignada, para identificar rápidamente disponibilidad.

##### Criterios de Aceptación

1. THE **Robot List Component** SHALL display robot name, current status, assigned machine and last heartbeat timestamp as returned by the provider.
2. WHEN a robot is reported as Online, THE **Robot List Component** SHALL display a green status indicator.
3. WHEN a robot is reported as Offline, THE **Robot List Component** SHALL display a red status indicator.
4. WHEN a robot is reported as Idle or Busy, THE **Robot List Component** SHALL display distinct indicators for each state.
5. THE **Robot List Component** SHALL support filtering by status and searching by robot name.

---

### Sección 5: Máquinas (Read On-Demand)

#### Requisito 11: Consulta de Máquinas Bajo Demanda

**Historia de Usuario:** Como operador, quiero ver las máquinas del proveedor sin sincronización previa, dado que su información cambia con poca frecuencia.

##### Criterios de Aceptación

1. WHEN the Machines view is requested, THE **Machine Service** SHALL invoke `IMachineProvider.GetMachinesAsync()` against the configured RPA Provider.
2. THE **Machine Service** SHALL NOT persist Machine entities in the BotPulse database.
3. WHERE short-lived caching is enabled, THE **Cache Service** SHALL cache the machine list for a configurable duration with a default of 300 seconds.
4. WHEN machine details are requested, THE **Machine Service** SHALL fetch details on-demand and NOT persist them.

---

#### Requisito 12: Visualización de Máquinas

**Historia de Usuario:** Como operador, quiero ver el estado de las máquinas con robots asociados, para detectar problemas de infraestructura.

##### Criterios de Aceptación

1. THE **Machine List Component** SHALL display machine name, status, connected robot count and last heartbeat timestamp from the provider.
2. WHEN a machine is Online, THE **Machine List Component** SHALL display a green status indicator.
3. WHEN a machine is Offline, THE **Machine List Component** SHALL display a red status indicator.
4. WHEN a machine row is expanded, THE **Machine Detail Panel** SHALL display connected robots retrieved on-demand.

---

### Sección 6: Procesos (Read On-Demand)

#### Requisito 13: Consulta de Procesos Bajo Demanda

**Historia de Usuario:** Como operador, quiero acceder a los procesos publicados en el proveedor bajo demanda, para iniciar ejecuciones con la versión más reciente.

##### Criterios de Aceptación

1. WHEN the Processes view is requested, THE **Process Service** SHALL invoke `IProcessProvider.GetProcessesAsync()` against the configured RPA Provider.
2. THE **Process Service** SHALL NOT persist Process definitions in the BotPulse database.
3. WHERE short-lived caching is enabled, THE **Cache Service** SHALL cache the process list for a configurable duration with a default of 600 seconds.
4. WHEN a new process version is published in the provider, THE **Process Service** SHALL reflect it on the next on-demand fetch or manual refresh.

---

#### Requisito 14: Visualización y Detalles de Procesos

**Historia de Usuario:** Como operador, quiero ver los procesos y sus parámetros de entrada, para ejecutarlos correctamente.

##### Criterios de Aceptación

1. THE **Process List Component** SHALL display process name, version, publication status and compatibility metadata.
2. THE **Process List Component** SHALL support searching by process name and filtering by publication status.
3. WHEN a process is selected, THE **Process Detail Panel** SHALL fetch parameter definitions on-demand from `IProcessProvider.GetProcessParametersAsync(processId)`.

---

### Sección 7: Assets (Read On-Demand)

#### Requisito 15: Consulta de Assets Bajo Demanda con Restricciones de Seguridad

**Historia de Usuario:** Como administrador de seguridad, quiero que los assets del proveedor se consulten bajo demanda y nunca se persistan, y que sus valores secretos nunca se expongan.

##### Criterios de Aceptación

1. WHEN an authorized user requests Assets, THE **Asset Service** SHALL invoke `IAssetProvider.GetAssetsAsync()` against the configured RPA Provider.
2. THE **Asset Service** SHALL NOT persist Asset entities or Asset values in the BotPulse database.
3. WHEN Asset metadata is returned, THE **Asset Service** SHALL exclude the secret value and return only name, type, scope and last modified timestamp.
4. IF the user role is not Administrator, THEN THE **Authorization Service** SHALL deny the request with HTTP 403 Forbidden.
5. WHEN an Asset is accessed, THE **Audit Logger** SHALL record the user, timestamp, asset name and originating IP.
6. IF the RPA Provider returns an error while fetching Assets, THEN THE **Asset Service** SHALL return a user-friendly error message and log the diagnostic details.

---

### Sección 8: Jobs (Persisted)

#### Requisito 16: Persistencia de Jobs mediante Sincronización

**Historia de Usuario:** Como analista, quiero que los trabajos se persistan localmente, para realizar consultas y análisis históricos sin cargar al proveedor.

##### Criterios de Aceptación

1. WHEN the `JobSynchronizationService` runs, THE **Job Sync Task** SHALL invoke `IJobProvider.GetJobsAsync(since)` to retrieve jobs updated since the last successful sync.
2. WHEN jobs are retrieved, THE **Job Repository** SHALL upsert each job by its external identifier, preserving fields: external job id, robot identifier, process identifier, machine identifier, start time, end time, duration, status, error type, error message.
3. WHEN a job transitions to a terminal state (Success, Failed, Stopped, Cancelled), THE **Job Repository** SHALL persist the terminal state and stop updating that job on subsequent syncs.
4. THE **Job Retention Policy** SHALL be configurable, with a default retention of 90 days for full detail and 12 months for aggregated summaries.
5. WHERE the retention window expires, THE **Job Archival Service** SHALL either delete or move records to cold storage based on configuration.

---

#### Requisito 17: Consulta y Filtros Avanzados de Jobs

**Historia de Usuario:** Como analista, quiero filtrar y ordenar el historial de jobs, para investigar patrones de fallo y desempeño.

##### Criterios de Aceptación

1. THE **Job Query Service** SHALL support filtering by date range, robot, process, machine, status and error type.
2. THE **Job List Component** SHALL display job id, robot, process, start time, end time, duration and status.
3. WHEN the status is Success, THE **Job List Component** SHALL show a green indicator; when Failed, red; when Running, yellow; when Cancelled or Stopped, gray.
4. THE **Job List Component** SHALL support pagination with a configurable page size and a default of 50 rows.
5. THE **Job List Component** SHALL support sorting by any visible column.
6. WHERE export is requested, THE **Export Service** SHALL generate a CSV of the filtered result set.

---

#### Requisito 18: Acciones sobre Jobs (Iniciar, Detener, Cancelar, Reintentar)

**Historia de Usuario:** Como operador, quiero iniciar, detener, cancelar y reintentar jobs desde BotPulse, para gestionar activamente las ejecuciones.

##### Criterios de Aceptación

1. WHEN an Operator requests to start a job, THE **Job Command Service** SHALL invoke `IJobProvider.StartJobAsync(processId, robotId, parameters)` and record the operation in the Audit Log.
2. WHEN an Operator requests to stop a running job, THE **Job Command Service** SHALL request confirmation and invoke `IJobProvider.StopJobAsync(jobId)`.
3. WHEN an Operator requests to cancel a pending job, THE **Job Command Service** SHALL request confirmation and invoke `IJobProvider.CancelJobAsync(jobId)`.
4. WHEN an Operator requests to retry a failed job, THE **Job Command Service** SHALL invoke `IJobProvider.StartJobAsync` with the original parameters and link the new job to the original one in the repository.
5. IF the provider returns an error for any job action, THEN THE **Job Command Service** SHALL return the error to the caller and record the failure in the Audit Log.
6. WHEN a job action is submitted, THE **Job Command Service** SHALL emit a `JobActionRequested` event to the notification pipeline.

---

### Sección 9: Queues y Queue Items (Persisted)

#### Requisito 19: Metadata de Colas Bajo Demanda

**Historia de Usuario:** Como operador, quiero ver los metadatos de las colas del proveedor bajo demanda, para reflejar cambios de configuración rápidamente.

##### Criterios de Aceptación

1. WHEN the Queues view is requested, THE **Queue Service** SHALL invoke `IQueueProvider.GetQueuesAsync()` against the configured provider.
2. THE **Queue Service** SHALL NOT persist Queue metadata (definitions) in the BotPulse database.
3. WHERE short-lived caching is enabled, THE **Cache Service** SHALL cache the queue metadata for a configurable duration with a default of 180 seconds.

---

#### Requisito 20: Persistencia de Queue Items para Análisis

**Historia de Usuario:** Como analista, quiero que los queue items se persistan localmente, para auditar y analizar cuellos de botella.

##### Criterios de Aceptación

1. WHEN the `QueueItemSynchronizationService` runs, THE **Queue Item Sync Task** SHALL invoke `IQueueProvider.GetQueueItemsAsync(since)` to retrieve queue items updated since the last successful sync.
2. WHEN queue items are retrieved, THE **Queue Item Repository** SHALL upsert each item preserving: external item id, queue name, status (New, InProgress, Success, Failed, Retried, Abandoned), retry count, processing start, processing end, output metadata.
3. WHEN a queue item is retried, THE **Queue Item Repository** SHALL maintain a link between the original item and each retry attempt.
4. THE **Queue Analytics Service** SHALL compute queue-level metrics (pending count, processed count, failed count, average processing time) from persisted queue items.
5. THE **Queue Item Retention Policy** SHALL be configurable with a default of 90 days.

---

### Sección 10: Execution History y Logs (Persisted)

#### Requisito 21: Sincronización y Persistencia de Execution Logs

**Historia de Usuario:** Como soporte, quiero que los logs de ejecución se persistan, para diagnosticar problemas sin depender del proveedor.

##### Criterios de Aceptación

1. WHEN the `LogSynchronizationService` runs, THE **Log Sync Task** SHALL invoke `ILogProvider.GetExecutionLogsAsync(since)` to retrieve execution logs since the last successful sync.
2. WHEN logs are retrieved, THE **Log Repository** SHALL persist each entry preserving: timestamp, severity (Debug, Info, Warn, Error, Fatal), logger name, message, job identifier, robot identifier, process identifier and structured properties.
3. WHEN log ingestion volume exceeds 1000 records per sync cycle, THE **Log Repository** SHALL use batched inserts with a configurable batch size and a default of 500.
4. THE **Log Retention Policy** SHALL be configurable with a default of 30 days for full detail.
5. WHEN the retention window expires, THE **Log Archival Service** SHALL delete or move records to cold storage based on configuration.

---

#### Requisito 22: Visualización y Búsqueda de Logs

**Historia de Usuario:** Como soporte, quiero visualizar y buscar dentro de los logs persistidos, para diagnosticar rápidamente.

##### Criterios de Aceptación

1. THE **Log Viewer** SHALL display logs in chronological order with timestamp, severity, message, job id and robot name.
2. THE **Log Viewer** SHALL support filtering by severity, date range, job id, robot and process.
3. THE **Log Viewer** SHALL support keyword search within the message field.
4. WHERE log volume exceeds 10,000 rows, THE **Log Viewer** SHALL apply pagination or virtualized scrolling.

---

### Sección 11: Métricas Operacionales (Persisted)

#### Requisito 23: Cálculo y Persistencia de Métricas Operacionales

**Historia de Usuario:** Como gerente operacional, quiero métricas históricas de KPIs, para visualizar tendencias.

##### Criterios de Aceptación

1. WHEN the `MetricsCollectionService` runs, THE **Metrics Collector** SHALL compute and persist the following metrics per interval: total jobs executed, success count, failed count, cancelled count, average execution duration, success rate percentage, queue backlog count, robot availability percentage, machine availability percentage.
2. WHEN metrics are persisted, THE **Metrics Repository** SHALL store each data point with timestamp, metric name, value and dimensions (robot, process, machine, queue as applicable).
3. WHEN historical charts are rendered, THE **Time Series Query Service** SHALL retrieve metric points efficiently using indexed queries.
4. THE **Metrics Aggregation Service** SHALL compute hourly and daily rollups from raw points for long-range visualizations.
5. THE **Metrics Retention Policy** SHALL be configurable, with a default of 30 days for raw points and 12 months for aggregated rollups.

---

#### Requisito 24: KPIs en el Dashboard

**Historia de Usuario:** Como gerente, quiero ver los KPIs de alto nivel del entorno RPA, para monitorear el estado general.

##### Criterios de Aceptación

1. THE **KPI Summary Widget** SHALL display total robots (from on-demand fetch), online robot count, offline robot count, jobs executed today, success rate percentage and queue backlog count.
2. WHEN a KPI card is clicked, THE **Drill-Down View** SHALL navigate to the detailed source (jobs list, queues list, robot list).
3. THE **KPI Summary Widget** SHALL refresh automatically via the Real-Time Notification pipeline described in Section 14.

---

### Sección 12: Dashboard Configurable (Widgets)

#### Requisito 25: Widgets Personalizables por Usuario

**Historia de Usuario:** Como usuario, quiero personalizar mi dashboard con los widgets que me son relevantes, para enfocarme en lo que importa a mi rol.

##### Criterios de Aceptación

1. THE **Dashboard Builder** SHALL expose the following widget types: Robot Monitor, Job Queue, Queue Progress, Machine Health, KPI Summary, Alerts, Execution Timeline.
2. WHEN a user enables or disables a widget, THE **Dashboard Configuration Service** SHALL persist the change scoped to the user.
3. WHEN a user reorders widgets via drag-and-drop, THE **Dashboard Configuration Service** SHALL persist the new order scoped to the user.
4. WHEN a user modifies widget-specific settings (refresh interval, filters), THE **Dashboard Configuration Service** SHALL persist the settings.
5. WHEN a user logs in, THE **Dashboard Renderer** SHALL load the persisted configuration for that user and render the widgets accordingly.

---

#### Requisito 26: Layouts Predefinidos por Rol

**Historia de Usuario:** Como administrador, quiero que cada rol tenga un layout inicial recomendado, para acelerar la adopción.

##### Criterios de Aceptación

1. THE **Role-Based Default Layout** SHALL define an initial widget set for Viewer, Operator and Administrator roles.
2. WHEN a user is created, THE **Dashboard Initializer** SHALL apply the default layout for the user's role.
3. WHEN a user selects "Reset to default", THE **Dashboard Configuration Service** SHALL restore the role-based default layout for that user.

---

#### Requisito 27: Autorización de Widgets

**Historia de Usuario:** Como responsable de seguridad, quiero que los widgets respeten los permisos del rol, para evitar exposición no autorizada.

##### Criterios de Aceptación

1. THE **Widget Permission Model** SHALL define required permissions per widget type.
2. WHEN a user opens the Dashboard Builder, THE **Dashboard Builder** SHALL list only widgets for which the user has view permission.
3. IF a widget requires a permission the user lacks, THEN THE **Dashboard Renderer** SHALL hide the widget and log the event as an authorization decision.

---

### Sección 13: Alert Engine

#### Requisito 28: Motor de Reglas Configurable

**Historia de Usuario:** Como operador, quiero que el sistema evalúe reglas de alerta automáticamente, para reaccionar proactivamente ante condiciones anómalas.

##### Criterios de Aceptación

1. THE **Alert Engine** SHALL evaluate configured `AlertRule` instances at a configurable interval with a default of 60 seconds.
2. WHEN a rule condition is met, THE **Alert Generator** SHALL create an `Alert` record with rule id, severity, timestamp, condition description and affected resource identifiers.
3. WHERE the same rule fires repeatedly on the same resource, THE **Alert Deduplication Service** SHALL suppress duplicates within a configurable window with a default of 5 minutes.
4. WHEN an alert is generated, THE **Alert Publisher** SHALL emit an event through the Real-Time Notification pipeline defined in Section 14.
5. WHEN an alert is generated, THE **Audit Logger** SHALL record the generation event.

---

#### Requisito 29: Reglas Predefinidas

**Historia de Usuario:** Como operador, quiero que BotPulse provea reglas listas para las situaciones RPA más comunes, para reducir el tiempo de configuración inicial.

##### Criterios de Aceptación

1. THE **Alert Engine** SHALL provide built-in rule types:
   - **Robot Offline**: robot offline for more than a configurable threshold with a default of 10 minutes, severity Critical.
   - **Queue Backlog**: queue pending item count above a configurable threshold with defaults of 500 (Warning), 1000 (Warning), 2000 (Critical).
   - **Jobs Failed in Window**: N or more jobs failed within a time window, defaults 5 jobs / 60 minutes (Warning), 10 jobs / 60 minutes (Critical).
   - **Machine Offline**: machine offline for more than a configurable threshold with a default of 60 minutes, severity Critical.
   - **Process Execution Time Exceeded**: job duration greater than expected duration per process, severity Warning.
2. THE **Alert Configuration API** SHALL allow Administrators to enable, disable and adjust thresholds for any predefined rule.
3. WHEN a threshold is updated, THE **Alert Engine** SHALL apply the new threshold on the next evaluation cycle without restart.

---

#### Requisito 30: Canales de Notificación Extensibles

**Historia de Usuario:** Como operador, quiero recibir alertas por múltiples canales, para asegurar la entrega crítica.

##### Criterios de Aceptación

1. THE **Notification Router** SHALL support the following alert channels: Log (always enabled), Email, Slack, Microsoft Teams and generic Webhook.
2. WHERE additional channels are required, THE **Notification Router** SHALL support plugin-based channel providers implementing an `IAlertChannel` interface without core modifications.
3. WHEN an alert is generated, THE **Notification Router** SHALL dispatch the alert to the channels configured for the rule and severity.
4. IF a channel delivery fails, THEN THE **Notification Router** SHALL retry with exponential backoff up to a configurable maximum with a default of 3 attempts.
5. WHEN a channel is unavailable after all retries, THE **Notification Router** SHALL log the failure and mark the channel as degraded in health checks.

---

#### Requisito 31: Historial y Acknowledgment de Alertas

**Historia de Usuario:** Como operador, quiero ver el historial de alertas y poder reconocerlas, para llevar control operativo.

##### Criterios de Aceptación

1. THE **Alert Repository** SHALL persist every alert with: alert id, rule id, severity, timestamp, condition description, affected resource, acknowledgment status, acknowledgment user and acknowledgment timestamp.
2. THE **Alert History View** SHALL support filtering by date range, severity, rule type and affected resource.
3. WHEN an Operator or Administrator acknowledges an alert, THE **Alert Repository** SHALL record the user and timestamp and THE **Notification Router** SHALL stop further notifications for that alert instance.
4. WHERE export is requested, THE **Alert Export Service** SHALL produce a CSV of the filtered alerts.
5. THE **Alert Retention Policy** SHALL be configurable with a default of 12 months.

---

#### Requisito 32: Escalación Automática (Opcional)

**Historia de Usuario:** Como administrador, quiero que las alertas críticas no reconocidas se escalen automáticamente, para asegurar respuesta a problemas graves.

##### Criterios de Aceptación

1. WHERE escalation is enabled for a rule, THE **Escalation Engine** SHALL wait a configurable timeout with a default of 15 minutes for acknowledgment.
2. IF a Critical alert is not acknowledged within the timeout, THEN THE **Escalation Engine** SHALL dispatch the alert to the configured escalation channels or recipients and log the escalation event.
3. WHERE a second-level escalation is configured, THE **Escalation Engine** SHALL trigger it after a configurable second timeout with a default of 30 minutes.
4. WHILE escalation is in progress, THE **Alert Display** SHALL show the current escalation level.

---

### Sección 14: Real-Time Updates

#### Requisito 33: Abstracción de Entrega de Notificaciones

**Historia de Usuario:** Como arquitecto, quiero que la UI dependa de una abstracción de notificaciones y no de polling directo, para poder cambiar el transporte sin tocar la UI ni el Core.

##### Criterios de Aceptación

1. THE **Core Application** SHALL define the `INotificationDelivery` abstraction for real-time notifications.
2. THE **UI Layer** SHALL depend exclusively on the `INotificationDelivery` abstraction and SHALL NOT reference concrete transport implementations directly.
3. WHERE the MVP transport is configured, THE **Dependency Injection Container** SHALL bind `INotificationDelivery` to either the Server-Sent Events (SSE) implementation or the Polling implementation, selected via configuration.
4. WHERE future transports are added (SignalR, WebSockets), THE **Notification Delivery System** SHALL support them by adding a new `INotificationDelivery` implementation without modifying the UI Layer or the Core.

---

#### Requisito 34: Latencia y Fiabilidad de Notificaciones

**Historia de Usuario:** Como usuario, quiero ver los cambios operacionales relevantes en menos de 10 segundos, para operar con información fresca.

##### Criterios de Aceptación

1. FROM the moment a job state transition is persisted, UNTIL the corresponding notification is delivered to a connected client, THE **End-to-End Latency** SHALL NOT exceed 10 seconds under normal conditions.
2. WHEN a notification connection drops, THE **Reconnection Handler** SHALL attempt reconnection with a backoff of 1, 2, 4, 8 seconds up to a maximum of 30 seconds between attempts.
3. WHILE many state changes occur in a short interval, THE **Notification Throttler** SHALL coalesce updates per resource to no more than one delivery per second per resource.
4. WHEN a notification is emitted, THE **Notification Delivery System** SHALL include event type, resource identifier, new state and timestamp.

---

### Sección 15: Background Synchronization Services

#### Requisito 35: Servicios de Sincronización Independientes

**Historia de Usuario:** Como arquitecto, quiero tareas de sincronización independientes por tipo de dato, para escalar y mantener cada una por separado.

##### Criterios de Aceptación

1. THE **Background Worker** SHALL host the following independent services as first-class components: `JobSynchronizationService`, `QueueItemSynchronizationService`, `LogSynchronizationService`, `MetricsCollectionService`.
2. WHEN a synchronization service is running, THE **Background Worker** SHALL allow other synchronization services to run concurrently without blocking each other.
3. IF one synchronization service fails, THEN THE **Background Worker** SHALL log the failure and continue running the remaining services.
4. WHERE a synchronization service is disabled by configuration, THE **Background Worker** SHALL NOT schedule it.

---

#### Requisito 36: SynchronizationOrchestrator

**Historia de Usuario:** Como operador, quiero un componente central que coordine el scheduling de los servicios, para tener visibilidad y control unificados.

##### Criterios de Aceptación

1. THE **SynchronizationOrchestrator** SHALL manage the lifecycle (start, stop, health) of every registered synchronization service.
2. THE **SynchronizationOrchestrator** SHALL expose the per-service status (last run timestamp, last outcome, next scheduled run, items processed) through the health endpoint `/health/ready` and an administrative API.
3. WHEN a service is manually triggered by an Administrator, THE **SynchronizationOrchestrator** SHALL execute the service outside of its normal schedule and record the manual invocation in the Audit Log.
4. WHEN configuration changes an interval or enabled flag, THE **SynchronizationOrchestrator** SHALL apply the change without requiring an application restart.

---

#### Requisito 37: Configuración Granular por Servicio

**Historia de Usuario:** Como administrador, quiero configurar intervalos y activación de cada servicio de sincronización independientemente.

##### Criterios de Aceptación

1. THE **Configuration Provider** SHALL expose per-service settings:
   - `SYNC_JOBS_ENABLED` / `SYNC_JOBS_INTERVAL_SECONDS` (default: true / 120)
   - `SYNC_QUEUE_ITEMS_ENABLED` / `SYNC_QUEUE_ITEMS_INTERVAL_SECONDS` (default: true / 180)
   - `SYNC_LOGS_ENABLED` / `SYNC_LOGS_INTERVAL_SECONDS` (default: true / 60)
   - `SYNC_METRICS_ENABLED` / `SYNC_METRICS_INTERVAL_SECONDS` (default: true / 300)
2. WHEN a configured interval is less than 30 seconds, THE **Configuration Validator** SHALL clamp it to 30 seconds and log a warning.
3. WHEN configuration changes at runtime, THE **SynchronizationOrchestrator** SHALL apply the new values on the next scheduled cycle.

---

### Sección 16: REST API (Versioned)

#### Requisito 38: Versionado de API Desde el Día 1

**Historia de Usuario:** Como consumidor externo de la API, quiero que la API esté versionada desde el inicio, para asegurar estabilidad contractual.

##### Criterios de Aceptación

1. THE **REST API** SHALL expose all public endpoints under the base path `/api/v1`.
2. WHEN a new API version is introduced, THE **API Gateway** SHALL expose it at `/api/v2` and continue serving `/api/v1` until it is officially deprecated.
3. WHEN a version is deprecated, THE **API Response** SHALL include a `Deprecation` header pointing to the successor version and THE **Deprecation Policy** SHALL provide at least 6 months of overlap before removal.
4. THE **OpenAPI/Swagger UI** SHALL expose one definition per active version at `/swagger/v1/swagger.json`, `/swagger/v2/swagger.json`, etc.

---

#### Requisito 39: Documentación OpenAPI y Manejo de Errores

**Historia de Usuario:** Como desarrollador de integraciones, quiero documentación estandarizada y errores consistentes, para integrar con confianza.

##### Criterios de Aceptación

1. WHEN the API starts, THE **Swagger UI** SHALL be reachable at `/swagger` and describe every endpoint including auth requirements.
2. WHEN an error occurs, THE **Error Handler Middleware** SHALL return a JSON envelope with fields: error code, human-readable message, correlation id and timestamp.
3. WHEN a validation error occurs, THE **Validation Handler** SHALL return HTTP 400 with per-field details.
4. WHEN an authorization error occurs, THE **Auth Handler** SHALL return HTTP 401 or HTTP 403 as appropriate.
5. WHEN an unhandled exception occurs, THE **Global Exception Handler** SHALL log the full stack trace with the correlation id and return a generic error message to the client.

---

### Sección 17: Health Checks

#### Requisito 40: Endpoints de Salud Production-Ready

**Historia de Usuario:** Como DevOps, quiero endpoints de salud diferenciados para monitoreo, liveness y readiness, para integrar con orquestadores de contenedores y balanceadores.

##### Criterios de Aceptación

1. THE **BotPulse API** SHALL expose the endpoints `/health`, `/health/live` and `/health/ready`.
2. THE **/health/live** endpoint SHALL return HTTP 200 as long as the process is alive and not in a terminal failure state.
3. THE **/health/ready** endpoint SHALL return HTTP 200 only when: the database is reachable, the configured RPA Provider is reachable, all enabled synchronization services are healthy and the cache service is reachable (when caching is enabled).
4. THE **/health** endpoint SHALL return an aggregated JSON payload with per-dependency status (database, RPA provider, background worker, cache) and per-synchronization-service status.
5. IF any critical dependency is unhealthy, THEN THE **/health/ready** endpoint SHALL return HTTP 503 with the offending dependencies listed.

---

### Sección 18: Logging Estructurado

#### Requisito 41: Structured Logging con Serilog

**Historia de Usuario:** Como operador de soporte, quiero logs estructurados con contexto, para correlacionar eventos y diagnosticar problemas.

##### Criterios de Aceptación

1. THE **Structured Logger** SHALL be implemented using Serilog and SHALL emit logs with timestamp, level, message template, correlation id and structured properties.
2. THE **Structured Logger** SHALL support multiple sinks configurable via configuration: Console, File and optionally a database or external log aggregator.
3. WHEN a request is processed, THE **Request Logger** SHALL log request id, user id, method, path, status code and duration.
4. WHEN a background sync service runs, THE **Sync Logger** SHALL log service name, start time, duration, items processed and outcome.
5. THE **Log Level** SHALL be configurable per namespace via configuration.

---

#### Requisito 42: Audit Logs Separados

**Historia de Usuario:** Como responsable de compliance, quiero que las acciones sensibles se registren en un audit log separado y consultable, para responder a auditorías.

##### Criterios de Aceptación

1. THE **Audit Logger** SHALL persist audit records in a dedicated repository separated from operational logs.
2. WHEN a user performs a sensitive action (login, logout, job action, alert rule change, asset access, configuration change), THE **Audit Logger** SHALL persist: user id, action, resource, timestamp, originating IP and outcome.
3. THE **Audit Log** SHALL be append-only from the application's perspective and SHALL NOT expose an API to modify or delete existing records.
4. THE **Audit Log Retention Policy** SHALL be configurable with a default of 24 months.

---

## Requisitos No Funcionales

### Performance

- El Dashboard SHALL cargar en menos de 2 segundos bajo condiciones normales de operación.
- Las actualizaciones en tiempo real SHALL alcanzar al cliente en menos de 10 segundos desde el evento en el proveedor.
- El sistema SHALL soportar la operación de al menos 1000 robots monitoreados sin degradación funcional.
- Las consultas de historial de jobs sobre 100.000 registros SHALL responder en menos de 3 segundos usando índices apropiados.

### Escalabilidad

- El Background Worker SHALL soportar escalado horizontal deshabilitando servicios de sincronización individualmente por instancia si es necesario.
- La caché SHALL depender de `ICacheService`, permitiendo migración de In-Memory a Redis o Distributed Cache sin cambios en los servicios de negocio.
- Los proveedores RPA SHALL implementarse a nivel granular (`IRobotProvider`, `IJobProvider`, etc.), permitiendo escalar responsabilidades por separado.

### Seguridad

- HTTPS SHALL ser obligatorio en todos los entornos que no sean desarrollo local.
- Las credenciales de proveedores (OAuth2 Client Secret, LDAP bind password, etc.) SHALL ser cargadas exclusivamente desde un secret store o environment variables encriptadas y nunca escritas en el código fuente.
- Todas las consultas a la base de datos SHALL utilizar consultas parametrizadas (prepared statements) para prevenir inyección SQL.
- Todo acceso a recursos sensibles (assets, configuración, audit log) SHALL registrarse en el Audit Log.
- Los tokens JWT SHALL firmarse con una clave rotable configurada externamente.

### Deployment Flexibility

- El mismo binario SHALL desplegarse en Docker Compose, Azure App Service, Azure Container Apps, IIS Windows y Linux con Reverse Proxy sin cambios de código.
- La configuración SHALL cargarse exclusivamente vía environment variables y configuration providers estándar, sin editar archivos empaquetados en la imagen.
- La imagen Docker Compose SHALL orquestar: Reverse Proxy, API, Worker, PostgreSQL y Redis (Redis provisionado aunque no se use en el MVP, para preparar cache, sessions, SignalR backplane y rate limiting).

### Extensibility

- Nuevos proveedores RPA SHALL incorporarse implementando las interfaces granulares sin modificar el Core.
- Nuevos proveedores de autenticación SHALL incorporarse implementando `IAuthenticationProvider` sin modificar el Core.
- Nuevos canales de alerta SHALL incorporarse implementando `IAlertChannel` sin modificar el Alert Engine.
- Nuevos transportes de notificaciones en tiempo real SHALL incorporarse implementando `INotificationDelivery` sin modificar la UI ni el Core.

### Observability

- El sistema SHALL emitir logs estructurados con correlation id propagado a través de todas las capas.
- El sistema SHALL exponer métricas operacionales consultables mediante la API.
- El sistema SHALL exponer endpoints de health checks (`/health`, `/health/live`, `/health/ready`) compatibles con Kubernetes y otros orquestadores.

## Restricciones Arquitectónicas

1. **Core Vendor-Agnostic**: El Core no SHALL contener referencias directas a UiPath, Power Automate, Blue Prism, Automation Anywhere ni ningún vendor específico. Toda integración vendor pasa por interfaces granulares.
2. **Clean Architecture**: La solución SHALL organizarse en capas Domain, Application, Infrastructure y Presentation con dependencias unidireccionales hacia el Domain.
3. **SOLID**: Las clases y módulos SHALL respetar los principios SOLID, en particular Interface Segregation (interfaces granulares por capacidad) y Dependency Inversion (dependencias hacia abstracciones).
4. **Provider Pattern Granular**: No SHALL existir una interfaz monolítica `IRpaProvider`. Cada capacidad tiene su interfaz específica.
5. **Persistencia Selectiva**: Únicamente Jobs, Queue Items, Execution Logs, Métricas y Audit Records SHALL persistirse localmente. Robots, Machines, Assets y Processes SHALL leerse bajo demanda.
6. **Configuration-Driven Deployment**: Todo comportamiento específico del entorno SHALL configurarse externamente sin cambios de código.
7. **Cache Abstraction**: Los servicios de negocio SHALL depender de `ICacheService` y nunca de una implementación específica de caché.
8. **Notification Abstraction**: La UI SHALL depender de `INotificationDelivery` y nunca de una implementación específica de transporte en tiempo real.
9. **Authentication Abstraction**: El Core SHALL depender de `IAuthenticationProvider` y nunca de un proveedor de identidad específico.

## Roadmap (Informativo)

### Fase 1 - MVP

- UiPath Provider (implementando `IRobotProvider`, `IJobProvider`, `IQueueProvider`, `ILogProvider`, `IAssetProvider`, `IMachineProvider`, `IProcessProvider`)
- Local Authentication Provider
- Alert Engine con reglas predefinidas y canal Log
- Real-Time Notifications vía SSE o Polling
- In-Memory Cache
- Dashboard con widgets básicos
- API v1
- Health Checks
- Deployment vía Docker Compose

### Fase 2

- Entra ID Authentication Provider
- LDAP Authentication Provider
- Alert Engine completo con canales Email, Slack, Teams, Webhook y escalación automática
- Widgets adicionales y layouts predefinidos por rol
- Deployment en Azure App Service y Azure Container Apps

### Fase 3

- Power Automate Provider
- Redis como implementación de `ICacheService`
- SignalR como implementación de `INotificationDelivery`
- Rate limiting

### Fase 4

- Blue Prism Provider
- Automation Anywhere Provider
- Multi-tenant
- Mobile dashboard
