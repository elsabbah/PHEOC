param([string]$RequirementsFile,[Security.SecureString]$SqlAdminPassword)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $projectRoot
if($RequirementsFile){$source=[IO.File]::ReadAllText($RequirementsFile);$password=[regex]::Match($source,'(?m)^Password:\s*(.+)').Groups[1].Value.Trim()}
else{if(-not $SqlAdminPassword){$SqlAdminPassword=Read-Host 'SQL administrator password' -AsSecureString};$pointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword);try{$password=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)}finally{[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)}}
Add-Type -AssemblyName System.Data
$secretPath=Join-Path $env:APPDATA 'Microsoft\UserSecrets\PHEOC-OMS-PHEOC2-Development\secrets.json'
$saved=Get-Content -LiteralPath $secretPath -Raw | ConvertFrom-Json
$runtime=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($saved.'ConnectionStrings:Oms')
$admin=New-Object System.Data.SqlClient.SqlConnectionStringBuilder($runtime.ConnectionString)
$admin['Initial Catalog']='EgyptEBS';$admin['User ID']='sa';$admin['Password']=$password
$connection=New-Object System.Data.SqlClient.SqlConnection($admin.ConnectionString)
try{
 $connection.Open();$script=[IO.File]::ReadAllText((Join-Path $projectRoot 'database/002-EgyptEBS-Reader.sql'))
 foreach($batch in [regex]::Split($script,'(?im)^GO\s*$')){if($batch.Trim()){$command=$connection.CreateCommand();$command.CommandText=$batch;$command.ExecuteNonQuery()|Out-Null;$command.Dispose()}}
}finally{$connection.Dispose()}
$runtime['Initial Catalog']='EgyptEBS'
$settings=@{'ConnectionStrings:EgyptEbsReader'=$runtime.ConnectionString}
$start=New-Object Diagnostics.ProcessStartInfo;$start.FileName='dotnet';$start.Arguments='user-secrets set --project src/PHEOC.OMS.Web';$start.WorkingDirectory=$projectRoot;$start.UseShellExecute=$false;$start.RedirectStandardInput=$true;$start.RedirectStandardOutput=$true;$start.RedirectStandardError=$true
$process=[Diagnostics.Process]::Start($start);$process.StandardInput.WriteLine(($settings|ConvertTo-Json -Compress));$process.StandardInput.Close();$null=$process.StandardOutput.ReadToEnd();$null=$process.StandardError.ReadToEnd();$process.WaitForExit();if($process.ExitCode -ne 0){throw 'Could not save reader connection.'}
Write-Output 'EgyptEBS SELECT-only access enabled on the ten approved tables; connection stored in .NET User Secrets.'
