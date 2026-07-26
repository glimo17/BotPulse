# ADR-017: RBAC — Autorización Independiente del Proveedor de Autenticación

## Status
Proposed (Planned for Fase 2)

## Context
BotPulse ya soporta múltiples proveedores de autenticación (Local, Entra ID, LDAP) mediante el patrón `IAuthenticationProvider`. Sin embargo, la autorización actual es muy básica — solo tiene 3 roles hardcodeados (Administrator, Operator, Viewer) implementados con las políticas de ASP.NET Core.

Los clientes enterprise necesitan:
1. Múltiples roles con permisos granulares
2. Roles personalizados por organización (Finance Supervisor, Night Shift Operator, etc.)
3. Que la autorización funcione independientemente del IdP
4. Folder-level security (futuro)
5. Auditoría completa de operaciones de seguridad

El sistema actual no puede evolucionar a estos requisitos sin un rediseño de la capa de autorización.

## Decision

Se introduce un módulo `BotPulse.Authorization` que implementa RBAC completo:

1. **Permission Catalog** — 23 permisos granulares (Dashboard.View, Jobs.Execute, etc.) definidos como constantes.
2. **Role Entity** — entidad de base de datos que almacena roles con sus permisos. 3 roles del sistema (no eliminables) + roles personalizados ilimitados.
3. **IAuthorizationService** — interfaz que resuelve `IEnumerable<Permission> GetPermissionsAsync(userId)` con caché por sesión.
4. **Permission Policies** — cada política de ASP.NET Core mapea a un permiso del catálogo.
5. **User-Role Assignment** — tabla many-to-many `UserRoles` con soporte para scope futuro (folder-level).
6. **Audit Service** — registro inmutable de operaciones de seguridad.
7. **Admin UI** — vistas de gestión de usuarios, roles y permisos.

### Separación Auth vs Authz

```
Authentication (Who are you?)        Authorization (What can you do?)
─────────────────────────────        ──────────────────────────────────
IAuthenticationProvider              IAuthorizationService
  └─ LocalAuthProvider                 └─ RbacAuthorizationService
  └─ EntraIdAuthProvider                   └─ RoleRepository
  └─ LdapAuthProvider                      └─ PermissionCache
  └─ OktaAuthProvider (future)
  └─ Auth0Provider (future)
```

El `ClaimsPrincipal` del usuario se enriquece con sus permisos BotPulse después de la autenticación, de forma transparente al auth provider.

## Alternatives Considered

**Usar solo las políticas de ASP.NET Core con claims**
Los claims se emiten en el token JWT y no se pueden invalidar sin reemitir el token. No soporta cambios de permisos en tiempo real. Descartado por inflexibilidad.

**Usar un sistema de autorización externo (OPA, Casbin)**
Introduce una dependencia de infraestructura adicional. Complejidad de operación mayor que los beneficios para el MVP. Descartado por over-engineering.

**Permissions como Enums hardcodeados**
No extensible — añadir un permiso requiere recompilar. Descartado por falta de flexibilidad para clientes enterprise.

## Consequences

**Positivas:**
- Clientes pueden crear roles exactamente ajustados a su organización sin modificar el producto.
- La autorización no depende del auth provider — cambiar de LDAP a Entra ID no afecta los roles.
- Folder-level security es extensible sin cambios de schema.
- Auditoría completa para compliance.

**Negativas:**
- Añade complejidad al sistema: nuevas tablas, nuevo módulo, UI de admin.
- La caché de permisos introduce latencia eventual (máx 5s) ante cambios de roles.
- Migración: los usuarios existentes con rol String en la tabla `users` necesitan migración.
