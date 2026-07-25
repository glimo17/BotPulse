# ADR-009: Polling/SSE para MVP Real-Time (SignalR después)

## Status
Accepted

## Context
BotPulse necesita notificar a los clientes de la UI cuando ocurren eventos relevantes: cambios de estado en jobs, alertas disparadas, actualización de métricas. El usuario espera que el dashboard se actualice en tiempo real sin necesidad de refrescar manualmente.

Hay varios transportes disponibles para implementar "real-time" en aplicaciones web:
- **Polling**: el cliente pregunta periódicamente al servidor si hay novedades.
- **Long Polling**: el servidor mantiene la conexión abierta hasta que hay una novedad.
- **Server-Sent Events (SSE)**: el servidor mantiene una conexión unidireccional y envía eventos cuando ocurren.
- **WebSockets**: conexión bidireccional full-duplex.
- **SignalR**: abstracción de Microsoft sobre WebSockets (con fallback a SSE y Long Polling).

Para MVP, SignalR tiene un requisito de infraestructura adicional: si hay múltiples instancias de la API, necesita un Redis backplane para que los eventos publicados en una instancia lleguen a los subscribers de las otras instancias. En MVP, Redis no está activo (solo provisionado).

## Decision
BotPulse implementa **Polling y SSE como transportes MVP**, seleccionables por configuración (`NOTIFICATION_TRANSPORT=SSE` o `Polling`).

La abstracción `INotificationDelivery` permite cambiar el transporte sin modificar los servicios de aplicación ni los controllers.

Implementaciones MVP:
- `SseNotificationDelivery`: usa `Channel<NotificationEvent>` por subscriber. El endpoint `/api/v1/notifications/stream` retorna `text/event-stream`.
- `PollingNotificationDelivery`: buffer en memoria con TTL. El endpoint `/api/v1/notifications/pull?since=X` retorna eventos acumulados.

Ambas implementaciones incluyen un `INotificationThrottler` (Token Bucket) para limitar a 1 evento por recurso por segundo.

El skeleton de `SignalRNotificationDelivery` se crea con `throw new NotImplementedException()` y comentarios `// TODO Phase 3` para señalizar el punto de extensión.

## Alternatives Considered

**WebSockets puros**
Máximo control pero mayor complejidad de implementación (handshake, ping/pong, reconnect, message framing). SSE cubre el caso de uso unidireccional (server → client) con menor complejidad. Para mensajes bidireccionales (client → server) no hay necesidad en BotPulse: los comandos van por la API REST. Descartado como transporte principal.

**SignalR desde el inicio**
SignalR es la solución más robusta a largo plazo para ASP.NET Core. Sin embargo, en MVP con una sola instancia de API no necesita backplane. Si se despliegan múltiples instancias sin Redis backplane, los subscribers de una instancia no recibirán eventos publicados en otra. Como Redis no está activo en MVP, SignalR podría dar una falsa sensación de seguridad. Aceptado como transporte de Fase 3.

**Long Polling**
Similar a Polling regular pero más eficiente. SSE cubre mejor el caso de uso de streaming de eventos del servidor. Descartado en favor de SSE.

## Consequences

**Positivas:**
- SSE es nativo del navegador (API `EventSource`), sin dependencias adicionales en el cliente.
- Menor complejidad en MVP: no se necesita Redis backplane ni configuración de SignalR Hub.
- La abstracción `INotificationDelivery` garantiza que la UI no depende del transporte concreto.
- El throttler previene tormentas de eventos cuando hay muchos cambios simultáneos.
- La transición a SignalR en Fase 3 solo requiere implementar `SignalRNotificationDelivery` y cambiar la variable de entorno.

**Negativas:**
- SSE es unidireccional (server → client). Para BotPulse es suficiente, pero si surgiera la necesidad de mensajes bidireccionales, se necesitaría SignalR o WebSockets.
- Con múltiples instancias de API y SSE, un cliente conectado a la instancia A no recibirá eventos publicados en la instancia B. Se mitiga desplegando una sola instancia del API en MVP o usando sticky sessions en el reverse proxy. En Fase 3 con SignalR + Redis backplane esto se resuelve correctamente.
- Polling introduce latencia proporcional al intervalo de poll. Para alertas críticas, SSE es el transporte preferido.
