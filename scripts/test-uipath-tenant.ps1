# BotPulse - UiPath Tenant Connection Test
# Run from repository root: .\scripts\test-uipath-tenant.ps1

param(
    [string]$EnvFile = ".env"
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  BotPulse - UiPath Tenant Connection Test" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""

# --- Load .env file ---
$envPath = Join-Path (Split-Path $PSScriptRoot -Parent) $EnvFile
if (-not (Test-Path $envPath)) {
    Write-Host "ERROR: .env file not found at $envPath" -ForegroundColor Red
    Write-Host "Copy .env.example to .env and fill in your credentials." -ForegroundColor Yellow
    exit 1
}

$envVars = @{}
Get-Content $envPath | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
        $key   = $Matches[1].Trim()
        $value = $Matches[2].Trim()
        $envVars[$key] = $value
    }
}

$baseUrl      = $envVars["UiPath__BaseUrl"]
$tenant       = $envVars["UiPath__Tenant"]
$clientId     = $envVars["UiPath__ClientId"]
$clientSecret = $envVars["UiPath__ClientSecret"]

if (-not $baseUrl -or -not $clientId -or -not $clientSecret) {
    Write-Host "ERROR: Missing UiPath credentials in .env" -ForegroundColor Red
    Write-Host "Required: UiPath__BaseUrl, UiPath__Tenant, UiPath__ClientId, UiPath__ClientSecret" -ForegroundColor Yellow
    exit 1
}

Write-Host "Tenant:   $tenant"   -ForegroundColor Gray
Write-Host "Base URL: $baseUrl"  -ForegroundColor Gray
Write-Host "ClientId: $clientId" -ForegroundColor Gray
Write-Host ""

$results      = @()
$script:token = $null

# --- Helper: run a named test block ---
function Test-Endpoint {
    param(
        [string]   $Name,
        [scriptblock] $ScriptBlock
    )
    Write-Host "Testing: $Name ..." -NoNewline
    try {
        $count = & $ScriptBlock
        Write-Host " OK ($count items)" -ForegroundColor Green
        $script:results += [PSCustomObject]@{ Test = $Name; Status = "PASS"; Details = "$count items" }
        return $true
    }
    catch {
        $msg = $_.Exception.Message
        Write-Host " FAIL" -ForegroundColor Red
        Write-Host "  -> $msg" -ForegroundColor DarkRed
        $script:results += [PSCustomObject]@{ Test = $Name; Status = "FAIL"; Details = $msg }
        return $false
    }
}

# --- Helper: call an OData endpoint and return item count ---
function Invoke-OData {
    param(
        [string] $Token,
        [string] $Path,
        [int]    $Top = 10
    )
    $url = "$baseUrl/$tenant/orchestrator_/$Path`?`$top=$Top"
    $headers = @{
        "Authorization"       = "Bearer $Token"
        "X-UIPATH-TenantName" = $tenant
    }
    $response = Invoke-RestMethod -Uri $url -Headers $headers -Method Get -ContentType "application/json"
    return ($response.value | Measure-Object).Count
}

# ---------------------------------------------------------------------------
# Step 1: OAuth2 Token
# ---------------------------------------------------------------------------
Write-Host "Step 1: OAuth2 Authentication" -ForegroundColor White

$tokenSuccess = Test-Endpoint "OAuth2 Token" {
    $tokenUrl = "$baseUrl/identity_/connect/token"
    $body = @{
        grant_type    = "client_credentials"
        client_id     = $clientId
        client_secret = $clientSecret
    }
    $tokenResponse = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $body `
        -ContentType "application/x-www-form-urlencoded"

    if (-not $tokenResponse.access_token) { throw "No access_token in response" }

    $script:token = $tokenResponse.access_token
    Write-Host ""
    Write-Host "  Token expires in: $($tokenResponse.expires_in)s" -ForegroundColor Gray
    return 1
}

if (-not $tokenSuccess -or -not $script:token) {
    Write-Host ""
    Write-Host "Cannot proceed without token. Check credentials." -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------
# Step 2: Orchestrator Endpoints
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "Step 2: Orchestrator Endpoints" -ForegroundColor White

$t = $script:token   # local copy so closures capture it cleanly

Test-Endpoint "GET odata/Robots"             { Invoke-OData $t "odata/Robots" }
Test-Endpoint "GET odata/Jobs (top 5)"       { Invoke-OData $t "odata/Jobs" 5 }
Test-Endpoint "GET odata/QueueDefinitions"   { Invoke-OData $t "odata/QueueDefinitions" }
Test-Endpoint "GET odata/Releases"           { Invoke-OData $t "odata/Releases" }
Test-Endpoint "GET odata/Machines"           { Invoke-OData $t "odata/Machines" }
Test-Endpoint "GET odata/Assets"             { Invoke-OData $t "odata/Assets" }
Test-Endpoint "GET odata/RobotLogs (top 5)"  { Invoke-OData $t "odata/RobotLogs" 5 }
Test-Endpoint "GET odata/Folders"            { Invoke-OData $t "odata/Folders" }

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  RESULTS SUMMARY" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

$passed = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$failed = ($results | Where-Object { $_.Status -eq "FAIL" }).Count

foreach ($r in $results) {
    $color = if ($r.Status -eq "PASS") { "Green" } else { "Red" }
    $icon  = if ($r.Status -eq "PASS") { "✅" }    else { "❌" }
    Write-Host "  $icon $($r.Test)" -ForegroundColor $color
    if ($r.Status -eq "FAIL") {
        Write-Host "        $($r.Details)" -ForegroundColor DarkRed
    }
}

Write-Host ""
$summaryColor = if ($failed -eq 0) { "Green" } else { "Yellow" }
Write-Host "  Passed: $passed / $($results.Count)" -ForegroundColor $summaryColor

if ($failed -gt 0) {
    Write-Host ""
    Write-Host "  HINT: If you see 403 errors, check that the corresponding" -ForegroundColor Yellow
    Write-Host "  OAuth2 scope is enabled in UiPath Administration -> External Apps" -ForegroundColor Yellow
    Write-Host "  Scope reference:" -ForegroundColor Yellow
    Write-Host "    odata/Robots           -> OR.Robots.Read"            -ForegroundColor Gray
    Write-Host "    odata/Jobs             -> OR.Jobs.Read"              -ForegroundColor Gray
    Write-Host "    odata/QueueDefinitions -> OR.Queues.Read"            -ForegroundColor Gray
    Write-Host "    odata/Releases         -> OR.Execution.Read"         -ForegroundColor Gray
    Write-Host "    odata/Machines         -> OR.Machines.Read"          -ForegroundColor Gray
    Write-Host "    odata/Assets           -> OR.Assets.Read"            -ForegroundColor Gray
    Write-Host "    odata/RobotLogs        -> OR.Robots.Read / OR.Monitoring.Read" -ForegroundColor Gray
    Write-Host "    odata/Folders          -> OR.Folders.Read"           -ForegroundColor Gray
}

Write-Host ""
exit $(if ($failed -eq 0) { 0 } else { 1 })
