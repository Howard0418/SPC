$ErrorActionPreference='Stop'
$settings=Get-Content -LiteralPath 'D:\SPC\release\test\backend\appsettings.json' -Raw|ConvertFrom-Json
if($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)'){throw 'Only PMR_SPC_TEST is allowed'}
$parts=@{};foreach($part in $settings.ConnectionStrings.SqlServer -split ';'){if($part -match '^\s*([^=]+)=(.*)$'){$parts[$matches[1].Trim().ToLowerInvariant()]=$matches[2].Trim()}}
$server=if($parts['server']){$parts['server']}else{$parts['data source']};$user=if($parts['user id']){$parts['user id']}else{$parts['uid']};$password=if($parts['password']){$parts['password']}else{$parts['pwd']}
$query=@"
SET NOCOUNT ON;
SELECT CONCAT(
 COUNT_BIG(*),'|',SUM(CASE WHEN Environment<>'Test' THEN 1 ELSE 0 END),'|',
 SUM(CASE WHEN LEN(DetailsJson)>4096 OR ISJSON(DetailsJson)<>1 THEN 1 ELSE 0 END),'|',
 SUM(CASE WHEN DetailsJson LIKE '%password%' OR DetailsJson LIKE '%token%' OR DetailsJson LIKE '%authorization%' OR DetailsJson LIKE '%query=%' THEN 1 ELSE 0 END),'|',
 SUM(CASE WHEN EventCode IN ('audit.events.query','audit.events.export') THEN 1 ELSE 0 END),'|',
 SUM(CASE WHEN EventCode IN ('masterdata.record.create','masterdata.record.delete') THEN 1 ELSE 0 END),'|',
 COALESCE(MIN(Id),0),'|',COALESCE(MAX(Id),0)) FROM dbo.AuditEvents;
SELECT CONCAT(COUNT_BIG(*),'|',COALESCE(SUM(CASE WHEN c>1 THEN 1 ELSE 0 END),0)) FROM (SELECT ClientEventId,COUNT_BIG(*) c FROM dbo.AuditEvents WHERE ClientEventId IS NOT NULL GROUP BY ClientEventId) d;
SELECT CONCAT(COUNT_BIG(*),'|',(SELECT COUNT_BIG(*) FROM dbo.AuditArchiveBatches),'|',(SELECT COUNT_BIG(*) FROM dbo.AuditEventArchives),'|',(SELECT COUNT_BIG(*) FROM dbo.AuditLegalHolds)) FROM dbo.AuditEvents WHERE OccurredAtUtc>=DATEADD(day,-7,SYSUTCDATETIME());
"@
$rows=& sqlcmd -S $server -d PMR_SPC_TEST -U $user -P $password -C -b -h -1 -W -Q $query
if($LASTEXITCODE -ne 0){throw 'Audit verification failed'}
$values=@($rows|Where-Object{$_ -match '^\d+\|'})
if($values.Count -ne 3){throw 'Unexpected verification result'}
$a=$values[0]-split '\|';$b=$values[1]-split '\|';$c=$values[2]-split '\|'
[pscustomobject]@{Database='PMR_SPC_TEST';Total=[long]$a[0];WrongEnvironment=[long]$a[1];InvalidDetails=[long]$a[2];SensitiveKeywordHits=[long]$a[3];AuditQueryExport=[long]$a[4];MasterCreateDelete=[long]$a[5];MinId=[long]$a[6];MaxId=[long]$a[7];ClientEventIds=[long]$b[0];DuplicateClientEventIds=[long]$b[1];Last7Days=[long]$c[0];ArchiveBatches=[long]$c[1];ArchivedEvents=[long]$c[2];ActiveHolds=[long]$c[3]}
