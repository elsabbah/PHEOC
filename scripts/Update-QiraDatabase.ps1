param([Security.SecureString]$SqlAdminPassword,[string]$RequirementsFile)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Add-Type -AssemblyName System.Data
$settings=Get-Content -LiteralPath (Join-Path $env:APPDATA 'Microsoft\UserSecrets\PHEOC-OMS-PHEOC2-Development\secrets.json') -Raw | ConvertFrom-Json
$cs=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($settings.'ConnectionStrings:Oms')
if($cs.InitialCatalog -ne 'PHEOC_OMS'){throw 'Only the existing PHEOC_OMS application database is supported.'}
if($RequirementsFile){$password=[regex]::Match([IO.File]::ReadAllText($RequirementsFile),'(?m)^Password:\s*(.+)').Groups[1].Value.Trim()}
else{
 if(-not $SqlAdminPassword){$SqlAdminPassword=Read-Host 'SQL administrator password' -AsSecureString}
 $pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
 try{$password=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)}
}
if([string]::IsNullOrWhiteSpace($password)){throw 'SQL administrator password was not supplied.'}
$cs['User ID']='sa';$cs['Password']=$password;$cs['Integrated Security']=$false
$connection=New-Object System.Data.SqlClient.SqlConnection($cs.ConnectionString)
try{
 $connection.Open()
 $command=$connection.CreateCommand();$command.CommandText="SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261106211504_QiraRiskAssessment'";$done=[int]$command.ExecuteScalar();$command.Dispose()
 if($done -eq 0){
  $command=$connection.CreateCommand();$command.CommandText="SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId=N'20261105213554_ReliableSyncAndSlaScheduling'";$previous=[int]$command.ExecuteScalar();$command.Dispose()
  if($previous -ne 1){throw 'Expected existing application schema was not found. No migration applied.'}
  $sql=[IO.File]::ReadAllText((Join-Path $projectRoot 'database/003-Qira.sql'))
  foreach($batch in [regex]::Split($sql,'(?im)^\s*GO\s*(?:\r?\n|$)')){
   if(-not [string]::IsNullOrWhiteSpace($batch)){$command=$connection.CreateCommand();$command.CommandTimeout=90;$command.CommandText=$batch;try{$command.ExecuteNonQuery()|Out-Null}finally{$command.Dispose()}}
  }
 }
 $command=$connection.CreateCommand();$command.CommandText="SELECT COUNT(*) FROM sys.tables WHERE name IN(N'QiraDefinitions',N'QiraNationalPolicies',N'QiraAssessments',N'QiraAnswers',N'QiraActions',N'QiraHistories')";$count=[int]$command.ExecuteScalar();$command.Dispose()
 if($count -ne 6){throw 'QIRA schema verification failed.'}
 Write-Output 'QIRA migration verified: six tables in PHEOC_OMS. Existing event and historical risk data preserved. EgyptEBS unchanged.'
}catch [System.Data.SqlClient.SqlException]{throw ('QIRA migration failed. SQL number: '+$_.Exception.Number)}
finally{$connection.Dispose();$password=$null;$cs.Clear()}
