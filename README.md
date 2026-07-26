# BotPulse

[![CI](https://github.com/your-org/BotPulse/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/BotPulse/actions/workflows/ci.yml)

**BotPulse** es una plataforma agnóstica de operaciones RPA (Robotic Process Automation) para monitoreo centralizado, gestión y análisis de entornos multi-vendor. UiPath es el primer proveedor soportado.

## Documentación

- [Coding Standards](docs/CodingStandards.md)
- [Deployment Guide](docs/Deployment.md)
- [Security](docs/Security.md)
- [Roadmap](docs/Roadmap.md)
- [Architecture Decisions](docs/ADR/README.md)



## Quick Start

```bash
# Clonar y configurar
git clone https://github.com/your-org/BotPulse.git
cd BotPulse
cp .env.example .env

# Levantar dependencias
docker compose up -d postgres redis

# Aplicar migraciones
dotnet ef database update --project src/BotPulse.Infrastructure --startup-project src/BotPulse.Api

# Ejecutar API
dotnet run --project src/BotPulse.Api
```

Ver la [guía completa de deployment](docs/Deployment.md) para más opciones.

## Stack

- **Backend:** .NET 8, ASP.NET Core, PostgreSQL, Serilog
- **Frontend:** React + TypeScript (Vite)
- **Containerización:** Docker + Docker Compose
- **Autenticación:** Pluggable (Entra ID, LDAP, Local)
