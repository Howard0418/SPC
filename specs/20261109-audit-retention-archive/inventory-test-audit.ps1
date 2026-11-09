param([string]$SettingsPath='D:\SPC\release\test\backend\appsettings.json')
$ErrorActionPreference='Stop'
$settings=Get-Content -LiteralPath $SettingsPath -Raw|ConvertFrom-Json
if($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)'){throw 'Refusing non-test database'}
$connectionString=$settings.ConnectionStrings.SqlServer -replace '(?i)Trust Server Certificate','TrustServerCertificate'
$connection=New-Object System.Data.SqlClient.SqlConnection $connectionString; $connection.Open()
try{
  $command=$connection.CreateCommand();$command.CommandText=@'
SELECT COUNT_BIG(*) TotalCount, MIN(OccurredAtUtc) EarliestUtc, MAX(OccurredAtUtc) LatestUtc,
 SUM(CASE WHEN OccurredAtUtc>=DATEADD(day,-7,SYSUTCDATETIME()) THEN 1 ELSE 0 END) Last7Days,
 SUM(CASE WHEN Category IN ('Navigation','Read') THEN 1 ELSE 0 END) GeneralCount,
 SUM(CASE WHEN Category NOT IN ('Navigation','Read') THEN 1 ELSE 0 END) CriticalCount
FROM dbo.AuditEvents;
SELECT Category,COUNT_BIG(*) EventCount FROM dbo.AuditEvents GROUP BY Category ORDER BY Category;
'@
  $reader=$command.ExecuteReader();$summary=$null;$categories=@()
  if($reader.Read()){$summary=[ordered]@{TotalCount=[Convert]::ToInt64($reader.GetValue(0));EarliestUtc=if($reader.IsDBNull(1)){$null}else{$reader.GetDateTime(1).ToString('o')};LatestUtc=if($reader.IsDBNull(2)){$null}else{$reader.GetDateTime(2).ToString('o')};Last7Days=[Convert]::ToInt64($reader.GetValue(3));GeneralCount=[Convert]::ToInt64($reader.GetValue(4));CriticalCount=[Convert]::ToInt64($reader.GetValue(5))}}
  [void]$reader.NextResult();while($reader.Read()){$categories += [ordered]@{Category=$reader.GetString(0);Count=[Convert]::ToInt64($reader.GetValue(1))}};$reader.Close()
  [ordered]@{Environment='test';Database='PMR_SPC_TEST';Summary=$summary;Categories=$categories}|ConvertTo-Json -Depth 5
}finally{$connection.Dispose()}
