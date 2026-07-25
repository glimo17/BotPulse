# ADR-004: Docker as Primary Packaging

## Status
Accepted

## Context
BotPulse necesita desplegarse en entornos muy diversos: equipos de desarrollo con Docker Desktop, organizaciones en Azure, entornos corporativos con IIS Windows, y servidores Linux con nginx. El equipo tiene capacidad limitada para mantener múltiples mecanismos de empaquetado.

Se necesita un mecanismo de empaquetado que:
- Garantice consistencia entre el entorno de desarrollo y producción.
- Funcione en múltiples plataformas cloud y on-premises.
- Sea independiente del sistema operativo del host.
- Simplifique el onboarding de nuevos desarrolladores.

## Decision
**Docker** es el mecanismo primario de empaquetado para BotPulse. Todos los componentes (API, Worker, Frontend, Mock UiPath) tienen Dockerfiles multi-stage. Docker Compose es el modelo de despliegue de referencia para desarrollo y producción básica.

Características del empaquetado:
- Dockerfiles multi-stage (build SDK → publish → runtime `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`)
- Imágenes basadas en Alpine para minimizar superficie de ataque
- Usuario no-root dentro del contenedor
- HEALTHCHECK integrado en los Dockerfiles
- Un `docker-compose.yml` que levanta toda la stack (API, Worker, Frontend, PostgreSQL, Redis, nginx)
- Servicio `mock-uipath` con `profiles: ["dev", "test"]` para excluirlo de producción

Los modelos de despliegue alternativos (Azure App Service, Azure Container Apps, IIS, Linux + systemd) usan el mismo binario/imagen, solo cambia la forma de ejecutarlo y la configuración de entorno.

## Alternatives Considered

**Instalador MSI/RPM/DEB**
Instaladores nativos por plataforma. Útil para software de escritorio, pero para aplicaciones de servidor server-side introduce inconsistencias entre entornos y depende de la configuración del SO host. Requeriría mantener scripts de instalación para Windows y Linux por separado. Descartado.

**Solo Azure (Azure App Service / Container Apps)**
Simplificaría el primer despliegue para organizaciones Azure-first pero excluiría despliegues on-premises con IIS o Linux sin Azure. Dado que BotPulse apunta a entornos corporativos diversos, la dependencia de un único cloud es una limitación inaceptable. Descartado como modelo primario (sigue siendo un target soportado).

**Solo binarios publicados (`dotnet publish`)**
Publicar los binarios directamente sin contenedores. Más simple pero requiere que el host tenga el .NET runtime instalado, y no garantiza aislamiento ni consistencia. Descartado como mecanismo primario; sigue siendo válido para IIS y systemd.

## Consequences

**Positivas:**
- Un comando (`docker compose up`) levanta el entorno completo con todas las dependencias.
- El mismo artefacto (imagen Docker) se despliega en desarrollo, staging y producción.
- Independencia del SO del host: funciona en Windows, Linux y macOS.
- Facilita CI/CD: el pipeline construye la imagen una vez y la despliega en cualquier target.
- La imagen Alpine minimiza la superficie de ataque.

**Negativas:**
- Requiere Docker instalado en producción (no siempre disponible en entornos corporativos muy restrictivos).
- La curva de aprendizaje de Docker puede ser un obstáculo para equipos sin experiencia con contenedores.
- La imagen Alpine puede presentar incompatibilidades con algunas librerías nativas (glibc vs musl). Se mitiga eligiendo dependencias compatibles.
- El pull inicial de imágenes puede ser lento en entornos con conectividad limitada. Se mitiga con un registry privado interno.
