# ADR-005: OAuth2 Client Credentials para Autenticación con UiPath

## Status
Accepted

## Context
BotPulse necesita autenticarse contra UiPath Orchestrator para realizar llamadas a su API OData. UiPath soporta múltiples mecanismos de autenticación dependiendo de la versión y el tipo de instalación: Basic Authentication (deprecado), API Keys (legacy), y OAuth2 Client Credentials (moderno y recomendado por UiPath para integraciones machine-to-machine).

Los requisitos de seguridad de BotPulse exigen:
- Credenciales rotables sin reiniciar la aplicación.
- No almacenar contraseñas de usuario en el código ni en archivos de configuración.
- Autenticación machine-to-machine (no hay interacción humana en la autenticación a UiPath).

## Decision
BotPulse usa **OAuth2 Client Credentials Flow** para autenticarse contra UiPath Orchestrator.

El flujo:
1. BotPulse envía `client_id` y `client_secret` al endpoint `{BaseUrl}/identity_/connect/token`.
2. UiPath devuelve un `access_token` con `expires_in`.
3. BotPulse cachea el token en memoria y lo incluye como `Authorization: Bearer` en todas las llamadas a la API OData.
4. Antes del vencimiento del token (con un skew configurable), `UiPathOAuth2TokenManager` obtiene automáticamente un nuevo token.
5. Si el refresh falla, se reintenta una re-autenticación completa. Si falla también, el proveedor se marca como unhealthy.

Las credenciales (`UIPATH_CLIENT_ID`, `UIPATH_CLIENT_SECRET`) se pasan como variables de entorno y nunca aparecen en código fuente ni archivos commiteados.

## Alternatives Considered

**Basic Authentication (usuario/contraseña)**
Deprecado por UiPath. Requiere almacenar credenciales de usuario real. Riesgo de seguridad mayor al usar credenciales humanas para llamadas de sistema. El token no expira, lo que amplía la ventana de ataque si es comprometido. Descartado.

**API Key estática**
Más simple que OAuth2, pero no tiene mecanismo de expiración. Si la key es comprometida, el atacante tiene acceso indefinido hasta que se revoque manualmente. OAuth2 Client Credentials ofrece tokens de corta vida con refresh automático. Descartado.

**OAuth2 con refresh tokens (Authorization Code Flow)**
Diseñado para autenticación interactiva con un usuario humano. No aplica para integraciones machine-to-machine donde no hay usuario presente. Descartado.

## Consequences

**Positivas:**
- Los tokens expiran (típicamente en 1 hora), reduciendo la ventana de exposición si son interceptados.
- La rotación de `client_secret` solo requiere actualizar la variable de entorno y reiniciar el token manager, no la aplicación completa.
- Compatible con el modelo de autenticación recomendado por UiPath para aplicaciones externas.
- El Mock UiPath Server implementa el mismo endpoint para desarrollo sin credenciales productivas.

**Negativas:**
- Requiere que el `client_id` y `client_secret` estén registrados en UiPath Orchestrator como una External Application.
- El refresh automático agrega complejidad en `UiPathOAuth2TokenManager` (concurrencia, manejo de errores, backoff).
- Si UiPath cambia el endpoint de token en futuras versiones, hay que actualizar la implementación. Se mitiga con `UiPathVersionNegotiator`.
