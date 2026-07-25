# ADR-012: API Versioning desde el Día 1

## Status
Accepted

## Context
BotPulse es una plataforma que será consumida por múltiples clientes: la UI propia, integraciones con herramientas de BI, scripts de operaciones y potencialmente SDKs de terceros. Una vez que hay consumers de la API, un cambio breaking (renombrar un campo, cambiar el tipo de un parámetro, eliminar un endpoint) tiene un impacto real en todos ellos.

Si la API no está versionada desde el inicio, cuando sea necesario introducir cambios breaking habrá que versionar retroactivamente o romper los consumers existentes. Retroversionar una API ya en producción es costoso y propenso a errores.

La experiencia del equipo indica que el overhead de versionar desde el principio es mínimo comparado con el costo de hacerlo más adelante.

## Decision
**Todos los endpoints de BotPulse están bajo `/api/v{version}/`** desde el primer commit de código.

Configuración:
- Default version: `1.0`
- `AssumeDefaultVersionWhenUnspecified = false` (los requests sin versión devuelven HTTP 400)
- Version readers: `UrlSegmentApiVersionReader` + `HeaderApiVersionReader("api-version")`
- Un documento Swagger por versión: `/swagger/v1/swagger.json`
- Los controllers declaran su versión: `[ApiVersion("1.0")]`

Política de deprecation:
- Cuando se publique v2, v1 continúa disponible al menos 6 meses.
- Los responses de v1 incluyen header `Deprecation` cuando se anuncia el sunset.
- El timeline de deprecation se comunicará en el changelog.

## Alternatives Considered

**Versionar cuando sea necesario (más adelante)**
El enfoque "no lo necesitamos ahora". El problema es que agregar versionado retroactivamente requiere cambiar todas las rutas de los endpoints, actualizar todos los clients, y puede requerir duplicar controllers temporalmente. El overhead de hacerlo desde el inicio (`[ApiVersion("1.0")]` + URL prefix) es de minutos, no de días. Descartado.

**URL sin versión (`/api/robots` en lugar de `/api/v1/robots`)**
Más limpio visualmente. Sin embargo, cuando sea necesario introducir v2 habrá que tomar una decisión retroactiva sobre cómo versionar. Descartado.

**Versionar por header únicamente (sin versión en URL)**
Más "RESTful purista" (las URLs no deben cambiar por la versión de representación). Pero dificulta el debugging, las pruebas manuales con curl/Postman, y la exploración en Swagger UI. BotPulse soporta ambos (header `api-version` + URL segment) por conveniencia. La URL segment es la principal.

**Versionado por fecha (calendar versioning: `2024-01-20`)**
Usado por algunas APIs de Stripe y GitHub. Más expresivo que números de versión pero introduce complejidad en el routing y puede confundir a los consumers. Descartado.

## Consequences

**Positivas:**
- Los consumers de la API pueden actualizar de v1 a v2 de forma gradual sin fecha límite estricta.
- Un cambio breaking en v2 no rompe ningún consumer existente de v1.
- La documentación Swagger es versión-específica, lo que facilita la migración.
- El overhead de configurar el versionado es mínimo desde el inicio.

**Negativas:**
- Un pequeño overhead de configuración al inicio (`services.AddApiVersioning(...)` y atributos en controllers).
- Los consumers deben incluir la versión en la URL o en el header. Con `AssumeDefaultVersionWhenUnspecified = false`, requests sin versión fallan. Esto es intencional para obligar a los consumers a ser explícitos.
- Mantener dos versiones de API en paralelo (v1 + v2) durante el período de transición requiere mantener dos sets de controllers o adaptar las rutas existentes.
