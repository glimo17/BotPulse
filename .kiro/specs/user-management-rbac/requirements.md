# Requisitos — User Management & Role-Based Access Control (RBAC)

## Estado: Planificado (Fase 2)

---

## Introducción

BotPulse implementa un sistema de control de acceso basado en roles (RBAC) que permite a los administradores gestionar usuarios, roles y permisos de forma centralizada. El sistema de autorización es completamente independiente del proveedor de autenticación (Local, Entra ID, LDAP, Okta, Auth0, etc.).

---

## Glosario

| Término | Definición |
|---------|-----------|
| **RBAC** | Role-Based Access Control — control de acceso basado en roles |
| **Permission** | Capacidad granular de ejecutar una acción sobre un recurso (ej. `Jobs.Execute`) |
| **Role** | Colección nombrada de permisos. Puede ser un rol del sistema o un rol personalizado |
| **System Role** | Rol predefinido no eliminable: Administrator, Operations Manager, Viewer |
| **Custom Role** | Rol creado por un administrador con permisos arbitrarios |
| **Authorization Provider** | Componente interno de BotPulse que resuelve permisos independientemente del auth provider |
| **Auth Provider** | Sistema externo que verifica identidad: Local, Entra ID, LDAP, Okta, Auth0 |

---

## Sección 1: Gestión de Usuarios

### Requisito 1: Administración de Usuarios

**Historia de Usuario:** Como administrador, quiero gestionar usuarios dentro de BotPulse, para controlar quién tiene acceso y con qué nivel.

#### Criterios de Aceptación

1. THE **User Management Module** SHALL allow administrators to view a list of all users with their roles, status and last login.
2. THE **User Management Module** SHALL allow administrators to enable or disable any user account.
3. THE **User Management Module** SHALL allow administrators to assign one or more roles to a user.
4. THE **User Management Module** SHALL allow administrators to view user activity (audit log entries by user).
5. WHEN `Authentication:Provider` is `Local`, THE **User Management Module** SHALL allow administrators to create new users with username, email and initial password.
6. WHEN `Authentication:Provider` is `Local`, THE **User Management Module** SHALL allow administrators to reset a user's password.
7. WHEN `Authentication:Provider` is `EntraID` or `LDAP`, THE **User Management Module** SHALL NOT expose user creation or password management — those remain in the external IdP.
8. FOR external providers, THE **system** SHALL synchronize only authorization-related information (roles, enabled/disabled status) and SHALL NOT store passwords.
9. WHEN a user is disabled, THE **system** SHALL reject all authenticated requests from that user, regardless of token validity.

---

## Sección 2: Roles del Sistema

### Requisito 2: Roles Predefinidos No Modificables

**Historia de Usuario:** Como plataforma, quiero tener roles del sistema bien definidos como punto de partida, para que cualquier instalación nueva tenga configuraciones de seguridad sensatas desde el primer arranque.

#### Criterios de Aceptación

1. THE **system** SHALL seed the following three system roles on startup if they do not exist: `Administrator`, `Operations Manager`, `Viewer`.
2. System roles SHALL NOT be deletable.
3. THE **Administrator** role SHALL include all permissions in the system.
4. THE **Operations Manager** role SHALL include: `Dashboard.View`, `Robots.View`, `Queues.View`, `Jobs.View`, `Jobs.Execute`, `Jobs.Cancel`, `Jobs.Retry`, `Logs.View`, `Machines.View`, `Assets.View`, `Metrics.View`.
5. THE **Viewer** role SHALL include: `Dashboard.View`, `Robots.View`, `Queues.View`, `Jobs.View`, `Metrics.View`, `Logs.View`, `Machines.View`, `Assets.View`.
6. WHEN the system starts, THE **Role Seeder** SHALL verify these roles exist and create them if missing, without overwriting custom modifications to their permission sets.

---

## Sección 3: Roles Personalizados

### Requisito 3: Creación de Roles Personalizados

**Historia de Usuario:** Como administrador, quiero crear roles con nombres y permisos personalizados, para adaptar el acceso a los perfiles específicos de mi organización sin modificar el producto.

#### Criterios de Aceptación

1. THE **Role Management Module** SHALL allow administrators to create custom roles with a unique name and description.
2. THE **Role Management Module** SHALL allow administrators to assign any combination of available permissions to a custom role.
3. THE **Role Management Module** SHALL allow administrators to edit custom roles (rename, change description, update permissions).
4. THE **Role Management Module** SHALL allow administrators to delete custom roles, provided no users are currently assigned to that role.
5. IF a custom role is deleted and users are assigned to it, THE **system** SHALL reject the deletion and inform the administrator of the affected users.
6. THE **system** SHALL support role examples such as: `Finance Supervisor`, `Night Shift Operator`, `RPA Support`, `Help Desk` — each with precisely the permissions needed.
7. Custom roles SHALL be stored in the database and SHALL persist across restarts.

---

## Sección 4: Modelo de Permisos Granulares

### Requisito 4: Catálogo de Permisos

**Historia de Usuario:** Como plataforma, quiero un conjunto granular de permisos bien definidos, para que cada acción de la UI y cada endpoint de la API pueda protegerse de forma independiente.

#### Criterios de Aceptación

1. THE **Permission Catalog** SHALL define the following permissions as the initial set:

| Permission | Description |
|-----------|-------------|
| `Dashboard.View` | Ver el dashboard y KPIs |
| `Dashboard.Export` | Exportar datos del dashboard |
| `Robots.View` | Ver lista de robots y su estado |
| `Robots.Restart` | Reiniciar un robot |
| `Jobs.View` | Ver lista y detalle de jobs |
| `Jobs.Execute` | Lanzar un proceso (Bot Launcher) |
| `Jobs.Cancel` | Cancelar un job en ejecución |
| `Jobs.Retry` | Reintentar un job fallido |
| `Queues.View` | Ver colas y sus ítems |
| `Logs.View` | Ver logs de ejecución |
| `Assets.View` | Ver assets |
| `Machines.View` | Ver máquinas |
| `Metrics.View` | Ver métricas y gráficos |
| `Alerts.View` | Ver alertas |
| `Alerts.Acknowledge` | Reconocer alertas |
| `Settings.View` | Ver configuración del sistema |
| `Settings.Edit` | Modificar configuración del sistema |
| `Users.View` | Ver lista de usuarios |
| `Users.Create` | Crear usuarios (Local auth) |
| `Users.Update` | Editar usuarios y asignar roles |
| `Users.Delete` | Eliminar usuarios |
| `Roles.View` | Ver roles y sus permisos |
| `Roles.Update` | Crear y editar roles personalizados |
| `Integrations.Configure` | Configurar integraciones RPA (UiPath, etc.) |

2. THE **Permission Catalog** SHALL be extensible — new permissions can be added without breaking existing roles.
3. Permissions SHALL be enforced server-side on every protected API endpoint.
4. Permissions SHALL NEVER be enforced only on the client side.

---

## Sección 5: Autorización por Feature

### Requisito 5: Protección de Endpoints y Features

**Historia de Usuario:** Como plataforma, quiero que cada endpoint de la API verifique los permisos del usuario antes de ejecutar la acción solicitada.

#### Criterios de Aceptación

1. EVERY protected API endpoint SHALL declare the required permission(s) using a policy attribute (e.g., `[Authorize(Policy = "Jobs.Execute")]`).
2. THE **Authorization Service** SHALL resolve permissions by: loading the user's roles → expanding roles to their permission sets → caching the result per user session.
3. WHEN a user lacks a required permission, THE **API** SHALL return `HTTP 403 Forbidden` with a descriptive error body.
4. THE **permission cache** SHALL be invalidated when a user's roles are modified.
5. THE **frontend** MAY hide UI elements based on permissions (as UX optimization), but SHALL NOT rely on client-side hiding as the security boundary.

---

## Sección 6: Seguridad a Nivel de Carpeta (Roadmap Futuro)

### Requisito 6: Preparación para Folder-Level Security

**Historia de Usuario:** Como arquitecto, quiero que el modelo de autorización soporte permisos por carpeta en el futuro, para que distintos equipos puedan acceder solo a sus procesos.

#### Criterios de Aceptación

1. THE **permission model** architecture SHALL support folder-scoped permissions in a future release (e.g., User A can execute jobs in `Finance` folder but not in `IT`).
2. THE **initial MVP** MAY defer folder-level enforcement, but the data model SHALL include a nullable `scope` field on role assignments to support it without schema migration.
3. THE **architecture** SHALL NOT hardcode any assumption that permissions are always global.

---

## Sección 7: Auditoría de Seguridad

### Requisito 7: Registro de Operaciones de Seguridad

**Historia de Usuario:** Como auditor de seguridad, quiero un registro inmutable de todas las operaciones sensibles de seguridad, para cumplir con requisitos de compliance y detectar anomalías.

#### Criterios de Aceptación

1. THE **Audit Service** SHALL record the following security events: User Login, User Logout, Failed Login Attempt, Role Assignment, Permission Change, User Enable/Disable, Job Execution, Job Cancellation, Configuration Change, API Credential creation/deletion.
2. EACH audit entry SHALL include: `userId`, `userName`, `timestamp`, `action`, `resource`, `resourceId`, `result` (Success/Failure), `ipAddress` (when available), `correlationId`.
3. Audit logs SHALL be immutable — no update or delete operations are allowed on audit entries.
4. THE **Audit Log** SHALL be queryable by administrators via `GET /api/v1/audit?userId=&action=&from=&to=`.
5. Audit entries SHALL be retained for a configurable period (default: 12 months).

---

## Sección 8: Extensibilidad de Proveedores de Autenticación

### Requisito 8: Independencia del Auth Provider

**Historia de Usuario:** Como arquitecto, quiero que el sistema de autorización funcione con cualquier proveedor de identidad, para no quedar atado a ninguna tecnología específica.

#### Criterios de Aceptación

1. THE **authorization system** SHALL be completely independent of the authentication provider.
2. THE **system** SHALL support the following auth providers without modifying authorization logic: Local, Microsoft Entra ID, LDAP/Active Directory, Okta, Auth0, Google Workspace, Azure B2C.
3. THE **IAuthenticationProvider** interface SHALL remain the only coupling point between auth and authz.
4. WHEN a new auth provider is added, THE **authorization system** SHALL require zero changes.

---

## Requisitos No Funcionales

### NFR-01: Performance
- Permission resolution SHALL be cached per user session (in-memory or Redis)
- Cache invalidation SHALL occur within 5 seconds of a role change
- Authorization checks SHALL add < 5ms overhead per request

### NFR-02: Security
- Permission checks SHALL occur server-side on every protected endpoint
- A disabled user SHALL be rejected even with a valid JWT
- Role assignments SHALL require `Users.Update` permission to modify

### NFR-03: Testability
- The `AuthorizationService` SHALL have ≥ 80% unit test coverage
- Permission resolution SHALL be testable without a database (in-memory role store)

### NFR-04: Auditability
- All permission changes SHALL be recorded in the audit log
- Audit entries SHALL be queryable without admin role (internal system access only)
