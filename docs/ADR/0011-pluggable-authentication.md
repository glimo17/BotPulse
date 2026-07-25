# ADR-011: Autenticación Pluggable con IAuthenticationProvider

## Status
Accepted

## Context
BotPulse debe funcionar en entornos corporativos muy diversos. Algunas organizaciones usan Microsoft Entra ID (Azure AD), otras tienen LDAP corporativo, y durante el desarrollo se necesita un proveedor local simple sin dependencias externas.

Si se hardcodea un único proveedor de autenticación, BotPulse no puede adaptarse a diferentes entornos sin modificar el código. Si se intentan soportar todos los proveedores simultáneamente con condicionales en el mismo código, la lógica se vuelve compleja e improbable.

Adicionalmente, el modelo de dominio de BotPulse (entidades `User`, claims, roles) debe ser independiente del IdP específico. Un `User` en BotPulse tiene un `Role` interno (`Viewer`, `Operator`, `Administrator`), independientemente de si vino de Entra ID, LDAP o una tabla local.

## Decision
BotPulse implementa **autenticación pluggable** mediante la interfaz `IAuthenticationProvider`:

```csharp
public interface IAuthenticationProvider
{
    string ProviderName { get; }
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request, CancellationToken ct);
    Task<bool> IsHealthyAsync(CancellationToken ct);
}
```

Implementaciones disponibles:
- `EntraIdAuthenticationProvider` (Microsoft Entra ID / Azure AD con OIDC)
- `LdapAuthenticationProvider` (LDAP con simple bind + group mapping)
- `LocalAuthenticationProvider` (tabla `users` con Argon2id, solo para desarrollo)

La selección se hace por configuración (`AUTHENTICATION_PROVIDER=EntraID|LDAP|Local`) en el método `AddPluggableAuthentication(configuration)`. Si el valor es inválido, el arranque falla con mensaje descriptivo.

`LocalAuthenticationProvider` emite un `Warning` si se activa en un entorno de producción.

Agregar un nuevo proveedor (Okta, Auth0, Google Workspace, Ping Identity) requiere:
1. Crear una clase `XxxAuthenticationProvider : IAuthenticationProvider`.
2. Agregar un `case` en `AddPluggableAuthentication`.
3. Documentar las variables de entorno en `docs/Deployment.md`.
4. El Core, la API, el Worker y los tests existentes no cambian.

## Alternatives Considered

**Hardcodear Entra ID como único proveedor**
Más simple inicialmente. Inaceptable para entornos sin Azure AD (LDAP corporativo on-premises, desarrollo local sin conexión a tenant). Descartado.

**Usar ASP.NET Core Identity completo**
`Microsoft.AspNetCore.Identity` es una solución robusta para autenticación con usuarios locales. Sin embargo, mezcla la gestión de usuarios, autenticación, autorizaciones y cookies en un solo stack que es difícil de intercambiar por proveedores externos. La abstracción `IAuthenticationProvider` es más liviana y mejor alineada con el diseño. Descartado.

**Configuración de múltiples esquemas en ASP.NET Core**
Registrar múltiples esquemas de autenticación (`JwtBearer` + `Cookie` + OpenIdConnect) y seleccionar dinámicamente. Más complejo de gestionar que una abstracción propia, y mezcla la autenticación de sesión (JWT propio) con la autenticación externa (IdP). Descartado.

## Consequences

**Positivas:**
- El Core no conoce ningún IdP específico. `AuthenticationOrchestrator` solo interactúa con `IAuthenticationProvider`.
- El health check de autenticación (`IsHealthyAsync`) funciona para cualquier proveedor.
- El cambio de proveedor en producción no requiere recompilación, solo cambiar la variable de entorno y reiniciar.
- Los unit tests de `AuthenticationOrchestrator` mockean `IAuthenticationProvider` y son independientes de cualquier IdP.

**Negativas:**
- El equipo debe implementar y mantener una abstracción propia en lugar de reutilizar completamente la stack de ASP.NET Core Identity.
- Cada nuevo proveedor requiere entender las particularidades del protocolo (OIDC para Entra ID, RFC 4511 para LDAP). Se mitiga con guías en `docs/Deployment.md` por proveedor.
