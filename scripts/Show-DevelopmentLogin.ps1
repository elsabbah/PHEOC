$ErrorActionPreference='Stop'
$secretPath=Join-Path $env:APPDATA 'Microsoft\UserSecrets\PHEOC-OMS-PHEOC2-Development\secrets.json'
$settings=Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
# Explicit local operator utility. It prints only the development web login,
# never SQL credentials or integration keys. Do not capture this output in logs.
Write-Host ('Username: '+$settings.'Bootstrap:UserName')
Write-Host ('Password: '+$settings.'Bootstrap:Password')
