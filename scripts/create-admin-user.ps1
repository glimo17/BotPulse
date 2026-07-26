# BotPulse - Create Initial Admin User
# Run from repository root: .\scripts\create-admin-user.ps1
# Requires PostgreSQL running and migrations applied

param(
    [string]$UserName = "admin",
    [string]$Email = "admin@botpulse.local",
    [string]$Password = "Admin@BotPulse2024!",
    [string]$EnvFile = ".env"
)

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  BotPulse - Create Initial Admin User" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Load .env
$envPath = Join-Path (Split-Path $PSScriptRoot -Parent) $EnvFile
if (-not (Test-Path $envPath)) {
    Write-Host "ERROR: .env not found. Copy .env.example to .env first." -ForegroundColor Red
    exit 1
}

$envVars = @{}
Get-Content $envPath | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
        $envVars[$Matches[1].Trim()] = $Matches[2].Trim()
    }
}

$connStr = $envVars["ConnectionStrings__PostgreSQL"]
if (-not $connStr) {
    $dbPassword = if ($envVars["DB_PASSWORD"]) { $envVars["DB_PASSWORD"] } else { "botpulse_dev_2024" }
    $connStr = "Host=localhost;Port=5432;Database=botpulse;Username=botpulse;Password=$dbPassword"
}

Write-Host "Database: $($connStr -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor Gray
Write-Host "UserName: $UserName" -ForegroundColor Gray
Write-Host "Email:    $Email" -ForegroundColor Gray
Write-Host ""

# Generate Argon2id hash using a small inline dotnet project
$tempDir = Join-Path $env:TEMP "botpulse-hash"
New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Konscious.Security.Cryptography.Argon2" Version="1.3.1" />
  </ItemGroup>
</Project>
"@ | Set-Content "$tempDir/HashGen.csproj"

@"
using System.Security.Cryptography;
using Konscious.Security.Cryptography;

var password = args.Length > 0 ? args[0] : "Admin@BotPulse2024!";
var salt = RandomNumberGenerator.GetBytes(16);
using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
{
    Salt = salt,
    DegreeOfParallelism = 1,
    MemorySize = 65536,
    Iterations = 3,
};
var hash = argon2.GetBytes(32);
Console.Write($"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}");
"@ | Set-Content "$tempDir/Program.cs"

Write-Host "Generating Argon2id password hash..." -NoNewline

Push-Location $tempDir
try {
    $null = & dotnet restore --verbosity quiet 2>&1
    $passwordHash = & dotnet run --verbosity quiet -- $Password 2>&1
    Write-Host " Done" -ForegroundColor Green
} finally {
    Pop-Location
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}

if (-not $passwordHash -or $passwordHash -notmatch ':') {
    Write-Host "ERROR: Failed to generate password hash" -ForegroundColor Red
    exit 1
}

# Insert user via docker exec + psql
$externalId = [System.Guid]::NewGuid().ToString()

$sqlInsert = @"
INSERT INTO users (id, external_id, user_name, email, role, auth_provider, password_hash, is_active, created_at_utc, updated_at_utc)
VALUES (
    gen_random_uuid(),
    '$externalId',
    '$UserName',
    '$Email',
    'Administrator',
    'Local',
    '$passwordHash',
    true,
    NOW(),
    NOW()
)
ON CONFLICT (auth_provider, external_id) DO UPDATE
SET password_hash = EXCLUDED.password_hash,
    updated_at_utc = NOW();
"@

Write-Host "Inserting user into database..." -NoNewline

$result = $sqlInsert | docker exec -i botpulse-db psql -U botpulse -d botpulse 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host " Done" -ForegroundColor Green
    Write-Host ""
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host "  Admin user created successfully!" -ForegroundColor Green
    Write-Host "=================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "  Username: $UserName" -ForegroundColor White
    Write-Host "  Password: $Password" -ForegroundColor White
    Write-Host "  Email:    $Email" -ForegroundColor White
    Write-Host "  Role:     Administrator" -ForegroundColor White
    Write-Host ""
    Write-Host "  Login via: POST http://localhost:5001/api/v1/auth/login" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host "Error: $result" -ForegroundColor Red
    Write-Host ""
    Write-Host "Ensure PostgreSQL container is running: docker compose up -d postgres" -ForegroundColor Yellow
    exit 1
}
