# Diagnostic v2: test with hashtable body and verbose output
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

# Show every char code for first 4 chars of secret to detect hidden chars
Write-Host "Secret first 4 char codes:" -ForegroundColor Gray
for ($i = 0; $i -lt [Math]::Min(4, $clientSecret.Length); $i++) {
    Write-Host "  [$i] '$($clientSecret[$i])' = $([int][char]$clientSecret[$i])"
}
Write-Host ""

$tokenUrl = "https://cloud.uipath.com/identity_/connect/token"

# Method 1: hashtable (Invoke-RestMethod encodes it properly)
Write-Host "--- Method 1: hashtable body ---"
$body = @{
    grant_type    = "client_credentials"
    client_id     = $clientId
    client_secret = $clientSecret
}
try {
    $r = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $body -ContentType "application/x-www-form-urlencoded"
    Write-Host "SUCCESS - token: $($r.access_token.Substring(0,20))..." -ForegroundColor Green
    Write-Host "expires_in: $($r.expires_in)"
} catch {
    $resp = $_.Exception.Response
    if ($resp) {
        Write-Host "HTTP $([int]$resp.StatusCode)" -ForegroundColor Red
        try {
            $stream = $resp.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            Write-Host "Body: $($reader.ReadToEnd())"
        } catch {}
    } else {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
# Method 2: URL-encoded string (same as original script)
Write-Host "--- Method 2: URL-encoded string ---"
$enc = [System.Web.HttpUtility]
Add-Type -AssemblyName System.Web
$encodedId     = [System.Web.HttpUtility]::UrlEncode($clientId)
$encodedSecret = [System.Web.HttpUtility]::UrlEncode($clientSecret)
$formBody = "grant_type=client_credentials&client_id=$encodedId&client_secret=$encodedSecret"
Write-Host "Form body (first 80 chars): $($formBody.Substring(0,[Math]::Min(80,$formBody.Length)))"
try {
    $r = Invoke-RestMethod -Uri $tokenUrl -Method Post -Body $formBody -ContentType "application/x-www-form-urlencoded"
    Write-Host "SUCCESS - token: $($r.access_token.Substring(0,20))..." -ForegroundColor Green
} catch {
    $resp = $_.Exception.Response
    if ($resp) {
        Write-Host "HTTP $([int]$resp.StatusCode)" -ForegroundColor Red
        try {
            $stream = $resp.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($stream)
            Write-Host "Body: $($reader.ReadToEnd())"
        } catch {}
    } else {
        Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    }
}
