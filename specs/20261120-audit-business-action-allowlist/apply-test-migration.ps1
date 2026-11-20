$ErrorActionPreference = 'Stop'
$root = 'D:\SPC'
$settingsPath = Join-Path $root 'release\test\backend\appsettings.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$connection = [string]$settings.ConnectionStrings.SqlServer
if ($settings.AppEnvironment -ne 'test' -or $connection -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'Unexpected target: only PMR_SPC_TEST is allowed.'
}
$parts = @{}
foreach ($part in $connection -split ';') {
    if ($part -match '^\s*([^=]+)=(.*)$') { $parts[$matches[1].Trim().ToLowerInvariant()] = $matches[2].Trim() }
}
$server = if ($parts['server']) { $parts['server'] } else { $parts['data source'] }
$user = if ($parts['user id']) { $parts['user id'] } else { $parts['uid'] }
$password = if ($parts['password']) { $parts['password'] } else { $parts['pwd'] }
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = "C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_TEST_business_action_$stamp.bak"
$fingerprintQuery = "SET NOCOUNT ON; SELECT CONCAT(COUNT_BIG(*),'|',COALESCE(MIN(Id),0),'|',COALESCE(MAX(Id),0),'|',COALESCE(CHECKSUM_AGG(BINARY_CHECKSUM(Id,EventCode,Outcome,DetailsJson)),0)) FROM dbo.AuditEvents;"
$before = (& sqlcmd -S $server -d PMR_SPC_TEST -U $user -P $password -C -b -h -1 -W -Q $fingerprintQuery | Where-Object { $_ -match '^\d+\|' } | Select-Object -Last 1).Trim()
$escapedBackup = $backup.Replace("'", "''")
$backupQuery = "BACKUP DATABASE [PMR_SPC_TEST] TO DISK=N'$escapedBackup' WITH COPY_ONLY,FORMAT,CHECKSUM,NAME=N'SPC business action pre-migration';"
$null = & sqlcmd -S $server -d PMR_SPC_TEST -U $user -P $password -C -b -Q $backupQuery
if ($LASTEXITCODE -ne 0) { throw 'COPY_ONLY CHECKSUM backup failed.' }
$metadataQuery = "SET NOCOUNT ON; SELECT TOP 1 CONCAT(CAST(bs.is_copy_only AS int),'|',CAST(bs.has_backup_checksums AS int),'|',CASE WHEN bs.backup_finish_date IS NOT NULL THEN 1 ELSE 0 END) FROM dbo.backupset bs INNER JOIN dbo.backupmediafamily bmf ON bmf.media_set_id=bs.media_set_id WHERE bs.database_name='PMR_SPC_TEST' AND bmf.physical_device_name=N'$escapedBackup' ORDER BY bs.backup_finish_date DESC;"
$backupMetadata = (& sqlcmd -S $server -d msdb -U $user -P $password -C -b -h -1 -W -Q $metadataQuery | Where-Object { $_ -match '^\d\|\d\|\d$' } | Select-Object -Last 1).Trim()
if ($backupMetadata -ne '1|1|1') { throw 'Backup metadata does not confirm COPY_ONLY, CHECKSUM and completion.' }

dotnet ef database update 20261120134446_AllowBusinessActionClientEvent --project (Join-Path $root 'backend\MesSpc.Api\MesSpc.Api.csproj') --startup-project (Join-Path $root 'backend\MesSpc.Api\MesSpc.Api.csproj') --no-build --configuration Release
if ($LASTEXITCODE -ne 0) { throw 'Migration failed.' }

$after = (& sqlcmd -S $server -d PMR_SPC_TEST -U $user -P $password -C -b -h -1 -W -Q $fingerprintQuery | Where-Object { $_ -match '^\d+\|' } | Select-Object -Last 1).Trim()
if ($before -ne $after) { throw 'AuditEvents fingerprint changed during constraint migration.' }
$verifyQuery = "SET NOCOUNT ON; SELECT CONCAT(CASE WHEN EXISTS(SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId='20261120134446_AllowBusinessActionClientEvent') THEN 1 ELSE 0 END,'|',CASE WHEN EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_AuditEvents_ClientEvent' AND is_disabled=0 AND is_not_trusted=0) THEN 1 ELSE 0 END);"
$verified = (& sqlcmd -S $server -d PMR_SPC_TEST -U $user -P $password -C -b -h -1 -W -Q $verifyQuery | Where-Object { $_ -match '^\d\|\d$' } | Select-Object -Last 1).Trim()
if ($verified -ne '1|1') { throw 'Migration history or check constraint verification failed.' }
[pscustomobject]@{ Database='PMR_SPC_TEST'; BackupChecksummed=$true; BackupFile=$backup; RestoreVerifyOnly='BlockedByLeastPrivilege'; AuditFingerprint=$after; Migration='20261120134446_AllowBusinessActionClientEvent'; ConstraintVerified=$true }
