# Diagnostic: test raw token request with full error body
param([string]$EnvFile = ".env")

$envPath = Join-Path (Split-Path $PSScriptRoot -Parent) $EnvFile
$envVars = @{}
Get-Content $envPath | ForEach-Object {
    if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
        $envVars[$Matches[1].Trim()] = $Matches[2].Trim()
    }
}

$clientId     = $envVars["UiPath__ClientId"]
$clientSecret = $envVars["UiPath__ClientSecret"]

Write-Host "ClientId     : $clientId"
Write-Host "Secret length: $($clientSecret.Length)"
Write-Host "Secret (raw) : $($clientSecret.Substring(0,[Math]::Min(8,$clientSecret.Length)))..."
Write-Host ""

$tokenUrl = "https://cloud.uipath.com/identity_/connect/token"
Write-Host "Token URL: $tokenUrl"
Write-Host ""

$encodedSecret   = [System.Uri]::EscapeDataString($clientSecret)
$encodedClientId = [System.Uri]::EscapeDataString($clientId)
$formBody = "grant_type=client_credentials&client_id=$encodedClientId&client_secret=$encodedSecret"

Write-Host "Sending POST..."
try {
    $r = Invoke-WebRequest -Uri $tokenUrl -Method Post `
        -Body $formBody `
        -ContentType "application/x-www-form-urlencoded" `
        -ErrorAction Stop
    Write-Host "HTTP $($r.StatusCode) $($r.StatusDescription)" -ForegroundColor Green
    Write-Host $r.Content
} catch {
    $resp = $_.Exception.Response
    if ($resp) {
        Write-Host "HTTP $([int]$resp.StatusCode) $($resp.StatusDescription)" -ForegroundColor Red
        try {
            $stream = $resp.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            $body   = $reader.ReadToEnd()
            Write-Host "Response body:" -ForegroundColor Yellow
            Write-Host $body
        } catch {
            Write-Host "(could not read response body: $_)"
        }
    } else {
        Write-Host "No HTTP response: $($_.Exception.Message)" -ForegroundColor Red
    }
}
