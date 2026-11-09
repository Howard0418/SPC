$ErrorActionPreference = 'Stop'
$settings = Get-Content -LiteralPath 'D:\SPC\release\test\backend\appsettings.json' -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'Unexpected target: only PMR_SPC_TEST is allowed.'
}
$parts = @{}; foreach ($part in $settings.ConnectionStrings.SqlServer -split ';') { if ($part -match '^\s*([^=]+)=(.*)$') { $parts[$matches[1].Trim().ToLowerInvariant()]=$matches[2].Trim() } }
$query="SET NOCOUNT ON; SELECT CONCAT((SELECT COUNT_BIG(*) FROM dbo.AuditEvents),'|',(SELECT COUNT_BIG(*) FROM dbo.AuditArchiveBatches),'|',(SELECT COUNT_BIG(*) FROM dbo.AuditEventArchives),'|',(SELECT COUNT_BIG(*) FROM dbo.AuditLegalHolds),'|',CONVERT(varchar(64),HASHBYTES('SHA2_256',CONCAT((SELECT COUNT_BIG(*) FROM dbo.AuditEvents),'|',COALESCE((SELECT MIN(Id) FROM dbo.AuditEvents),0),'|',COALESCE((SELECT MAX(Id) FROM dbo.AuditEvents),0),'|',COALESCE(CONVERT(varchar(33),(SELECT MIN(OccurredAtUtc) FROM dbo.AuditEvents),126),''),'|',COALESCE(CONVERT(varchar(33),(SELECT MAX(OccurredAtUtc) FROM dbo.AuditEvents),126),''))),2),'|',(SELECT COUNT(*) FROM dbo.__EFMigrationsHistory WHERE MigrationId='20261109024438_AddAuditRetentionArchive'),'|',(SELECT COUNT(*) FROM sys.triggers WHERE name IN ('TR_AuditEventArchives_Immutable','TR_AuditArchiveBatches_DeleteGuard')));"
$server=if($parts['server']){$parts['server']}else{$parts['data source']}; $user=if($parts['user id']){$parts['user id']}else{$parts['uid']}; $password=if($parts['password']){$parts['password']}else{$parts['pwd']}
$result=& sqlcmd -S $server -d PMR_SPC_TEST -U $user -P $password -C -b -h -1 -W -Q $query
if ($LASTEXITCODE -ne 0) { throw 'Verification query failed.' }; $v=($result | Where-Object { $_ -match '^\d+\|' } | Select-Object -Last 1) -split '\|'
[pscustomobject]@{Database='PMR_SPC_TEST';AuditCount=[long]$v[0];BatchCount=[long]$v[1];ArchiveCount=[long]$v[2];HoldCount=[long]$v[3];AuditFingerprint=$v[4];MigrationCount=[int]$v[5];TriggerCount=[int]$v[6]}
