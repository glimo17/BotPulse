# BotPulse — Deployment Guide

Esta guía cubre los modelos de despliegue soportados, las variables de entorno requeridas y los procedimientos paso a paso para cada plataforma.

---

## Variables de Entorno Requeridas

Todas las variables marcadas como **Obligatoria** deben estar presentes antes de arrancar. La aplicación falla al iniciar si alguna de ellas falta.

### Base de Datos

| Variable                         | Descripción                                          | Ejemplo                                                     |
|----------------------------------|------------------------------------------------------|-------------------------------------------------------------|
| `DB_PASSWORD`                    | Contraseña de PostgreSQL                             | `s3cur3P@ssw0rd`                                            |
| `ConnectionStrings__PostgreSQL`  | Connection string completa (si no se usa `DB_PASSWORD`) | `Host=db;Port=5432;Database=botpulse;Username=botpulse;Password=...` |

### Seguridad / JWT

| Variable            | Descripción                                                              | Obligatoria |
|---------------------|--------------------------------------------------------------------------|-------------|
| `JWT_SIGNING_KEY`   | Clave de firma del JWT en Base64 (mínimo 32 bytes, 44 chars Base64)      | Sí          |

### Autenticación

| Variable                  | Descripción                                                       | Valores válidos                |
|---------------------------|-------------------------------------------------------------------|-------------------------------|
| `AUTHENTICATION_PROVIDER` | Proveedor de autenticación activo                                 | `Local`, `EntraID`, `LDAP`    |

### Proveedor RPA — UiPath

| Variable               | Descripción                                                    | Obligatoria | Notas                               |
|------------------------|----------------------------------------------------------------|-------------|-------------------------------------|
| `UIPATH_BASE_URL`      | URL base del UiPath Orchestrator                               | Sí          | Ver nota de entorno abajo           |
| `UIPATH_TENANT`        | Nombre del tenant en UiPath                                    | Sí          |                                     |
| `UIPATH_CLIENT_ID`     | Client ID de la aplicación OAuth2 en UiPath                    | Sí          |                                     |
| `UIPATH_CLIENT_SECRET` | Client Secret de la aplicación OAuth2 en UiPath                | Sí          | Nunca en código ni en archivos commiteados |

> **Nota de entorno:**
> - **Desarrollo / Testing**: usar `UIPATH_BASE_URL=http://mock-uipath:5100` para apuntar al Mock UiPath Server incluido en la stack de Docker Compose. No se requieren credenciales productivas.
> - **Producción**: cambiar a la URL real del Orchestrator (ej. `https://cloud.uipath.com/miorganizacion`). El resto de la aplicación no cambia.

### Notificaciones y Caché

| Variable                 | Descripción                                    | Valores válidos        | Default  |
|--------------------------|------------------------------------------------|------------------------|----------|
| `NOTIFICATION_TRANSPORT` | Transporte de notificaciones en tiempo real    | `SSE`, `Polling`       | `SSE`    |
| `CACHE_PROVIDER`         | Implementación de caché                        | `Memory`, `Redis`      | `Memory` |

### Sincronización Background (opcionales, tienen defaults)

| Variable                            | Default | Descripción                              |
|-------------------------------------|---------|------------------------------------------|
| `SYNC_JOBS_INTERVAL_SECONDS`        | `120`   | Intervalo de sincronización de Jobs      |
| `SYNC_QUEUE_ITEMS_INTERVAL_SECONDS` | `180`   | Intervalo de sincronización de QueueItems|
| `SYNC_LOGS_INTERVAL_SECONDS`        | `60`    | Intervalo de sincronización de Logs      |
| `SYNC_METRICS_INTERVAL_SECONDS`     | `300`   | Intervalo de recolección de métricas     |

### CORS

| Variable               | Descripción                                           | Ejemplo                          |
|------------------------|-------------------------------------------------------|----------------------------------|
| `CORS_ALLOWED_ORIGINS` | Orígenes permitidos, separados por coma               | `https://app.example.com`        |

---

## Estrategia de Migraciones de Base de Datos

> **Regla crítica**: Las migraciones se ejecutan **antes** del rollout de la aplicación, nunca de forma automática al iniciar.

Este principio previene condiciones de carrera en despliegues con múltiples réplicas y facilita el rollback.

### Aplicar migraciones manualmente

```bash
dotnet ef database update \
  --project src/BotPulse.Infrastructure \
  --startup-project src/BotPulse.Api \
  --connection "Host=localhost;Port=5432;Database=botpulse;Username=botpulse;Password=..."
```

### Aplicar migraciones con Docker

```bash
docker compose -f docker-compose.yml -f deploy/docker-compose.migrate.yml run --rm migrations
```

### En CI/CD

El pipeline debe incluir un step de migración antes del step de despliegue:

```yaml
# Ejemplo en GitHub Actions
- name: Apply database migrations
  run: |
    docker run --rm \
      -e ConnectionStrings__PostgreSQL="$DB_CONNECTION_STRING" \
      botpulse-migrations:latest
```

---

## Modelo de Despliegue — Matriz

| Modelo                        | API                        | Worker                      | Proxy / Ingress              | Base de Datos                          |
|-------------------------------|----------------------------|-----------------------------|------------------------------|----------------------------------------|
| **Docker Compose** (dev/prod) | Contenedor `api`           | Contenedor `worker`         | nginx / traefik              | postgres + redis                       |
| **Azure App Service**         | App Service (Linux)        | WebJob o App Service sep.   | Azure Front Door / APIM      | Azure Database for PostgreSQL + Redis  |
| **Azure Container Apps**      | Container App              | Container App (jobs mode)   | Azure Front Door             | Azure Database for PostgreSQL + Redis  |
| **IIS Windows**               | AppPool + ANCM             | Windows Service             | IIS / ARR                    | PostgreSQL local o managed             |
| **Linux + Reverse Proxy**     | systemd service            | systemd service             | nginx / traefik              | PostgreSQL local o managed             |

**Principio clave:** el mismo binario se usa en todos los modelos. Solo cambia la configuración vía environment variables.

---

## Setup Local (Paso a Paso)

### Prerrequisitos

- .NET 8.0 SDK
- Docker Desktop o Docker Engine + Docker Compose v2
- `dotnet ef` tools: `dotnet tool install --global dotnet-ef`

### 1. Clonar y configurar entorno

```bash
git clone https://github.com/your-org/BotPulse.git
cd BotPulse
cp .env.example .env
# Editar .env con tus valores (JWT key, DB password, etc.)
```

### 2. Levantar infraestructura (PostgreSQL + Redis + Mock UiPath)

```bash
# Levanta solo las dependencias (no la API ni el Worker)
docker compose up -d postgres redis

# Opcionalmente, también el mock de UiPath para desarrollo sin credenciales reales
docker compose --profile dev up -d mock-uipath
```

### 3. Aplicar migraciones

```bash
dotnet ef database update \
  --project src/BotPulse.Infrastructure \
  --startup-project src/BotPulse.Api
```

### 4. Ejecutar la API

```bash
dotnet run --project src/BotPulse.Api
# API disponible en: https://localhost:5001
# Swagger UI en:     https://localhost:5001/swagger
# Health check en:   https://localhost:5001/health
```

### 5. Ejecutar el Worker (en otra terminal)

```bash
dotnet run --project src/BotPulse.Worker
```

### 6. (Opcional) Levantar el Frontend

```bash
cd ui
npm install
npm run dev
# UI disponible en: http://localhost:5173
# Proxy de /api -> https://localhost:5001 configurado en vite.config.ts
```

---

## Docker Compose — Despliegue Completo

### Producción (sin mock UiPath)

```bash
# Crear archivo .env con variables de producción
cp .env.example .env
# Editar .env

# Aplicar migraciones primero
docker compose -f docker-compose.yml -f deploy/docker-compose.migrate.yml run --rm migrations

# Levantar la stack completa
docker compose up -d
```

### Desarrollo con Mock UiPath

```bash
# Levanta todo incluyendo el mock (perfil dev)
docker compose --profile dev up -d
```

El servicio `mock-uipath` estará disponible en `http://localhost:5100` (externamente) y en `http://mock-uipath:5100` dentro de la red Docker. Configurar `UIPATH_BASE_URL=http://mock-uipath:5100`.

### Verificar el estado

```bash
docker compose ps
curl http://localhost/health/live
curl http://localhost/health/ready
```

---

## Azure App Service

### Prerrequisitos

- Azure CLI autenticado
- Imagen Docker disponible en Azure Container Registry (ACR) o Docker Hub

### Deploy

```bash
# Crear grupo de recursos y App Service Plan
az group create --name rg-botpulse --location eastus
az appservice plan create --name plan-botpulse --resource-group rg-botpulse --sku B2 --is-linux

# Crear la App (API)
az webapp create \
  --resource-group rg-botpulse \
  --plan plan-botpulse \
  --name botpulse-api \
  --deployment-container-image-name myacr.azurecr.io/botpulse-api:latest

# Configurar variables de entorno
az webapp config appsettings set \
  --resource-group rg-botpulse \
  --name botpulse-api \
  --settings \
    AUTHENTICATION_PROVIDER=EntraID \
    UIPATH_BASE_URL=https://cloud.uipath.com/myorg \
    NOTIFICATION_TRANSPORT=SSE
```

Los secretos (`JWT_SIGNING_KEY`, `DB_PASSWORD`, `UIPATH_CLIENT_SECRET`) deben configurarse desde Azure Key Vault con referencias de secreto, nunca directamente como App Settings.

---

## Azure Container Apps

```bash
# Crear Container Apps Environment
az containerapp env create \
  --name env-botpulse \
  --resource-group rg-botpulse \
  --location eastus

# Desplegar la API
az containerapp create \
  --name botpulse-api \
  --resource-group rg-botpulse \
  --environment env-botpulse \
  --image myacr.azurecr.io/botpulse-api:latest \
  --target-port 8080 \
  --ingress external \
  --min-replicas 1 \
  --max-replicas 5

# Desplegar el Worker
az containerapp create \
  --name botpulse-worker \
  --resource-group rg-botpulse \
  --environment env-botpulse \
  --image myacr.azurecr.io/botpulse-worker:latest \
  --ingress disabled \
  --min-replicas 1 \
  --max-replicas 1
```

---

## IIS Windows con ANCM

### Prerrequisitos

- Windows Server con IIS habilitado
- .NET 8 Hosting Bundle instalado (incluye ANCM — ASP.NET Core Module)
- PostgreSQL accesible (local o remoto)

### Pasos

1. Publicar la aplicación:
   ```powershell
   dotnet publish src/BotPulse.Api -c Release -o C:\Sites\BotPulse.Api --self-contained false
   ```

2. Crear un Application Pool en IIS:
   - Sin managed code (No Managed Code)
   - Identidad: cuenta de servicio dedicada (no ApplicationPoolIdentity si necesita acceso a red)

3. Crear el sitio en IIS apuntando a `C:\Sites\BotPulse.Api`.

4. Configurar variables de entorno en la sección **Environment Variables** del Application Pool, o mediante `web.config`:
   ```xml
   <aspNetCore processPath="dotnet" arguments=".\BotPulse.Api.dll" stdoutLogEnabled="false">
     <environmentVariables>
       <environmentVariable name="AUTHENTICATION_PROVIDER" value="EntraID" />
       <!-- Los secretos vienen de Windows Credential Manager o Azure Key Vault -->
     </environmentVariables>
   </aspNetCore>
   ```

5. Para el Worker, registrarlo como Windows Service:
   ```powershell
   sc.exe create BotPulseWorker binPath= "dotnet C:\Services\BotPulse.Worker\BotPulse.Worker.dll"
   sc.exe start BotPulseWorker
   ```

---

## Linux + systemd + nginx

### Publicar y copiar binarios

```bash
dotnet publish src/BotPulse.Api -c Release -o /opt/botpulse/api
dotnet publish src/BotPulse.Worker -c Release -o /opt/botpulse/worker
```

### Crear servicio systemd para la API

```ini
# /etc/systemd/system/botpulse-api.service
[Unit]
Description=BotPulse API
After=network.target postgresql.service

[Service]
Type=notify
User=botpulse
WorkingDirectory=/opt/botpulse/api
ExecStart=/usr/bin/dotnet /opt/botpulse/api/BotPulse.Api.dll
Restart=always
RestartSec=10
EnvironmentFile=/etc/botpulse/environment
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable botpulse-api
sudo systemctl start botpulse-api
```

El archivo `/etc/botpulse/environment` contiene las variables de entorno. Protegerlo con `chmod 600` y ownership `root:botpulse`.

### Configurar nginx como Reverse Proxy

```nginx
server {
    listen 443 ssl http2;
    server_name botpulse.example.com;

    ssl_certificate /etc/nginx/certs/botpulse.crt;
    ssl_certificate_key /etc/nginx/certs/botpulse.key;

    location /api/ {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # SSE requiere deshabilitar buffering
    location /api/v1/notifications/stream {
        proxy_pass http://localhost:8080;
        proxy_buffering off;
        proxy_cache off;
        proxy_set_header Connection '';
        proxy_http_version 1.1;
        chunked_transfer_encoding on;
    }

    location / {
        root /opt/botpulse/ui/dist;
        try_files $uri $uri/ /index.html;
    }
}

server {
    listen 80;
    server_name botpulse.example.com;
    return 301 https://$host$request_uri;
}
```

---

## Certificados TLS para Desarrollo

Para generar un certificado self-signed en desarrollo:

```bash
# Con openssl
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout deploy/nginx/certs/dev.key \
  -out deploy/nginx/certs/dev.crt \
  -subj "/CN=localhost"

# Con dotnet dev-certs
dotnet dev-certs https --export-path deploy/nginx/certs/aspnetcore.pfx --password devpassword
```

---

## Gestión de Secretos por Entorno

| Entorno            | Mecanismo recomendado                                      |
|--------------------|------------------------------------------------------------|
| Desarrollo local   | `dotnet user-secrets` + variables de entorno locales       |
| Docker Compose dev | Archivo `.env` (en `.gitignore`)                           |
| Azure              | Azure Key Vault + referencias en App Settings              |
| AWS                | AWS Secrets Manager + Parameter Store                      |
| HashiCorp Vault    | Vault Agent sidecar con inyección de variables de entorno  |
| Kubernetes         | Kubernetes Secrets (idealmente cifrados y gestionados)     |

La prioridad de fuentes de configuración (mayor a menor):
1. Variables de entorno
2. Secret store (Key Vault, AWS SM, Vault)
3. `appsettings.{Environment}.json`
4. `appsettings.json`

---

## Servir el Frontend desde los archivos estáticos de `ui/dist`

Tras ejecutar `npm run build` en el directorio `ui/`, los archivos estáticos quedan en `ui/dist/`.

- Con **Docker Compose**: el servicio `ui` usa `deploy/Dockerfile.Ui` (multi-stage: `node:20-alpine` para el build, `nginx:1.27-alpine` para servir). El reverse proxy enruta `/` al servicio `ui` y `/api` al servicio `api`.
- Con **nginx standalone**: configurar `root /opt/botpulse/ui/dist;` con `try_files $uri $uri/ /index.html;` para el routing SPA.
- Con **Azure Static Web Apps / Azure Blob Storage**: subir el contenido de `ui/dist` y configurar el fallback a `index.html`.

---

## Smoke Tests Post-Despliegue

Después de cualquier despliegue ejecutar el smoke test:

```powershell
# PowerShell
.\scripts\smoke.ps1 -BaseUrl "https://botpulse.example.com"
```

```bash
# Bash
./scripts/smoke.sh https://botpulse.example.com
```

El script verifica:
1. `GET /health/live` → 200
2. `GET /health/ready` → 200
3. `POST /api/v1/auth/login` con credenciales de prueba → token JWT
4. `GET /api/v1/robots` con el token → lista de robots
