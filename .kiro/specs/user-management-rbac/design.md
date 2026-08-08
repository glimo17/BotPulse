# Design — User Management & Role-Based Access Control (RBAC)

## Status
In Progress (Phase 2)

---

## 1. Architecture Overview

This design implements ADR-017: three clearly separated layers — Identity, Authentication, and Authorization — ensuring the RBAC engine works independently of the authentication provider.

```
┌──────────────────────────────────────────────────────┐
│              Identity Layer                          │
│  BotPulse.Authorization (identity concerns)          │
│  - User lifecycle management                         │
│  - Local credential storage (Argon2id)               │
│  - User activation / deactivation                    │
└─────────────────────┬────────────────────────────────┘
                      │ IUserRepository
┌─────────────────────▼────────────────────────────────┐
│           Authentication Layer                       │
│  BotPulse.Api (middleware + JWT issuance)            │
│  IAuthenticationProvider                             │
│    ├── LocalAuthenticationProvider  (MVP)            │
│    └── EntraId / LDAP / Okta        (future)         │
└─────────────────────┬────────────────────────────────┘
                      │ ClaimsPrincipal (userId)
┌─────────────────────▼────────────────────────────────┐
│         Authorization / RBAC Layer                   │
│  BotPulse.Authorization                              │
│  IAuthorizationService → AuthorizationContext        │
│  IAuthorizationContextAccessor (scoped DI)           │
└──────────┬──────────────────────────┬────────────────┘
           │ AuthorizationContext     │ AuthorizationContext
┌──────────▼──────────┐   ┌──────────▼──────────────────┐
│   BotPulse Core     │   │  BotPulse Intelligence      │
│   API Endpoints     │   │  Platform  (future Phase 3) │
└─────────────────────┘   └─────────────────────────────┘
```

---

## 2. Project Structure

The authorization concerns are consolidated in `BotPulse.Authorization`. Infrastructure implementations live in `BotPulse.Infrastructure`.

```
src/
├── BotPulse.Authorization/           # Domain contracts + entities
│   ├── Entities/
│   │   ├── Role.cs                   # Role domain entity
│   │   └── UserRole.cs               # User-Role assignment
│   ├── Permissions/
│   │   └── PermissionCatalog.cs      # 23 permission constants
│   ├── Repositories/
│   │   ├── IRoleRepository.cs        # Role persistence contract
│   │   └── IUserRoleRepository.cs    # User-Role assignment contract
│   ├── Services/
│   │   └── RbacAuthorizationService.cs
│   ├── IAuthorizationService.cs      # Main authorization contract
│   ├── IAuthorizationContextAccessor.cs
│   └── AuthorizationContext.cs       # Authorization context model
│
├── BotPulse.Infrastructure/
│   └── Persistence/
│       ├── Entities/
│       │   ├── Role.cs               # EF Core entity
│       │   ├── RolePermissionEntry.cs
│       │   └── UserRole.cs           # EF Core junction entity
│       ├── Repositories/
│       │   ├── RoleRepository.cs     # IRoleRepository implementation
│       │   └── UserRoleRepository.cs # IUserRoleRepository implementation
│       └── Migrations/
│           └── YYYYMMDD_AddRbac.cs   # Migration: roles + user_roles tables
│
├── BotPulse.Api/
│   ├── Controllers/V1/
│   │   ├── RolesController.cs        # Role CRUD endpoints
│   │   └── UsersController.cs        # User management endpoints
│   └── Middleware/
│       └── AuthorizationContextMiddleware.cs
│
└── ui/src/
    ├── pages/
    │   ├── AdminUsers.tsx            # User management page
    │   └── AdminRoles.tsx            # Role management page
    └── components/
        └── PermissionGate.tsx        # Permission-based UI guard
```

---

## 3. Domain Entities

### Role

```csharp
namespace BotPulse.Authorization.Entities;

public sealed class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
```

### UserRole

```csharp
namespace BotPulse.Authorization.Entities;

public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Scope { get; set; }  // Future: folder-level security
}
```

### AuthorizationContext

```csharp
namespace BotPulse.Authorization;

public sealed class AuthorizationContext
{
    public Guid UserId { get; init; }
    public Guid? OrganizationId { get; init; }   // Future: multi-tenant
    public string? TenantId { get; init; }        // Future: multi-tenant
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
}
```

---

## 4. Permission Catalog

Full 23-permission catalog replacing the current partial implementation:

```csharp
namespace BotPulse.Authorization.Permissions;

public static class PermissionCatalog
{
    // Dashboard
    public const string DashboardView   = "Dashboard.View";
    public const string DashboardExport = "Dashboard.Export";

    // Jobs
    public const string JobsView    = "Jobs.View";
    public const string JobsExecute = "Jobs.Execute";
    public const string JobsCancel  = "Jobs.Cancel";
    public const string JobsRetry   = "Jobs.Retry";

    // Robots
    public const string RobotsView    = "Robots.View";
    public const string RobotsRestart = "Robots.Restart";

    // Queues
    public const string QueuesView = "Queues.View";

    // Logs
    public const string LogsView = "Logs.View";

    // Machines
    public const string MachinesView = "Machines.View";

    // Assets
    public const string AssetsView = "Assets.View";

    // Metrics
    public const string MetricsView = "Metrics.View";

    // Alerts
    public const string AlertsView        = "Alerts.View";
    public const string AlertsAcknowledge = "Alerts.Acknowledge";

    // Users
    public const string UsersView   = "Users.View";
    public const string UsersCreate = "Users.Create";
    public const string UsersUpdate = "Users.Update";
    public const string UsersDelete = "Users.Delete";

    // Roles
    public const string RolesView   = "Roles.View";
    public const string RolesUpdate = "Roles.Update";

    // Settings
    public const string SettingsView = "Settings.View";
    public const string SettingsEdit = "Settings.Edit";

    // Integrations
    public const string IntegrationsConfigure = "Integrations.Configure";

    // All permissions (for Administrator seeding)
    public static readonly IReadOnlyList<string> All = new[]
    {
        DashboardView, DashboardExport,
        JobsView, JobsExecute, JobsCancel, JobsRetry,
        RobotsView, RobotsRestart,
        QueuesView,
        LogsView,
        MachinesView,
        AssetsView,
        MetricsView,
        AlertsView, AlertsAcknowledge,
        UsersView, UsersCreate, UsersUpdate, UsersDelete,
        RolesView, RolesUpdate,
        SettingsView, SettingsEdit,
        IntegrationsConfigure
    };
}
```

---

## 5. Key Interfaces

### IAuthorizationService (expanded)

```csharp
namespace BotPulse.Authorization;

public interface IAuthorizationService
{
    Task<IEnumerable<string>> GetPermissionsAsync(Guid userId);
    Task<bool> HasPermissionAsync(Guid userId, string permission);
    Task<AuthorizationContext> GetAuthorizationContextAsync(Guid userId);
    Task InvalidateCacheAsync(Guid userId);
}
```

### IAuthorizationContextAccessor

```csharp
namespace BotPulse.Authorization;

public interface IAuthorizationContextAccessor
{
    AuthorizationContext? Current { get; set; }
}
```

### IRoleRepository (current — already implemented)

```csharp
public interface IRoleRepository
{
    Task<IReadOnlyCollection<Role>> GetAllAsync();
    Task<Role?> GetByIdAsync(Guid id);
    Task<Role?> GetByNameAsync(string name);
    Task<IReadOnlyCollection<string>> GetPermissionsForUserAsync(Guid userId);
    Task CreateAsync(Role role);
    Task UpdateAsync(Role role);
    Task DeleteAsync(Guid id);
}
```

### IUserRoleRepository (new)

```csharp
namespace BotPulse.Authorization.Repositories;

public interface IUserRoleRepository
{
    Task<IReadOnlyCollection<Role>> GetRolesByUserIdAsync(Guid userId);
    Task AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedByUserId = null, string? scope = null);
    Task RemoveRoleAsync(Guid userId, Guid roleId);
    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId);
}
```

---

## 6. Database Schema

### New tables

```sql
-- roles table (already partially exists, needs Description + timestamps)
CREATE TABLE roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) UNIQUE NOT NULL,
    description TEXT,
    is_system_role BOOLEAN NOT NULL DEFAULT FALSE,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- role_permissions (already exists)
CREATE TABLE role_permissions (
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission VARCHAR(255) NOT NULL,
    PRIMARY KEY (role_id, permission)
);

-- user_roles junction table (new)
CREATE TABLE user_roles (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    assigned_by_user_id UUID REFERENCES users(id),
    assigned_at_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    scope VARCHAR(255),  -- Future: folder-level security
    PRIMARY KEY (user_id, role_id)
);
```

### Migration strategy

The existing `role` string column on the `users` table must be migrated:

```
Up Migration:
  1. Create user_roles table
  2. Seed 3 system roles (Administrator, Operations Manager, Viewer) with full permission sets
  3. INSERT INTO user_roles SELECT id, <role_id> FROM users WHERE role = '<role_name>'
  4. DROP COLUMN role FROM users

Down Migration:
  1. ALTER TABLE users ADD COLUMN role VARCHAR(50)
  2. UPDATE users SET role = (SELECT r.name FROM roles r JOIN user_roles ur ON ur.role_id = r.id WHERE ur.user_id = users.id LIMIT 1)
  3. DROP TABLE user_roles
```

### System Role Seeds

| Role | Permissions |
|------|------------|
| **Administrator** | All 23 permissions |
| **Operations Manager** | Dashboard.View, Robots.View, Queues.View, Jobs.View, Jobs.Execute, Jobs.Cancel, Jobs.Retry, Logs.View, Machines.View, Assets.View, Metrics.View, Alerts.View, Alerts.Acknowledge |
| **Viewer** | Dashboard.View, Robots.View, Queues.View, Jobs.View, Metrics.View, Logs.View, Machines.View, Assets.View, Alerts.View |

---

## 7. RbacAuthorizationService (expanded)

```csharp
namespace BotPulse.Authorization.Services;

public sealed class RbacAuthorizationService : IAuthorizationService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(300); // 5 min sliding

    public async Task<IEnumerable<string>> GetPermissionsAsync(Guid userId)
    {
        var cacheKey = $"permissions:{userId}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cached))
            return cached!;

        var permissions = await _roleRepository.GetPermissionsForUserAsync(userId);
        _cache.Set(cacheKey, permissions, new MemoryCacheEntryOptions
        {
            SlidingExpiration = CacheDuration
        });
        return permissions;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var permissions = await GetPermissionsAsync(userId);
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<AuthorizationContext> GetAuthorizationContextAsync(Guid userId)
    {
        var permissions = await GetPermissionsAsync(userId);
        var roles = await _roleRepository.GetRolesByUserIdAsync(userId); // new method needed
        return new AuthorizationContext
        {
            UserId = userId,
            Permissions = permissions.ToList(),
            Roles = roles.Select(r => r.Name).ToList()
        };
    }

    public Task InvalidateCacheAsync(Guid userId)
    {
        _cache.Remove($"permissions:{userId}");
        return Task.CompletedTask;
    }
}
```

---

## 8. API Endpoints

### Roles

| Method | Endpoint | Permission | Description |
|--------|----------|-----------|-------------|
| GET | `/api/v1/roles` | `Roles.View` | List all roles |
| GET | `/api/v1/roles/{id}` | `Roles.View` | Get role by ID |
| POST | `/api/v1/roles` | `Roles.Update` | Create custom role |
| PUT | `/api/v1/roles/{id}` | `Roles.Update` | Update custom role |
| DELETE | `/api/v1/roles/{id}` | `Roles.Update` | Delete custom role (if no users assigned) |

### Users

| Method | Endpoint | Permission | Description |
|--------|----------|-----------|-------------|
| GET | `/api/v1/users` | `Users.View` | List users with roles + last login |
| GET | `/api/v1/users/{id}` | `Users.View` | Get user detail |
| POST | `/api/v1/users` | `Users.Create` | Create local user |
| PUT | `/api/v1/users/{id}` | `Users.Update` | Update user profile |
| PUT | `/api/v1/users/{id}/roles` | `Users.Update` | Assign roles to user |
| PUT | `/api/v1/users/{id}/enable` | `Users.Update` | Enable user account |
| PUT | `/api/v1/users/{id}/disable` | `Users.Update` | Disable user account |
| DELETE | `/api/v1/users/{id}` | `Users.Delete` | Delete user |

### Permission enforcement

Each controller uses `IAuthorizationService` via a custom `[RequirePermission]` attribute or inline check:

```csharp
[HttpDelete("{id:guid}")]
public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
{
    var existing = await _roleRepository.GetByIdAsync(id);
    if (existing is null) return NotFound();

    // System roles cannot be deleted
    if (existing.IsSystemRole)
        return Conflict(new { error = "System roles cannot be deleted." });

    // Check if users are assigned to this role
    var hasUsers = await _userRoleRepository.RoleHasUsersAsync(id);
    if (hasUsers)
        return Conflict(new { error = "Cannot delete role with assigned users." });

    await _roleRepository.DeleteAsync(id);
    return NoContent();
}
```

---

## 9. Authorization Middleware

```csharp
namespace BotPulse.Api.Middleware;

public sealed class AuthorizationContextMiddleware
{
    private readonly RequestDelegate _next;

    public async Task InvokeAsync(
        HttpContext context,
        IAuthorizationService authorizationService,
        IAuthorizationContextAccessor contextAccessor)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var authContext = await authorizationService.GetAuthorizationContextAsync(userId);
            contextAccessor.Current = authContext;
        }
        await _next(context);
    }
}
```

---

## 10. Frontend Components

### AdminUsers page

- Table: username, email, roles (badges), status (active/disabled), last login
- Actions: assign roles, enable/disable, reset password (local users only)
- Filter by role, status

### AdminRoles page (already scaffolded — needs create/edit/delete)

- List roles with permission count
- Create custom role modal with permission selector
- Edit permissions for custom roles
- Delete custom role (blocked if users assigned)
- System roles shown as read-only

### PermissionGate component

```tsx
interface PermissionGateProps {
  permission: string
  children: React.ReactNode
  fallback?: React.ReactNode
}

export function PermissionGate({ permission, children, fallback = null }: PermissionGateProps) {
  const { user } = useAuth()
  const hasPermission = user?.permissions?.includes(permission) ?? false
  return hasPermission ? <>{children}</> : <>{fallback}</>
}
```

### AuthContext — permissions support

The existing `AuthContext` returns `roles[]` from JWT. It needs to be extended to also surface `permissions[]` so `PermissionGate` can work client-side (as UX optimization only — enforcement is server-side):

```tsx
interface User {
  userId: string
  userName: string
  email: string
  roles: string[]
  permissions: string[]  // Added
}
```

---

## 11. What Already Exists vs What's Missing

### Already implemented ✅
- `BotPulse.Authorization` project structure
- `Role` entity (needs Description + timestamps)
- `IRoleRepository` interface
- `RoleRepository` implementation (Infrastructure)
- `RbacAuthorizationService` (partial — needs cache + context)
- `RolesController` (CRUD — needs permission checks + system role guard)
- `AdminRoles.tsx` (list view — needs create/edit/delete)
- `AuthContext.tsx` (needs permissions support)
- Migration `SeedDefaultRoles` (partial — wrong permission names, missing Operations Manager permissions)

### Missing / needs update 🔧
- `UserRole` entity + `IUserRoleRepository` + `UserRoleRepository`
- `AuthorizationContext` class
- `IAuthorizationContextAccessor` interface + implementation
- `AuthorizationContextMiddleware`
- Full 23-permission `PermissionCatalog` (current has only 4)
- `UsersController` (user management endpoints)
- `AdminUsers.tsx` (user management page)
- `PermissionGate.tsx` component
- EF Core migration `AddRbac` (user_roles table + full permission seeding + user migration from role string)
- Permission-based policy registration in DI
- `IUserRoleRepository` methods on `RolesController.Delete` (guard against assigned users)

---

## 12. Non-Functional Design Decisions

| Concern | Decision |
|---------|---------|
| **Permission Cache** | In-memory `IMemoryCache`, 5-minute sliding expiration per userId |
| **Cache Invalidation** | `InvalidateCacheAsync(userId)` called on any role assignment change |
| **System Role Protection** | `IsSystemRole = true` → reject delete + reject permission modification |
| **Disabled User** | JWT middleware checks `is_active` on every request; returns 401 if false |
| **Folder-Level Security** | `Scope` column nullable on `user_roles` — not enforced in MVP |
| **Client-Side Guards** | `PermissionGate` hides UI elements as UX optimization — NOT security boundary |
| **Audit** | Role assignment/removal + user enable/disable logged to audit table (Phase 2B) |
