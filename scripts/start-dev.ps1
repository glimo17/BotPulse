# BotPulse - Start Development Stack
# Run from repository root: .\scripts\start-dev.ps1

param(
    [switch]$SkipMigrations,
    [switch]$SkipAdminUser
)

$ErrorActionPreference = "Continue"

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  BotPulse - Starting Development Stack" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

# 1. Check .env exists
if (-not (Test-Path ".env")) {
    Write-Host "ERROR: .env not found. Copy .env.example to .env first." -ForegroundColor Red
    exit 1
}

# 2. Start PostgreSQL
Write-Host "Starting PostgreSQL..." -NoNewline
docker compose up -d postgres 2>&1 | Out-Null

$attempts = 0
$healthy = ""
while ($healthy -ne 'healthy' -and $attempts -lt 15) {
    Start-Sleep -Seconds 2
    $healthy = docker inspect botpulse-db --format='{{.State.Health.Status}}' 2>$null
    $attempts++
    Write-Host "." -NoNewline
}

if ($healthy -eq 'healthy') {
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " WARNING (may still be starting)" -ForegroundColor Yellow
}

# 3. Apply migrations
if (-not $SkipMigrations) {
    Write-Host "Applying database migrations..." -NoNewline
    
    $migrateResult = dotnet ef database update `
        --project src/BotPulse.Infrastructure `
        --startup-project src/BotPulse.Api 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host " OK" -ForegroundColor Green
    } else {
        Write-Host " FAIL" -ForegroundColor Red
        Write-Host $migrateResult -ForegroundColor Red
        Write-Host ""
        Write-Host "TIP: Make sure dotnet-ef is installed:" -ForegroundColor Yellow
        Write-Host "  dotnet tool install --global dotnet-ef" -ForegroundColor Gray
        exit 1
    }
}

# 4. Create admin user
if (-not $SkipAdminUser) {
    Write-Host "Creating admin user..."
    & "$PSScriptRoot\create-admin-user.ps1"
}

Write-Host ""
Write-Host "=================================================" -ForegroundColor Green
Write-Host "  Stack ready!" -ForegroundColor Green
Write-Host "=================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Open NEW terminals and run:" -ForegroundColor White
Write-Host ""
Write-Host "  Terminal 1 - API:" -ForegroundColor Cyan
Write-Host "    dotnet run --project src/BotPulse.Api --launch-profile http" -ForegroundColor Gray
Write-Host "    Swagger: http://localhost:5001/swagger" -ForegroundColor Gray
Write-Host ""
Write-Host "  Terminal 2 - Worker (optional):" -ForegroundColor Cyan
Write-Host "    dotnet run --project src/BotPulse.Worker" -ForegroundColor Gray
Write-Host ""
Write-Host "  Login: admin / Admin@BotPulse2024!" -ForegroundColor Cyan
Write-Host "  Health: http://localhost:5001/health" -ForegroundColor Gray
Write-Host ""
