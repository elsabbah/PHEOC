param(
    [string]$Server = 'DESKTOP-HP\MSSQLSERVER2016',
    [string]$AdminUser = 'sa',
    [Security.SecureString]$SqlAdminPassword,
    [string]$AdministratorName = 'pheoc.admin',
    [Security.SecureString]$AdministratorPassword,
    [string]$RequirementsFile
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
Add-Type -AssemblyName System.Data
function Plain([Security.SecureString]$value) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($value)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) } finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}
if ($RequirementsFile) {
    $source = [IO.File]::ReadAllText($RequirementsFile)
    $sqlPassword = [regex]::Match($source, '(?m)^Password:\s*(.+)').Groups[1].Value.Trim()
} else {
    if (-not $SqlAdminPassword) { $SqlAdminPassword = Read-Host 'SQL administrator password' -AsSecureString }
    $sqlPassword = Plain $SqlAdminPassword
}
if ($AdministratorPassword) { $adminPassword = Plain $AdministratorPassword }
else { $adminPassword = 'Ph!9aA' + [Guid]::NewGuid().ToString('N') }
$cs = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$cs['Data Source']=$Server; $cs['Initial Catalog']='master'; $cs['User ID']=$AdminUser; $cs['Password']=$sqlPassword; $cs['Encrypt']=$false; $cs['Connect Timeout']=10
$connection=New-Object System.Data.SqlClient.SqlConnection($cs.ConnectionString)
function Sql([string]$query) { $command=$connection.CreateCommand(); $command.CommandTimeout=60; $command.CommandText=$query; try { $command.ExecuteNonQuery() | Out-Null } finally { $command.Dispose() } }
try {
    $connection.Open()
    $check=$connection.CreateCommand(); $check.CommandText="SELECT DB_ID(N'PHEOC_OMS')"; $exists=$check.ExecuteScalar(); $check.Dispose()
    if ($exists -is [DBNull]) { Sql "CREATE DATABASE [PHEOC_OMS]"; Sql 'ALTER DATABASE [PHEOC_OMS] SET COMPATIBILITY_LEVEL=130' }
    $connection.ChangeDatabase('PHEOC_OMS')
    $check=$connection.CreateCommand(); $check.CommandText="SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0"; $tables=[int]$check.ExecuteScalar(); $check.Dispose()
    if($tables -gt 0) {
        $check=$connection.CreateCommand(); $check.CommandText="SELECT COUNT(*) FROM sys.tables WHERE name=N'__EFMigrationsHistory'"; $known=[int]$check.ExecuteScalar(); $check.Dispose()
        if($known -eq 0){throw 'Existing database contains an unknown schema. No migrations applied.'}
    }
    $secretPath=Join-Path $env:APPDATA 'Microsoft\UserSecrets\PHEOC-OMS-PHEOC2-Development\secrets.json'
    $existingSecrets=if(Test-Path -LiteralPath $secretPath){Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json}else{$null}
    $runtimePassword='Ph!8bB'+[Guid]::NewGuid().ToString('N')
    $check=$connection.CreateCommand(); $check.CommandText="SELECT COUNT(*) FROM sys.server_principals WHERE name=N'pheoc_oms_app'"; $loginExists=[int]$check.ExecuteScalar(); $check.Dispose()
    if($loginExists -eq 0) { Sql ("CREATE LOGIN [pheoc_oms_app] WITH PASSWORD=N'"+$runtimePassword+"', CHECK_POLICY=ON") }
    elseif($existingSecrets -and $existingSecrets.'ConnectionStrings:Oms') {
        $oldCs=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($existingSecrets.'ConnectionStrings:Oms'); $runtimePassword=$oldCs['Password']
    } else { throw 'Runtime login already exists and no saved development connection is available. Refusing to reset its password.' }
    $cs['Initial Catalog']='PHEOC_OMS'; $env:ConnectionStrings__Oms=$cs.ConnectionString
    $env:Bootstrap__UserName=$AdministratorName; $env:Bootstrap__Password=$adminPassword; $env:ASPNETCORE_ENVIRONMENT='Development'
    dotnet run --no-build --project src/PHEOC.OMS.Web -- --initialize-database
    if($LASTEXITCODE -ne 0){throw 'Database initialization failed.'}
    Sql "IF USER_ID(N'pheoc_oms_app') IS NULL CREATE USER [pheoc_oms_app] FOR LOGIN [pheoc_oms_app]; IF DATABASE_PRINCIPAL_ID(N'pheoc_runtime') IS NULL CREATE ROLE [pheoc_runtime]; ALTER ROLE [pheoc_runtime] ADD MEMBER [pheoc_oms_app]; GRANT SELECT, INSERT, UPDATE ON SCHEMA::dbo TO [pheoc_runtime];"
    foreach($table in @('AuditLogs','EventTimeline','IncidentDecisions','InventoryTransactions','RiskAssessments','RiskPolicies','ActivationDecisions','AARReviews','LaboratoryResults','SampleTransfers','InboxMessages','EventMessages','KpiDateRecords','EarlyResponseMilestones','ClosurePolicies','SlaDefinitions','SlaEscalations')) { Sql ("DENY UPDATE, DELETE ON OBJECT::dbo.["+$table+"] TO [pheoc_runtime]") }
    foreach($table in @('AspNetUserClaims','AspNetUserRoles','AspNetUserLogins','AspNetUserTokens','AspNetRoleClaims')) { Sql ("GRANT DELETE ON OBJECT::dbo.["+$table+"] TO [pheoc_runtime]") }
    $cs['User ID']='pheoc_oms_app'; $cs['Password']=$runtimePassword
    $secrets=@{ 'ConnectionStrings:Oms'=$cs.ConnectionString; 'Bootstrap:UserName'=$AdministratorName; 'Bootstrap:Password'=$adminPassword; 'Integration:Ebs:ApiKey'=([Guid]::NewGuid().ToString('N')+[Guid]::NewGuid().ToString('N')) }
    if($existingSecrets){foreach($p in $existingSecrets.PSObject.Properties){if(-not $secrets.ContainsKey($p.Name)){$secrets[$p.Name]=$p.Value}}; $secrets['Bootstrap:Password']=$existingSecrets.'Bootstrap:Password'; $secrets['Integration:Ebs:ApiKey']=$existingSecrets.'Integration:Ebs:ApiKey'}
    $start=New-Object Diagnostics.ProcessStartInfo; $start.FileName='dotnet'; $start.Arguments='user-secrets set --project src/PHEOC.OMS.Web'; $start.UseShellExecute=$false; $start.RedirectStandardInput=$true; $start.RedirectStandardOutput=$true; $start.RedirectStandardError=$true; $start.WorkingDirectory=$projectRoot
    $process=[Diagnostics.Process]::Start($start); $process.StandardInput.WriteLine(($secrets|ConvertTo-Json -Compress)); $process.StandardInput.Close(); $output=$process.StandardOutput.ReadToEnd(); $errorOutput=$process.StandardError.ReadToEnd(); $process.WaitForExit()
    if($process.ExitCode -ne 0){throw 'Saving User Secrets failed.'}
    Write-Output 'Initialized PHEOC_OMS on SQL Server; restricted runtime login configured; credentials stored in local .NET User Secrets.'
    Write-Output ('Administrator account: '+$AdministratorName)
} finally {
    $connection.Dispose()
    Remove-Item Env:\ConnectionStrings__Oms -ErrorAction SilentlyContinue
    Remove-Item Env:\Bootstrap__Password -ErrorAction SilentlyContinue
}
