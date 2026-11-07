param([ValidateRange(1,65535)][int]$Port=5268)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
$url="http://localhost:$Port"
if(-not (Get-Command dotnet -ErrorAction SilentlyContinue)){throw 'The .NET SDK is not available. Install the SDK specified in global.json.'}

# Reusing a verified running app avoids a second launch and locked build files.
try {
    $health=Invoke-RestMethod -Uri "$url/health/live" -TimeoutSec 3 -ErrorAction Stop
    $login=Invoke-WebRequest -Uri "$url/Account/Login" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
    if($health.status -eq 'running' -and $login.Content.Contains('PHEOC-OMS')) {
        Write-Host "PHEOC-OMS is already running: $url" -ForegroundColor Green
        Write-Host 'Open this address in your browser. No second instance was started.'
        return
    }
} catch { # No healthy app on this address; continue with a normal launch.
}
$socket=[Net.Sockets.TcpClient]::new()
try {
    $pending=$socket.ConnectAsync('localhost',$Port)
    try { $occupied=$pending.Wait(1000) -and $socket.Connected } catch { $occupied=$false }
    if($occupied){throw "Port $Port is occupied, but PHEOC-OMS did not pass its health check. Check the existing application, or use -Port with a free port."}
} finally {$socket.Dispose()}

Write-Host 'Building PHEOC-OMS. Wait for this step to finish...' -ForegroundColor Cyan
dotnet build src/PHEOC.OMS.Web --nologo -v minimal
if($LASTEXITCODE -ne 0){throw 'Build failed. Review the build errors above; the application was not started.'}
$env:ASPNETCORE_ENVIRONMENT='Development'
Write-Host "Starting PHEOC-OMS: $url" -ForegroundColor Green
Write-Host 'Keep this window open. Press Ctrl+C to stop the application.'
dotnet run --no-build --no-restore --no-launch-profile --project src/PHEOC.OMS.Web --urls $url
if($LASTEXITCODE -ne 0){throw "PHEOC-OMS stopped with exit code $LASTEXITCODE. Review the startup errors above."}
