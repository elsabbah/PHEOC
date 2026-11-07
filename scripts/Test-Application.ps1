param([switch]$Browser,[string]$Url='http://localhost:5268')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path $PSScriptRoot -Parent)
$settings=Get-Content -LiteralPath (Join-Path $env:APPDATA 'Microsoft\UserSecrets\PHEOC-OMS-PHEOC2-Development\secrets.json') -Raw | ConvertFrom-Json
try{
 $env:PHEOC_TEST_SQL=$settings.'ConnectionStrings:Oms';$env:PHEOC_EBS_SQL=$settings.'ConnectionStrings:EgyptEbsReader'
 dotnet test PHEOC.OMS.sln --logger 'console;verbosity=minimal'
 if($LASTEXITCODE -ne 0){throw 'Application tests failed.'}
 if($Browser){$env:PHEOC_URL=$Url;node scripts/browser-check.mjs;if($LASTEXITCODE -ne 0){throw 'Browser checks failed.'}}
}finally{Remove-Item Env:\PHEOC_TEST_SQL,Env:\PHEOC_EBS_SQL -ErrorAction SilentlyContinue}
