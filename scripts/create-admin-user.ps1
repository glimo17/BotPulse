# BotPulse - Create Initial Admin User
# Local: .\scripts\create-admin-user.ps1
# Remote: .\scripts\create-admin-user.ps1 -ConnectionString "postgresql://user:pass@host:5432/db"
param(
    [string]$UserName = "admin",
    [string]$Email = "admin@botpulse.local",
    [string]$Password = "Admin@BotPulse2024!",
    [string]$EnvFile = ".env",
    [string]$ConnectionString = ""
)

Write-Host ""
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host "  BotPulse - Create Initial Admin User" -ForegroundColor Cyan
Write-Host "=================================================" -ForegroundColor Cyan
Write-Host ""

# Determine connection string
$connStr = ""

if ($ConnectionString -ne "") {
    # Parse postgresql:// URL into Npgsql format
    if ($ConnectionString -match '^postgresql://([^:]+):([^@]+)@([^:/]+)(?::(\d+))?/(.+)$') {
        $pgUser = $Matches[1]
        $pgPass = $Matches[2]
        $pgHost = $Matches[3]
        $pgPort = if ($Matches[4]) { $Matches[4] } else { "5432" }
        $pgDb   = $Matches[5]
        $connStr = "Host=$pgHost;Port=$pgPort;Database=$pgDb;Username=$pgUser;Password=$pgPass;SSL Mode=Require;Trust Server Certificate=true"
    } else {
        # Already in Npgsql format
        $connStr = $ConnectionString
    }
} else {
    # Load from .env
    $envPath = Join-Path (Split-Path $PSScriptRoot -Parent) $EnvFile
    if (-not (Test-Path $envPath)) {
        Write-Host "ERROR: .env not found. Use -ConnectionString parameter or create .env file." -ForegroundColor Red
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
        $connStr = "Host=localhost;Port=5433;Database=botpulse;Username=botpulse;Password=$dbPassword"
    }
}

Write-Host "Database: $($connStr -replace 'Password=[^;]+', 'Password=***')" -ForegroundColor Gray
Write-Host "UserName: $UserName" -ForegroundColor Gray
Write-Host "Email:    $Email" -ForegroundColor Gray
Write-Host ""

# Generate Argon2id hash and insert user via a small inline dotnet project
$tempDir = Join-Path $env:TEMP "botpulse-admin"
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
    <PackageReference Include="Npgsql" Version="8.0.3" />
  </ItemGroup>
</Project>
"@ | Set-Content "$tempDir/AdminCreate.csproj"

@"
using System.Security.Cryptography;
using Konscious.Security.Cryptography;
using Npgsql;

var password = args[0];
var connStr  = args[1];
var userName = args[2];
var email    = args[3];

// Generate Argon2id hash
var salt = RandomNumberGenerator.GetBytes(16);
using var argon2 = new Argon2id(System.Text.Encoding.UTF8.GetBytes(password))
{
    Salt = salt,
    DegreeOfParallelism = 1,
    MemorySize = 65536,
    Iterations = 3,
};
var hash = argon2.GetBytes(32);
var passwordHash = `$"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";

// Insert into DB
await using var conn = new NpgsqlConnection(connStr);
await conn.OpenAsync();

var sql = @"
    INSERT INTO users (id, external_id, user_name, email, role, auth_provider, password_hash, is_active, created_at_utc, updated_at_utc)
    VALUES (gen_random_uuid(), @extId, @userName, @email, 'Administrator', 'Local', @hash, true, NOW(), NOW())
    ON CONFLICT (auth_provider, external_id) DO UPDATE
    SET password_hash = EXCLUDED.password_hash, updated_at_utc = NOW()";

await using var cmd = new NpgsqlCommand(sql, conn);
cmd.Parameters.AddWithValue("extId",    `$"admin-local-{Guid.NewGuid():N}");
cmd.Parameters.AddWithValue("userName", userName);
cmd.Parameters.AddWithValue("email",    email);
cmd.Parameters.AddWithValue("hash",     passwordHash);
await cmd.ExecuteNonQueryAsync();

Console.WriteLine("OK");
"@ | Set-Content "$tempDir/Program.cs"

Write-Host "Generating hash and inserting user..." -NoNewline

Push-Location $tempDir
try {
    $null = & dotnet restore --verbosity quiet 2>&1
    $result = & dotnet run --verbosity quiet -- $Password $connStr $UserName $Email 2>&1
} finally {
    Pop-Location
    Remove-Item -Recurse -Force $tempDir -ErrorAction SilentlyContinue
}

if ($result -contains "OK" -or $result -match "OK") {
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
} else {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host "Output: $result" -ForegroundColor Red
    exit 1
}
