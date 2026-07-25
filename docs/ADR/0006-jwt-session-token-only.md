# ADR-006: JWT Solo como Session Token Post-Autenticación

## Status
Accepted

## Context
BotPulse soporta múltiples proveedores de autenticación (Entra ID, LDAP, Local). Una vez que el usuario es autenticado por su IdP, la API necesita un mecanismo para mantener la sesión de forma stateless entre peticiones.

Hay un patrón común pero incorrecto: usar JWT como método de autenticación directamente (aceptar tokens JWT de terceros como credenciales de identidad). Esto confunde el rol de JWT y complica la validación de firmas de múltiples emisores.

La pregunta es: ¿cómo se gestiona la sesión del usuario de forma segura después de que la autenticación es exitosa?

## Decision
**JWT en BotPulse es exclusivamente un session token**, emitido por BotPulse tras una autenticación exitosa. No es un método de autenticación en sí mismo.

Flujo:
1. El cliente envía credenciales a `POST /api/v1/auth/login`.
2. `AuthenticationOrchestrator` delega en el `IAuthenticationProvider` activo (Entra ID, LDAP o Local).
3. Si la autenticación es exitosa, `ISessionTokenService.IssueToken` emite un JWT firmado por BotPulse.
4. El JWT tiene `exp` (expiración), `iss` (BotPulse), `aud` (botpulse-api) y los claims del usuario.
5. El cliente incluye este JWT en `Authorization: Bearer` en cada petición subsiguiente.
6. El middleware de ASP.NET Core valida la firma y los claims del JWT de BotPulse. No valida tokens de terceros directamente.

La clave de firma se carga desde secret store (variable de entorno `JWT_SIGNING_KEY` en Base64), nunca hardcodeada. La expiración es configurable entre 15 y 480 minutos, con default de 60 minutos.

## Alternatives Considered

**Session Cookies**
Un enfoque stateful clásico. Requiere almacenamiento de sesión del lado del servidor (memoria, Redis, base de datos) para validar la sesión en cada petición. Con múltiples instancias de la API, se necesita un store compartido. Aumenta la complejidad de deployment y no es nativamente compatible con APIs consumidas por clientes no-navegador. Descartado.

**OAuth2 para usuarios también (Authorization Server)**
Convertir BotPulse en un Authorization Server que emita tokens OAuth2 para los usuarios. Es la arquitectura "correcta" para escenarios enterprise complejos. Para el MVP, la complejidad de implementar un AS completo supera el beneficio. BotPulse no es un IdP. Descartado para MVP; puede reconsiderarse en Fase 3+.

**Aceptar directamente tokens de Entra ID como sesión**
Confiaría el token JWT de Entra ID como token de sesión de BotPulse. Requeriría validar firmas de múltiples emisores, manejar diferencias de claims entre providers y no funciona para LDAP ni Local (que no emiten JWT). Descartado.

## Consequences

**Positivas:**
- Stateless: ningún store de sesión necesario. Cualquier instancia de la API puede validar cualquier token.
- Compatible con todos los proveedores de autenticación (Entra ID, LDAP, Local) porque BotPulse siempre emite su propio JWT.
- La expiración corta (1h default) limita la ventana de exposición de tokens comprometidos.
- Simple de implementar con `System.IdentityModel.Tokens.Jwt`.

**Negativas:**
- No hay revocación en mid-session sin una blacklist. Si un token es comprometido, es válido hasta su expiración.
- Para revocar un token es necesario implementar una lista negra (almacenada en Redis). Esto se puede agregar en Fase 3 si es necesario.
- La rotación de la clave de firma invalida todos los tokens existentes. Los usuarios deben re-autenticarse. Se mitiga haciendo las rotaciones en mantenimientos programados.
