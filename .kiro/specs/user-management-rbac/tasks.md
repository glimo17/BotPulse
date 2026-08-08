# Tasks — User Management & Role-Based Access Control (RBAC)

## Status
Ready for Implementation

---

## Task Dependency Graph

```
M1: Domain Foundation
  T1.1 PermissionCatalog (23 permissions)
  T1.2 Role entity (add Description + timestamps)
  T1.3 UserRole entity (new)
  T1.4 AuthorizationContext + IAuthorizationContextAccessor
  T1.5 IUserRoleRepository interface
    └── depends on T1.3

M2: Infrastructure & Database
  T2.1 UserRoleRepository implementation
    └── depends on T1.5
  T2.2 EF Core migration AddRbac
    └── depends on T1.2, T1.3
  T2.3 Role seeding (3 system roles, full permission sets)
    └── depends on T1.1, T2.2
  T2.4 User migration (role string → user_roles)
    └── depends on T2.3

M3: Authorization Service
  T3.1 RbacAuthorizationService (expand: cache + context + invalidation)
    └── depends on T1.1, T1.4, T2.1
  T3.2 AuthorizationContextMiddleware
    └── depends on T1.4, T3.1
  T3.3 Permission policies registration in DI
    └── depends on T1.1, T3.1

M4: API Layer
  T4.1 RolesController (add permission guards + system role protection)
    └── depends on T3.3, T2.1
  T4.2 UsersController (user management endpoints)
    └── depends on T3.3, T2.1
  T4.3 Auth/me endpoint (return permissions[])
    └── depends on T3.1

M5: Frontend
  T5.1 AuthContext (add permissions[] support)
    └── depends on T4.3
  T5.2 PermissionGate component
    └── depends on T5.1
  T5.3 AdminRoles page (create/edit/delete)
    └── depends on T4.1, T5.2
  T5.4 AdminUsers page (user management)
    └── depends on T4.2, T5.2
  T5.5 Sidebar/navigation (admin section guard)
    └── depends on T5.1
```

---

## Milestone 1 — Domain Foundation

**Goal:** Establish the domain contracts and entities that everything else builds on. No infrastructure dependencies.

### T1.1 — Expand PermissionCatalog to 23 permissions

**File:** `src/BotPulse.Authorization/Permissions/PermissionCatalog.cs`

Replace the current partial `Permissions.cs` (4 permissions) with the full catalog:

- Rename file to `PermissionCatalog.cs`, class to `PermissionCatalog`
- Add all 23 permissions as `public const string` following `Area.Action` naming
- Add `public static readonly IReadOnlyList<string> All` with all permissions (for seeding Administrator role)
- Remove obsolete `UsersManage` constant — replace with `Users.View`, `Users.Create`, `Users.Update`, `Users.Delete`

**Acceptance criteria:**
- All 23 constants defined and compile
- `PermissionCatalog.All` contains exactly 23 entries
- Old `Permissions.cs` removed or replaced

---

### T1.2 — Update Role entity

**File:** `src/BotPulse.Authorization/Entities/Role.cs`

Add missing properties to align with design:

- Add `string Description { get; set; } = string.Empty`
- Add `DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow`
- Add `DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow`
- Change class to `sealed`

---

### T1.3 — Create UserRole entity

**File:** `src/BotPulse.Authorization/Entities/UserRole.cs`

New entity for the many-to-many user-role assignment:

```csharp
public sealed class UserRole
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public Guid? AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Scope { get; set; }  // Future: folder-level security
}
```

---

### T1.4 — Create AuthorizationContext and IAuthorizationContextAccessor

**Files:**
- `src/BotPulse.Authorization/AuthorizationContext.cs`
- `src/BotPulse.Authorization/IAuthorizationContextAccessor.cs`

`AuthorizationContext`:
- `Guid UserId`
- `Guid? OrganizationId` (future multi-tenant)
- `string? TenantId` (future multi-tenant)
- `IReadOnlyList<string> Permissions`
- `IReadOnlyList<string> Roles`
- `bool HasPermission(string permission)` helper method

`IAuthorizationContextAccessor`:
- `AuthorizationContext? Current { get; set; }`
- Implementation: `HttpContextAuthorizationContextAccessor` (backed by `IHttpContextAccessor`)

---

### T1.5 — Create IUserRoleRepository

**File:** `src/BotPulse.Authorization/Repositories/IUserRoleRepository.cs`

```csharp
public interface IUserRoleRepository
{
    Task<IReadOnlyCollection<Role>> GetRolesByUserIdAsync(Guid userId);
    Task AssignRoleAsync(Guid userId, Guid roleId, Guid? assignedByUserId = null, string? scope = null);
    Task RemoveRoleAsync(Guid userId, Guid roleId);
    Task<bool> UserHasRoleAsync(Guid userId, Guid roleId);
    Task<bool> RoleHasUsersAsync(Guid roleId);
}
```

Also add `GetRolesByUserIdAsync(Guid userId)` to `IRoleRepository` to support context resolution.

---

## Milestone 2 — Infrastructure & Database

**Goal:** Persist the domain to PostgreSQL. Zero application logic here — only data access.

### T2.1 — Implement UserRoleRepository

**File:** `src/BotPulse.Infrastructure/Persistence/Repositories/UserRoleRepository.cs`

Implement `IUserRoleRepository` using EF Core:
- `GetRolesByUserIdAsync`: JOIN user_roles → roles → role_permissions
- `AssignRoleAsync`: INSERT into user_roles, catch duplicate key gracefully
- `RemoveRoleAsync`: DELETE from user_roles WHERE user_id AND role_id
- `UserHasRoleAsync`: EXISTS check
- `RoleHasUsersAsync`: EXISTS check (used before delete)

Add `DbSet<UserRole> UserRoles` to `BotPulseDbContext` if not present.

---

### T2.2 — EF Core migration AddRbac

**File:** `src/BotPulse.Infrastructure/Migrations/YYYYMMDD_AddRbac.cs`

`Up()`:
1. Add `description` and timestamp columns to `roles` table
2. Create `user_roles` table with FK constraints and nullable `scope` column
3. Create indexes: `user_roles(user_id)`, `user_roles(role_id)`

`Down()`:
1. Drop `user_roles` table
2. Remove added columns from `roles`

> Note: Do NOT include seeding in this migration — seeding is T2.3.

---

### T2.3 — Role seeding migration

**File:** `src/BotPulse.Infrastructure/Migrations/YYYYMMDD_SeedDefaultRoles.cs`

Replace the existing partial `SeedDefaultRoles` migration with a complete one:

`Up()`:
- Seed `Administrator` role with all 23 permissions from `PermissionCatalog.All`
- Seed `Operations Manager` role with 13 permissions (see design.md Section 6)
- Seed `Viewer` role with 9 permissions
- Use fixed deterministic GUIDs for system roles

`Down()`:
- Delete all seeded role_permissions
- Delete the 3 system roles

---

### T2.4 — User migration (role string → user_roles)

**File:** `src/BotPulse.Infrastructure/Migrations/YYYYMMDD_MigrateUserRoles.cs`

`Up()`:
```sql
INSERT INTO user_roles (user_id, role_id, assigned_at_utc)
SELECT u.id,
       r.id,
       NOW()
FROM users u
JOIN roles r ON r.name = u.role
WHERE u.role IS NOT NULL;

ALTER TABLE users DROP COLUMN role;
```

`Down()`:
```sql
ALTER TABLE users ADD COLUMN role VARCHAR(50);
UPDATE users SET role = (
    SELECT r.name FROM roles r
    JOIN user_roles ur ON ur.role_id = r.id
    WHERE ur.user_id = users.id
    LIMIT 1
);
```

---

## Milestone 3 — Authorization Service

**Goal:** Wire up the RBAC engine with caching and context propagation.

### T3.1 — Expand RbacAuthorizationService

**File:** `src/BotPulse.Authorization/Services/RbacAuthorizationService.cs`

Expand the existing partial implementation:

- Inject `IMemoryCache` for permission caching (5-minute sliding expiration)
- Implement `HasPermissionAsync(Guid userId, string permission)`
- Implement `GetAuthorizationContextAsync(Guid userId)` — resolves permissions + role names
- Implement `InvalidateCacheAsync(Guid userId)` — call on any role assignment change
- Update `IAuthorizationService` interface to add new methods

---

### T3.2 — AuthorizationContextMiddleware

**File:** `src/BotPulse.Api/Middleware/AuthorizationContextMiddleware.cs`

ASP.NET Core middleware that runs after authentication:
- Extract `userId` from `ClaimsPrincipal`
- Call `IAuthorizationService.GetAuthorizationContextAsync(userId)`
- Set result on `IAuthorizationContextAccessor.Current`
- Skip if request is not authenticated

Register in `Program.cs` after `UseAuthentication()` and before `UseAuthorization()`.

---

### T3.3 — Permission policies registration

**File:** `src/BotPulse.Api/Extensions/AuthorizationExtensions.cs`

Register one ASP.NET Core policy per permission constant:

```csharp
foreach (var permission in PermissionCatalog.All)
{
    options.AddPolicy(permission, policy =>
        policy.Requirements.Add(new PermissionRequirement(permission)));
}
```

Implement `PermissionRequirement` + `PermissionAuthorizationHandler` that reads from `IAuthorizationContextAccessor.Current`.

Update `RolesController` to use `[Authorize(Policy = PermissionCatalog.RolesView)]` instead of `[Authorize(Policy = "RequireAdministrator")]`.

---

## Milestone 4 — API Layer

**Goal:** Expose user and role management via REST endpoints with proper permission enforcement.

### T4.1 — Update RolesController

**File:** `src/BotPulse.Api/Controllers/V1/RolesController.cs`

Update existing controller:

- Replace `[Authorize(Policy = "RequireAdministrator")]` with per-action permission policies
  - GET actions: `Roles.View`
  - POST/PUT/DELETE actions: `Roles.Update`
- `DELETE`: add system role guard (return `409 Conflict` if `IsSystemRole = true`)
- `DELETE`: add users-assigned guard using `IUserRoleRepository.RoleHasUsersAsync`
- Add `PUT /{id}/permissions` endpoint to update permissions of custom roles only
- Return proper `400` / `409` error bodies with descriptive messages

---

### T4.2 — Create UsersController

**File:** `src/BotPulse.Api/Controllers/V1/UsersController.cs`

New controller with these endpoints:

- `GET /api/v1/users` — list users with roles + last login (`Users.View`)
- `GET /api/v1/users/{id}` — get user detail (`Users.View`)
- `POST /api/v1/users` — create local user (`Users.Create`) — local auth only
- `PUT /api/v1/users/{id}` — update profile (`Users.Update`)
- `PUT /api/v1/users/{id}/roles` — assign roles, call `InvalidateCacheAsync` after (`Users.Update`)
- `PUT /api/v1/users/{id}/enable` — set `is_active = true`, invalidate cache (`Users.Update`)
- `PUT /api/v1/users/{id}/disable` — set `is_active = false`, invalidate cache (`Users.Update`)
- `DELETE /api/v1/users/{id}` — soft delete (`Users.Delete`)

Disable user cannot disable self. Return `400` with descriptive error.

---

### T4.3 — Extend auth/me endpoint

**File:** `src/BotPulse.Api/Controllers/V1/AuthController.cs`

Update the `/api/v1/auth/me` response to include `permissions[]`:

```json
{
  "userId": "...",
  "userName": "...",
  "email": "...",
  "roles": ["Administrator"],
  "permissions": ["Dashboard.View", "Jobs.Execute", ...]
}
```

Source permissions from `IAuthorizationService.GetPermissionsAsync(userId)`.

---

## Milestone 5 — Frontend

**Goal:** User-facing admin UI for managing users and roles.

### T5.1 — Extend AuthContext with permissions

**File:** `ui/src/contexts/AuthContext.tsx`

- Add `permissions: string[]` to `User` interface
- Populate from `/api/v1/auth/me` response
- Add `hasPermission(permission: string): boolean` helper to context
- Persist permissions in memory (re-fetch on page reload via /me)

---

### T5.2 — Create PermissionGate component

**File:** `ui/src/components/PermissionGate.tsx`

```tsx
interface PermissionGateProps {
  permission: string
  children: React.ReactNode
  fallback?: React.ReactNode
}

export function PermissionGate({ permission, children, fallback = null }: PermissionGateProps) {
  const { hasPermission } = useAuth()
  return hasPermission(permission) ? <>{children}</> : <>{fallback}</>
}
```

---

### T5.3 — Upgrade AdminRoles page

**File:** `ui/src/pages/AdminRoles.tsx`

Expand the existing list-only view:

- Add **Create Role** button (visible only with `Roles.Update` permission via `PermissionGate`)
- Create role modal: name, description, permission selector (checkbox grid grouped by area)
- Edit role: same modal pre-populated (disabled for system roles)
- Delete role: confirmation dialog, show error if users assigned
- Permission count badge per role
- System roles shown with lock icon, no edit/delete actions

---

### T5.4 — Create AdminUsers page

**File:** `ui/src/pages/AdminUsers.tsx`

New page at `/admin/users`:

- Table: avatar/initials, username, email, role badges, status (active/disabled chip), last login
- Search/filter by name, role, status
- **Assign Roles** action: multi-select role picker modal, saves to `PUT /users/{id}/roles`
- **Enable / Disable** toggle with confirmation
- **Create User** button (shown only if local auth + `Users.Create` permission)
- Create user form: username, email, temporary password

---

### T5.5 — Update Sidebar and navigation guards

**File:** `ui/src/components/layout/Sidebar.tsx`

- Wrap admin nav items with `PermissionGate` using appropriate permissions
- Admin → Users: requires `Users.View`
- Admin → Roles: requires `Roles.View`
- Admin → Settings: requires `Settings.View`
- Show admin section only if user has at least one admin permission

---

## Definition of Done

A milestone is complete when:

- [ ] All tasks in the milestone are implemented
- [ ] Unit tests cover the new service logic (RbacAuthorizationService, cache invalidation)
- [ ] API endpoints return correct HTTP status codes (200, 201, 400, 401, 403, 404, 409)
- [ ] System roles cannot be deleted or modified through any code path
- [ ] A disabled user receives 401 on any protected endpoint
- [ ] Permission cache is invalidated after any role assignment change
- [ ] Migration applies cleanly on a fresh DB and on an existing DB with users
- [ ] No TypeScript errors in frontend components
