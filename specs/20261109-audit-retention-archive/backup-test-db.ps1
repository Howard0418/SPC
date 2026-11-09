$ErrorActionPreference = 'Stop'
$settings = Get-Content -LiteralPath 'D:\SPC\backend\MesSpc.Api\appsettings.json' -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'Unexpected target: only PMR_SPC_TEST is allowed.'
}
$parts = @{}; foreach ($part in $settings.ConnectionStrings.SqlServer -split ';') { if ($part -match '^\s*([^=]+)=(.*)$') { $parts[$matches[1].Trim().ToLowerInvariant()]=$matches[2].Trim() } }
$server=if($parts['server']){$parts['server']}else{$parts['data source']}; $user=if($parts['user id']){$parts['user id']}else{$parts['uid']}; $password=if($parts['password']){$parts['password']}else{$parts['pwd']}; $backup='C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_TEST_audit_retention_20261109.bak'
$query="SET NOCOUNT ON; BACKUP DATABASE [PMR_SPC_TEST] TO DISK=N'$backup' WITH COPY_ONLY, FORMAT, CHECKSUM, NAME=N'SPC audit retention pre-migration'; RESTORE VERIFYONLY FROM DISK=N'$backup' WITH CHECKSUM; SELECT CONCAT(COUNT_BIG(*),'|',CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT(COUNT_BIG(*),'|',COALESCE(MIN(Id),0),'|',COALESCE(MAX(Id),0),'|',COALESCE(CONVERT(varchar(33),MIN(OccurredAtUtc),126),''),'|',COALESCE(CONVERT(varchar(33),MAX(OccurredAtUtc),126),''))),2)) FROM dbo.AuditEvents;"
$result = & sqlcmd -S $server -d PMR_SPC_TEST -U $user -P $password -C -b -h -1 -W -Q $query
if ($LASTEXITCODE -ne 0) { throw ('Backup or VERIFYONLY failed: ' + ($result -join ' ')) }
$summary = ($result | Where-Object { $_ -match '^\d+\|' } | Select-Object -Last 1) -split '\|'
[pscustomobject]@{Database='PMR_SPC_TEST';BackupVerified=$true;AuditCount=[long]$summary[0];AuditFingerprint=$summary[1];BackupFile=$backup}
