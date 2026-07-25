# BotPulse — Security Posture

Este documento describe la postura de seguridad de BotPulse: cómo se gestionan las credenciales, cómo se asegura el transporte, qué protecciones existen a nivel de datos y cómo se mantiene la trazabilidad de acciones sensibles.

---

## 1. Gestión de Credenciales

### Principios generales

- **Ningún secreto en el código fuente ni en el repositorio.** Las contraseñas, API keys, tokens de firma JWT y client secrets de OAuth2 se pasan siempre a través de variables de entorno o un secret store externo.
- **Rotación periódica.** Las claves de firma JWT y los client secrets de UiPath deben rotarse regularmente. El sistema soporta rotación sin downtime.
- **Separación de secretos por entorno.** Dev, staging y producción tienen secretos distintos. El `.env` de producción nunca se commitea.

### Variables de entorno críticas

| Variable               | Descripción                                               | Mínimo de seguridad                            |
|------------------------|-----------------------------------------------------------|------------------------------------------------|
| `DB_PASSWORD`          | Contraseña de PostgreSQL                                  | Mínimo 16 caracteres, alfanumérico + símbolos  |
| `JWT_SIGNING_KEY`      | Clave de firma JWT en Base64                              | Mínimo 256 bits (32 bytes)                     |
| `UIPATH_CLIENT_SECRET` | Client Secret OAuth2 de UiPath                            | Generado por UiPath, no modificar manualmente  |

### Secret Stores recomendados por entorno

| Entorno     | Recomendación                                               |
|-------------|-------------------------------------------------------------|
| Desarrollo  | `dotnet user-secrets` (nunca commiteado)                    |
| Docker      | Archivo `.env` con permisos `600`, excluido de git          |
| Azure       | Azure Key Vault con referencias en App Settings             |
| AWS         | AWS Secrets Manager o Parameter Store (SecureString)        |
| Kubernetes  | Kubernetes Secrets cifrados en etcd + acceso RBAC           |
| On-premises | HashiCorp Vault con Vault Agent sidecar                     |

### UiPath Assets

Los Assets de UiPath pueden contener valores de tipo credencial (usuario/contraseña) o config. BotPulse **nunca** expone el valor de un Asset en ninguna respuesta de API. El DTO `AssetMetadata` contiene solo metadatos (nombre, tipo, scope, última modificación). El campo de valor secreto no existe en la capa de dominio.

---

## 2. Hashing de Contraseñas

Cuando se usa el proveedor de autenticación `Local` (solo recomendado para desarrollo), las contraseñas de usuario se almacenan con **Argon2id**.

### Parámetros de Argon2id

| Parámetro         | Valor               | Notas                                              |
|-------------------|---------------------|----------------------------------------------------|
| Iterations (t)    | `3`                 | Número de iteraciones de CPU                       |
| Memory (m)        | `65536` KiB (64 MiB)| Uso de memoria por hashing                         |
| Parallelism (p)   | `1`                 | Grado de paralelismo                               |
| Hash length       | `32` bytes          |                                                    |
| Salt length       | `16` bytes          | Generado criptográficamente, único por hash        |

Estos parámetros superan las recomendaciones mínimas de OWASP para Argon2id (t=1, m=37 MiB, p=1). Son ajustables vía configuración si el hardware de producción lo permite.

### Verificación de contraseña

La comparación usa `CryptographicOperations.FixedTimeEquals` para prevenir ataques de timing. El mensaje de error ante fallo de autenticación es genérico (`"Invalid credentials"`) independientemente de si la falla fue por usuario inexistente o contraseña incorrecta.

### Proveedor Local en producción

Si `AUTHENTICATION_PROVIDER=Local` y `ASPNETCORE_ENVIRONMENT=Production`, la aplicación emite un `LogLevel.Warning` al arrancar indicando que el proveedor local está pensado para entornos de desarrollo. Para producción se recomienda `EntraID` o `LDAP`.

---

## 3. JWT — Session Token Post-Autenticación

El JWT en BotPulse no es un método de autenticación. Es el session token que se emite **después** de que un `IAuthenticationProvider` valida exitosamente al usuario.

### Características

| Propiedad          | Valor/Comportamiento                                              |
|--------------------|-------------------------------------------------------------------|
| Algoritmo          | HMAC SHA-256 (`HS256`)                                            |
| Clave de firma     | Cargada desde secret store, configurable, rotable                 |
| Issuer / Audience  | Configurables (`botpulse` / `botpulse-api` por default)           |
| Expiración         | Configurable entre 15 minutos y 8 horas. Default: **1 hora**      |
| Claims incluidos   | `sub`, `name`, `email`, `auth_provider`, `role`(s)               |

### Flujo

1. El cliente envía credenciales a `POST /api/v1/auth/login`.
2. El `AuthenticationOrchestrator` llama al `IAuthenticationProvider` activo (Entra ID, LDAP o Local).
3. Si la autenticación es exitosa, `ISessionTokenService` emite un JWT firmado.
4. El cliente incluye el JWT en el header `Authorization: Bearer <token>` en cada petición.
5. El middleware de autenticación de ASP.NET Core valida la firma, el issuer, el audience y la expiración.
6. Tokens expirados o con firma inválida devuelven HTTP 401 con `errorCode: UNAUTHENTICATED`.

### Rotación de la clave de firma

Para rotar la clave de firma:

1. Actualizar `JWT_SIGNING_KEY` en el secret store con la nueva clave.
2. Reiniciar las instancias de la API (rolling restart en producción).
3. Los tokens existentes firmados con la clave anterior serán inválidos inmediatamente. Los usuarios deberán autenticarse de nuevo.

---

## 4. HTTPS Obligatorio en Producción

En todos los entornos que no sean `Development`:

- **Redirect HTTP → HTTPS**: activado con `app.UseHttpsRedirection()`.
- **HSTS**: activado con `app.UseHsts()` (header `Strict-Transport-Security`).
- El reverse proxy (nginx, Azure Front Door, IIS ARR) termina TLS. La comunicación interna entre el proxy y la API puede ser HTTP en una red privada y segura.

En `Development` se usan certificados de desarrollo generados por `dotnet dev-certs https`.

---

## 5. CORS Restrictivo

La configuración de CORS solo permite orígenes explícitamente configurados. No se acepta `*` (comodín) en producción.

```json
{
  "Cors": {
    "AllowedOrigins": ["https://app.example.com", "https://admin.example.com"]
  }
}
```

Equivalente vía variable de entorno:

```
CORS_ALLOWED_ORIGINS=https://app.example.com,https://admin.example.com
```

Los orígenes no listados recibirán una respuesta de error CORS. Para APIs consumidas desde backends (machine-to-machine), CORS no aplica.

---

## 6. Prevención de SQL Injection

BotPulse usa **Entity Framework Core** con consultas parametrizadas en todos los accesos a datos. Nunca se concatenan valores de usuario en strings de SQL.

Reglas:
- Prohibido el uso de `FromSqlRaw` con interpolación de variables de usuario.
- Si es necesario usar `FromSqlRaw`, todos los valores deben ser parámetros (`SqlParameter`).
- Los filtros de repositorios usan expresiones LINQ que EF Core traduce a consultas parametrizadas.

---

## 7. Validación de Entrada

Todos los DTOs de entrada de la API se validan con **FluentValidation** antes de llegar al Application Service. Los errores de validación devuelven HTTP 400 con `errorCode: VALIDATION_ERROR` y la lista de campos inválidos.

Protecciones adicionales:
- Los campos de texto libre (mensajes de búsqueda, filtros) se tratan como datos sin privilegios, nunca se interpolan directamente en queries.
- Los parámetros de paginación (`top`, `skip`, `page`) tienen límites máximos para prevenir ataques de denegación de servicio.

---

## 8. RBAC — Control de Acceso Basado en Roles

BotPulse tiene tres roles built-in con escalada de permisos:

| Rol             | Capacidades                                                                         |
|-----------------|-------------------------------------------------------------------------------------|
| `Viewer`        | Solo lectura: robots, jobs (sin acciones), colas, logs, métricas, dashboard        |
| `Operator`      | Todo lo de Viewer + acciones sobre jobs (Start, Stop, Cancel, Retry) + ack alertas |
| `Administrator` | Todo lo de Operator + gestión de alert rules + acceso a assets + admin sync        |

Las políticas de autorización se aplican a nivel de endpoint con `[Authorize(Policy = ...)]`. Toda decisión de autorización (grant o deny) queda registrada en el audit log.

---

## 9. Audit Trail

### Propósito

Mantener trazabilidad de todas las acciones sensibles realizadas por usuarios. El audit log es la fuente de verdad para análisis de seguridad e investigación de incidentes.

### Acciones auditadas

| Acción                    | Campos registrados                                          |
|---------------------------|-------------------------------------------------------------|
| `Login` / `Logout`        | usuario, IP, resultado, proveedor de autenticación          |
| `StartJob` / `StopJob` / `CancelJob` / `RetryJob` | usuario, job ID, resultado                |
| Modificación de AlertRule  | usuario, regla ID, cambios aplicados                       |
| Lectura de Assets          | usuario, asset name (sin valor secreto)                    |
| Cambios de configuración   | usuario, clave modificada (sin valor)                      |
| Trigger manual de sync     | usuario, servicio, timestamp                               |
| Acknowledge de alerta      | usuario, alert ID                                          |

### Propiedades de cada registro

```json
{
  "id": 1234,
  "timestamp_utc": "2024-01-20T12:34:56.789Z",
  "user_id": "user-uuid",
  "user_name": "jane.doe",
  "action": "StartJob",
  "resource_type": "Job",
  "resource_id": "ext-job-abc123",
  "outcome": "Success",
  "ip_address": "10.0.1.50",
  "correlation_id": "1ff34a09b8cd4a71a2f9",
  "details_json": { "processId": "proc-456" }
}
```

### Inmutabilidad

La tabla `audit_records` es **append-only** desde el punto de vista de la aplicación. No existe ninguna API de Update ni Delete sobre registros de auditoría. La retención se configura en semanas/meses y se ejecuta mediante jobs de mantenimiento de base de datos externos a la aplicación. El default es 24 meses.

---

## 10. Headers de Seguridad HTTP

Los siguientes headers de seguridad son aplicados por el middleware de ASP.NET Core y el reverse proxy nginx:

| Header                        | Valor recomendado                                          |
|-------------------------------|------------------------------------------------------------|
| `Strict-Transport-Security`   | `max-age=31536000; includeSubDomains`                      |
| `X-Content-Type-Options`      | `nosniff`                                                  |
| `X-Frame-Options`             | `DENY`                                                     |
| `Referrer-Policy`             | `strict-origin-when-cross-origin`                          |
| `Content-Security-Policy`     | Configurado por la aplicación según las fuentes permitidas |

---

## 11. Logging de Seguridad

Las siguientes condiciones siempre se logean en nivel `Warning` o superior:

- Fallos de autenticación (sin revelar si fue usuario o contraseña).
- Accesos denegados por RBAC (HTTP 403).
- Tokens JWT expirados o inválidos.
- Arranque con `AUTHENTICATION_PROVIDER=Local` en entorno de producción.
- Fallo de conectividad con el UiPath Orchestrator (o mock).
- Intento de acceder a Assets (auditado también en `audit_records`).

Los valores de secretos nunca aparecen en los logs. Los campos sensibles se enmascaran con `Serilog.Destructurama` (marcados con `[Redacted]`).

---

## 12. Resumen de Controles de Seguridad

| Control                              | Estado en MVP | Notas                                         |
|--------------------------------------|---------------|-----------------------------------------------|
| HTTPS obligatorio                    | Sí            | Enforced vía middleware + HSTS                |
| JWT firmado con clave rotable        | Sí            | HMAC SHA-256, expiración 1h default           |
| Argon2id para contraseñas locales    | Sí            | t=3, m=64MiB, p=1                             |
| CORS restrictivo                     | Sí            | Solo orígenes configurados                    |
| RBAC con 3 roles                     | Sí            | Viewer / Operator / Administrator             |
| SQL injection prevention             | Sí            | EF Core con parámetros                        |
| Audit trail append-only              | Sí            | 24 meses de retención, correlationId          |
| Secrets via env vars / secret store  | Sí            | Nunca en archivos commiteados                 |
| Assets: sin exposición de valores    | Sí            | `AssetMetadata` no tiene campo de valor       |
| Rate limiting                        | Preparado     | Se activa en Fase 3 con Redis                 |
| OAuth2 PKCE para usuarios            | Fase 2        | Para flujo Entra ID con navegador             |
| Multi-tenancy isolation              | Fase 4        | Aislamiento por tenant en persistence         |
