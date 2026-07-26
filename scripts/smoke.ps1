# BotPulse - Smoke Tests Post-Deploy
# Run: .\scripts\smoke.ps1 -BaseUrl "http://localhost"
param(
    [string]$BaseUrl = "http://localhost",
    [string]$AdminUser = "admin",
    [string]$AdminPass = "Admin@BotPulse2024!"
)

$ErrorActionPreference = "Continue"
$pass = 0; $fail = 0

function Test-Endpoint {
    param([string]$Name, [scriptblock]$ScriptBlock)
    Write-Host "Testing: $Name ..." -NoNewline
    try {
        & $ScriptBlock
        Write-Host " OK" -ForegroundColor Green; $script:pass++
    } catch {
        Write-Host " FAIL: $($_.Exception.Message)" -ForegroundColor Red; $script:fail++
    }
}

Write-Host "`n================================================="
Write-Host "  BotPulse Smoke Tests — $BaseUrl"
Write-Host "=================================================`n"

# 1. Health live
Test-Endpoint "GET /health/live" {
    $r = Invoke-RestMethod "$BaseUrl/health/live" -Method Get
    if ($r.status -ne "Healthy" -and $null -eq $r) { throw "Not healthy" }
}

# 2. Health ready
Test-Endpoint "GET /health/ready" {
    $r = Invoke-RestMethod "$BaseUrl/health/ready" -Method Get
    if ($null -eq $r) { throw "No response" }
}

# 3. Login
$token = $null
Test-Endpoint "POST /api/v1/auth/login" {
    $body = @{ userName = $AdminUser; password = $AdminPass } | ConvertTo-Json
    $r = Invoke-RestMethod "$BaseUrl/api/v1/auth/login" -Method Post -Body $body -ContentType "application/json"
    $script:token = $r.token
    if (-not $script:token) { throw "No token returned" }
}

if ($token) {
    $headers = @{ Authorization = "Bearer $token" }

    # 4. GET /me
    Test-Endpoint "GET /api/v1/auth/me" {
        $r = Invoke-RestMethod "$BaseUrl/api/v1/auth/me" -Headers $headers
        if (-not $r.userName) { throw "No userName" }
    }

    # 5. GET /robots
    Test-Endpoint "GET /api/v1/robots" {
        $null = Invoke-RestMethod "$BaseUrl/api/v1/robots" -Headers $headers
    }

    # 6. GET /jobs
    Test-Endpoint "GET /api/v1/jobs" {
        $null = Invoke-RestMethod "$BaseUrl/api/v1/jobs" -Headers $headers
    }

    # 7. GET /machines
    Test-Endpoint "GET /api/v1/machines" {
        $null = Invoke-RestMethod "$BaseUrl/api/v1/machines" -Headers $headers
    }
}

Write-Host ""
Write-Host "================================================="
Write-Host "  Results: $pass passed, $fail failed"
Write-Host "================================================="
if ($fail -gt 0) { exit 1 }
