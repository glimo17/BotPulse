# ADR-017: Identity, Authentication & Authorization Architecture

## Status
Proposed

**Target Implementation:** Phase 2 - Enterprise Auth + RBAC

**Related ADRs:** ADR-016 (Intelligence Platform consumes the authorization context defined here)

---

## Context

BotPulse requiere una arquitectura de seguridad enterprise-ready que soporte:
1. Gestión de usuarios locales (MVP) sin dependencia de IdPs externos
2. Evolución futura hacia proveedores enterprise (Entra ID, LDAP, Okta, Auth0)
3. Control de acceso granular basado en roles (RBAC)
4. Autorización independiente del mecanismo de autenticación
5. Contexto de autorización propagado a todos los módulos (incluyendo Intelligence Platform)

La arquitectura actual tiene autorización básica (3 roles hardcodeados) sin separación clara entre identity management, authentication, y authorization.

### Problema Arquitectónico

¿Cómo diseñamos una arquitectura de seguridad que:
- Soporte usuarios locales inicialmente sin asumir infraestructura enterprise
- Permita integración futura con IdPs externos sin reescribir la autorización
- Propague contexto de autorización a todos los módulos (Core, Intelligence Platform)
- Soporte multi-tenancy futuro sin cambios arquitectónicos

La plataforma NO debe asumir:
- Active Directory
- Entra ID
- LDAP
- Ningún dominio corporativo
- Proveedor de identidad cloud externo

Esas integraciones son **proveedores de autenticación futuros**, no la fundación de la plataforma.

---

## Decision

Se introduce una arquitectura de seguridad en **tres capas claramente separadas**: Identity, Authentication, y Authorization.

### Architecture Overview

```
┌──────────────────────────────────────────────────────┐
│              Identity Layer                          │
│  BotPulse.Identity                                   │
│                                                      │
│  - Local user creation & lifecycle                   │
│  - User profile management                           │
│  - Password credential storage (Argon2id)            │
│  - User activation / deactivation                    │
│  - Organization association (multi-tenant future)    │
└─────────────────────┬────────────────────────────────┘
                      │ IUserRepository
                      │ IUserManagementService
┌─────────────────────▼────────────────────────────────┐
│           Authentication Layer                       │
│  "Who are you?"                                      │
│                                                      │
│  IAuthenticationProvider                             │
│    ├── LocalAuthenticationProvider       (MVP)       │
│    ├── EntraIdAuthenticationProvider     (future)    │
│    ├── LdapAuthenticationProvider        (future)    │
│    └── OktaAuthenticationProvider        (future)    │
└─────────────────────┬────────────────────────────────┘
                      │ ClaimsPrincipal
                      │ User Context
┌─────────────────────▼────────────────────────────────┐
│         Authorization Layer (RBAC)                   │
│  "What can you do?"                                  │
│                                                      │
│  BotPulse.Authorization                              │
│    - Permission Catalog (23 permissions)             │
│    - Role Management (System + Custom roles)         │
│    - Permission Cache (< 5s invalidation)            │
│    - IAuthorizationService                           │
│    - IPermissionEvaluator                            │
│    - IAuthorizationContextAccessor                   │
└──────────┬──────────────────────────┬────────────────┘
           │ AuthorizationContext     │ AuthorizationContext
           │                          │
┌──────────▼──────────┐   ┌──────────▼──────────────────┐
│   BotPulse Core     │   │  BotPulse Intelligence      │
│                     │   │  Platform  (ADR-016)        │
└─────────────────────┘   └─────────────────────────────┘
```

---

### Layer 1: Identity Management

Se introduce `BotPulse.Identity` — módulo responsable de gestionar usuarios de la plataforma.

**Responsibilities:**
- Local user creation (username, email, hashed password)
- User profile management (name, preferences)
- Password credential storage (Argon2id hashing)
- User activation / deactivation (soft delete)
- User lifecycle management
- Organization association (future multi-tenant support)

**Key Interfaces:**

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId);
    Task<User?> GetByUsernameAsync(string username);
    Task<User?> GetByEmailAsync(string email);
    Task<IEnumerable<User>> FindAllAsync(UserQueryOptions options);
    Task AddAsync(User user);
    void Update(User user);
    void Remove(User user);
}

public interface IUserManagementService
{
    Task<User> CreateLocalUserAsync(string username, string email, string password);
    Task<bool> ValidatePasswordAsync(Guid userId, string password);
    Task ResetPasswordAsync(Guid userId, string newPassword);
    Task ActivateUserAsync(Guid userId);
    Task DeactivateUserAsync(Guid userId);
}
```

**Evolution Path:**
```
Local Users (MVP)
      │
      ├──► Entra ID  (external users, password_hash = NULL)
      ├──► LDAP      (external users, password_hash = NULL)
      └──► Okta/Auth0 (external users, password_hash = NULL)
```

---

### Layer 2: Authentication

Mantiene el patrón `IAuthenticationProvider` existente, con local authentication como fundación.

```csharp
public interface IAuthenticationProvider
{
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request);
    Task<ClaimsPrincipal> GetUserClaimsAsync(Guid userId);
    string ProviderName { get; }
}
```

**Implementations:**
- **LocalAuthenticationProvider (MVP):** Valida username/password contra tabla `users`, verifica hash Argon2id, genera `ClaimsPrincipal` con userId/username/email.
- **EntraIdAuthenticationProvider (Future):** Valida token con Microsoft Entra ID, sincroniza perfil en tabla `users` (password_hash = NULL).
- **LdapAuthenticationProvider (Future):** Valida credenciales contra LDAP, sincroniza perfil en tabla `users` (password_hash = NULL).

**Authentication Flow:**
```
1. User submits credentials
2. IAuthenticationProvider validates (local or external)
3. Generate ClaimsPrincipal with userId
4. Pass userId to Authorization Layer
5. Authorization Layer resolves permissions → AuthorizationContext
```

---

### Layer 3: Authorization (RBAC)

Authorization es completamente independiente de authentication. Funciona igual sin importar si el usuario se autenticó vía local, Entra ID, o LDAP.

**Permission Catalog — 23 permisos granulares:**

| Area | Permissions |
|------|------------|
| Dashboard | `Dashboard.View`, `Dashboard.Export` |
| Jobs | `Jobs.View`, `Jobs.Execute`, `Jobs.Cancel`, `Jobs.Retry` |
| Robots | `Robots.View`, `Robots.Restart` |
| Queues | `Queues.View` |
| Logs | `Logs.View` |
| Machines | `Machines.View` |
| Assets | `Assets.View` |
| Metrics | `Metrics.View` |
| Alerts | `Alerts.View`, `Alerts.Acknowledge` |
| Users | `Users.View`, `Users.Create`, `Users.Update`, `Users.Delete` |
| Roles | `Roles.View`, `Roles.Update` |
| Settings | `Settings.View`, `Settings.Edit` |
| Integrations | `Integrations.Configure` |

**Role Management:**
- **System Roles** (no eliminables): Administrator (all), Operations Manager, Viewer
- **Custom Roles**: ilimitados, creados por administradores con permisos arbitrarios

**Key Interfaces:**

```csharp
public interface IAuthorizationService
{
    Task<IEnumerable<string>> GetPermissionsAsync(Guid userId);
    Task<bool> HasPermissionAsync(Guid userId, string permission);
    Task<AuthorizationContext> GetAuthorizationContextAsync(Guid userId);
}

public interface IPermissionEvaluator
{
    bool Evaluate(AuthorizationContext context, string requiredPermission);
}

public interface IAuthorizationContextAccessor
{
    AuthorizationContext? Current { get; }
}

public class AuthorizationContext
{
    public Guid UserId { get; init; }
    public Guid? OrganizationId { get; init; }   // Multi-tenant future
    public string? TenantId { get; init; }        // Multi-tenant future
    public IReadOnlyList<string> Permissions { get; init; }
    public IReadOnlyList<string> Roles { get; init; }
}
```

**Authorization Context Propagation:**

El `AuthorizationContext` se propaga a todos los módulos mediante:
- **ASP.NET Core Middleware:** `AuthorizationContextMiddleware` enriquece `HttpContext`
- **DI Scoped Service:** `IAuthorizationContextAccessor` accesible en toda la aplicación
- **Intelligence Platform:** Agents y Tools reciben el contexto para tenant isolation y permission checks

---

## Architecture Drivers

### Identity Provider Independence
La autorización es completamente independiente del mecanismo de autenticación. Cambiar de LDAP a Entra ID no afecta los roles ni los permisos.

### Enterprise Scalability
La arquitectura soporta miles de usuarios, cientos de roles, permisos granulares, roles personalizados por organización, y crecimiento hacia SaaS Multi-Tenant.

### Extensibility
- Nuevos permisos sin modificar la arquitectura
- Nuevos auth providers sin afectar la autorización
- Evolución hacia Folder-Level Security (`Scope` nullable en UserRoles)
- Evolución hacia ABAC sin romper el modelo RBAC

### Security
- Principle of Least Privilege
- Fail-Closed Authorization
- Explicit Permissions (deny by default)
- Auditoría completa de todas las operaciones sensibles

### Performance
- Permission cache por sesión (in-memory, < 5s invalidation)
- Cache invalidation inmediata cuando se modifican roles del usuario

---

## Alternatives Considered

### Alternative 1: Asumir Active Directory/Entra ID desde el inicio
**Rechazado:** Limita adopción. Local authentication es fundación, enterprise IdPs son extensión.

### Alternative 2: No separar Identity y Authentication
**Rechazado:** Dificulta evolución a IdPs externos. Con separación clara, agregar Entra ID no modifica user management.

### Alternative 3: Autorización basada en claims JWT
**Rechazado:** No soporta cambios de permisos en tiempo real. RBAC dinámico requiere resolución server-side.

### Alternative 4: Sistema externo de autorización (OPA, Casbin)
**Rechazado:** Over-engineering para el MVP. RBAC interno es suficiente y más simple de operar.

---

## Consequences

### Positivas
- **Local-first:** Plataforma funcional sin infraestructura enterprise
- **Enterprise-ready:** Evolución clara hacia Entra ID, LDAP, Okta sin reescribir autorización
- **Authorization Independence:** Cambiar auth provider no afecta RBAC
- **Context Propagation:** Intelligence Platform y Core comparten `AuthorizationContext` sin duplicación
- **Multi-tenant Ready:** Arquitectura soporta tenant isolation desde el diseño
- **Auditability:** Todas las operaciones de seguridad auditables por usuario

### Negativas
- **Complejidad Inicial:** Tres capas (Identity, Auth, Authz) en lugar de una
- **Overhead de Contexto:** Propagar `AuthorizationContext` agrega latencia mínima (mitigado con cache)
- **Migración:** Usuarios existentes con rol string requieren migración a nueva estructura

### Mitigaciones
- MVP con `LocalAuthenticationProvider` simplifica despliegue inicial
- Permission cache (< 5s) reduce overhead de authorization checks
- Migration script automático para usuarios existentes

---

## Dependencies

- **PostgreSQL** para persistencia de users, roles, user_roles
- **ADR-016 (Intelligence Platform)** consume `AuthorizationContext` de este ADR
- **Future (Multi-Tenant):** Requiere `organization_id` y `tenant_id` en `AuthorizationContext`

---

## References

- Clean Architecture by Robert C. Martin
- Microsoft Identity Platform Documentation
- RBAC Design Patterns
- ASP.NET Core Authorization
- OWASP Authentication Cheat Sheet
