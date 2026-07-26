# BotPulse — Render.com Deployment Guide

## Prerequisites

- GitHub repository connected to Render
- Render account (free tier is sufficient for MVP demo)

## Architecture on Render

```
┌─────────────────┐     ┌──────────────────┐     ┌───────────────┐
│  botpulse-ui    │────▶│  botpulse-api    │────▶│  botpulse-db  │
│  (Static Site)  │     │  (Web Service)   │     │  (PostgreSQL) │
│  React SPA      │     │  .NET 8 Docker   │     │  Free 256MB   │
└─────────────────┘     └──────────────────┘     └───────────────┘
```

## Deployment Steps

### Option A: Blueprint (Recommended)

1. Go to Render Dashboard → **New** → **Blueprint**
2. Connect your GitHub repo (`glimo17/BotPulse`)
3. Select branch `feature/render-deployment` (or `main` once merged)
4. Render will detect `render.yaml` and create all services automatically
5. Wait for initial deploy (~5 min for Docker build)

### Option B: Manual Setup

#### 1. Create PostgreSQL Database
- Render Dashboard → **New PostgreSQL**
- Name: `botpulse-db`
- Database: `botpulse`
- User: `botpulse`
- Plan: **Free**
- Copy the **Internal Connection String**

#### 2. Create Web Service (API)
- Render Dashboard → **New Web Service**
- Connect GitHub repo
- Name: `botpulse-api`
- Runtime: **Docker**
- Dockerfile Path: `./deploy/Dockerfile.Api`
- Docker Context: `.`
- Plan: **Free**
- Environment Variables (set these):
  ```
  ASPNETCORE_ENVIRONMENT=Production
  ASPNETCORE_URLS=http://+:8080
  ConnectionStrings__PostgreSQL=<paste Internal Connection String from step 1>
  Jwt__SigningKeyBase64=<generate with: openssl rand -base64 32>
  Jwt__Issuer=botpulse
  Jwt__Audience=botpulse-api
  Jwt__ExpirationMinutes=60
  Authentication__Provider=Local
  RpaProvider=Demo
  Notifications__Transport=SSE
  Cors__AllowedOrigins=https://botpulse-ui.onrender.com
  ```
- Health Check Path: `/health/live`

#### 3. Create Static Site (Frontend)
- Render Dashboard → **New Static Site**
- Connect same GitHub repo
- Name: `botpulse-ui`
- Build Command: `cd ui && npm ci && npm run build`
- Publish Directory: `ui/dist`
- Add Rewrite Rule: `/* → /index.html` (SPA fallback)
- Environment Variables:
  ```
  NODE_VERSION=20
  VITE_API_URL=https://botpulse-api.onrender.com
  ```

### Post-Deploy Steps

1. **Wait for API to be healthy** — check `/health/live` returns 200
2. **Migrations are applied automatically** — EF Core runs pending migrations on startup
3. **Create admin user** — Use the API's admin endpoint:
   - Go to `https://botpulse-api.onrender.com/swagger`
   - Or run the SQL script in `deploy/render/create-admin.sql` via Render's PostgreSQL console

## Environment Variables Reference

| Variable | Required | Default | Description |
|----------|----------|---------|-------------|
| `ConnectionStrings__PostgreSQL` | Yes | — | PostgreSQL connection string |
| `Jwt__SigningKeyBase64` | Yes | — | Base64-encoded 32+ byte key |
| `Jwt__Issuer` | No | botpulse | JWT issuer claim |
| `Jwt__Audience` | No | botpulse-api | JWT audience claim |
| `Authentication__Provider` | No | Local | Auth provider (Local for demo) |
| `RpaProvider` | No | Demo | RPA provider (Demo = no UiPath needed) |
| `Notifications__Transport` | No | SSE | Real-time transport |
| `Cors__AllowedOrigins` | Yes | — | Frontend URL(s), comma-separated |
| `ASPNETCORE_URLS` | No | http://+:8080 | Listen URL |

## Troubleshooting

- **API shows "unhealthy"**: Check PostgreSQL connection string is correct
- **502 Bad Gateway on first request**: Free tier needs ~30s cold start
- **CORS errors in browser**: Verify `Cors__AllowedOrigins` includes the exact frontend URL
- **Login fails**: Admin user needs to be created after first deploy
- **SSE 401 errors**: Expected — EventSource doesn't support auth headers (cosmetic issue)

## Free Tier Limitations

- **API sleeps after 15 min** of inactivity — first request after sleep takes ~30s
- **PostgreSQL expires in 90 days** — recreate or upgrade before expiration
- **No Redis** — in-memory cache only (fine for DemoProvider)
- **750 hours/month** — one service running 24/7 is fine (~720h/month)
