param([switch]$Apply)
$ErrorActionPreference='Stop'
$root='D:\SPC'
$cfg=Get-Content "$root/release/test/backend/appsettings.json" -Raw|ConvertFrom-Json
$b=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($cfg.ConnectionStrings.SqlServer)
if($b.DataSource -ne '172.16.110.16' -or $b.InitialCatalog -ne 'PMR_SPC_TEST' -or $cfg.AppEnvironment -ne 'test'){throw 'Unexpected target'}
$prod=Get-Content "$root/release/production/backend/appsettings.json" -Raw|ConvertFrom-Json
$pb=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($prod.ConnectionStrings.SqlServer)
if($pb.DataSource -ne $b.DataSource -or $pb.InitialCatalog -ne 'PMR_SPC_2026'){throw 'Unexpected source'}
$cn=[System.Data.SqlClient.SqlConnection]::new($b.ConnectionString)
$cn.Open()
function Query([string]$sql){
 $q=$cn.CreateCommand();$q.CommandTimeout=180;$q.CommandText=$sql
 $a=[System.Data.SqlClient.SqlDataAdapter]::new($q);$t=[System.Data.DataTable]::new();[void]$a.Fill($t);return ,$t
}
function Exec([string]$sql){$q=$cn.CreateCommand();$q.CommandTimeout=300;$q.CommandText=$sql;[void]$q.ExecuteNonQuery()}
function Ident([string]$s){return '['+$s.Replace(']',']]')+']'}
$excluded=@('__EFMigrationsHistory','Operators','SpcReportSchedules','SpcAlertNotificationSettings','EquipmentPointMappings','ChameleonSourceSettings')
$tables=Query @'
SELECT t.name FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id
JOIN PMR_SPC_2026.sys.tables p ON p.name=t.name JOIN PMR_SPC_2026.sys.schemas ps ON ps.schema_id=p.schema_id
WHERE s.name='dbo' AND ps.name='dbo' ORDER BY t.name
'@
$names=@($tables.Rows|ForEach-Object {$_.name}|Where-Object {$_ -notin $excluded})
$all=Query "SELECT name FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') ORDER BY name"
$keep=@($all.Rows|ForEach-Object {$_.name}|Where-Object {$_ -notin $names})
$lines=[System.Collections.Generic.List[string]]::new()
$lines.Add("SET XACT_ABORT ON; SET LOCK_TIMEOUT 30000; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; BEGIN TRY BEGIN TRANSACTION;")
# Acquire shared locks on all source tables before copying, giving one consistent committed source view.
foreach($n in $names){$i=Ident $n;$lines.Add("DECLARE @lock_$n bigint; SELECT @lock_$n=COUNT_BIG(*) FROM PMR_SPC_2026.dbo.$i WITH(TABLOCK,HOLDLOCK);")}
foreach($n in $keep){$i=Ident $n;$lines.Add("DECLARE @keep_$n varbinary(32); SELECT @keep_$n=HASHBYTES('SHA2_256',(SELECT * FROM dbo.$i FOR JSON PATH,INCLUDE_NULL_VALUES));")}
foreach($n in $names){$i=Ident $n;$lines.Add("ALTER TABLE dbo.$i NOCHECK CONSTRAINT ALL;")}
foreach($n in $names){
 $i=Ident $n
 $cols=Query "SELECT c.name,c.is_identity FROM sys.columns c JOIN PMR_SPC_2026.sys.columns p ON p.name=c.name AND p.object_id=(SELECT object_id FROM PMR_SPC_2026.sys.tables WHERE name=N'$n' AND schema_id=1) WHERE c.object_id=OBJECT_ID(N'dbo.$n') AND c.is_computed=0 AND c.system_type_id<>189 ORDER BY c.column_id"
 $list=(@($cols.Rows|ForEach-Object {Ident $_.name}) -join ',')
 if(!$list){throw "No columns for $n"}
 $identity=@($cols.Rows|Where-Object {$_.is_identity}).Count -gt 0
 $lines.Add("DELETE FROM dbo.$i;")
 if($identity){$lines.Add("SET IDENTITY_INSERT dbo.$i ON;")}
 $lines.Add("INSERT INTO dbo.$i ($list) SELECT $list FROM PMR_SPC_2026.dbo.$i;")
 if($identity){$lines.Add("SET IDENTITY_INSERT dbo.$i OFF;")}
 $lines.Add("IF (SELECT COUNT_BIG(*) FROM dbo.$i)<>(SELECT COUNT_BIG(*) FROM PMR_SPC_2026.dbo.$i) OR EXISTS(SELECT $list FROM dbo.$i EXCEPT SELECT $list FROM PMR_SPC_2026.dbo.$i) OR EXISTS(SELECT $list FROM PMR_SPC_2026.dbo.$i EXCEPT SELECT $list FROM dbo.$i) THROW 51000,'Data mismatch: $n',1;")
}
foreach($n in $names){$i=Ident $n;$lines.Add("ALTER TABLE dbo.$i WITH CHECK CHECK CONSTRAINT ALL;")}
foreach($n in $keep){$i=Ident $n;$lines.Add("IF @keep_$n<>HASHBYTES('SHA2_256',(SELECT * FROM dbo.$i FOR JSON PATH,INCLUDE_NULL_VALUES)) THROW 51000,'Preserved table changed: $n',1;")}
$lines.Add("IF EXISTS(SELECT 1 FROM dbo.CalibrationInstruments c LEFT JOIN dbo.Operators o ON o.Id=c.CustodianOperatorId WHERE o.Id IS NULL) THROW 51000,'Invalid custodian',1;")
$lines.Add("IF EXISTS(SELECT 1 FROM dbo.VariableMeasurements WHERE SamplingStage<>'GENERAL') THROW 51000,'Unexpected sampling stage',1;")
$lines.Add("COMMIT; END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;")
$folder="$root/release-staging/data-refresh-20260917"
New-Item -ItemType Directory -Path $folder -Force|Out-Null
$lines -join "`r`n" | Set-Content "$folder/refresh.sql"
@{Synced=$names;Preserved=$keep}|ConvertTo-Json -Depth 3|Set-Content "$folder/plan.json"
Write-Output "Plan: sync $($names.Count) tables; preserve $($keep.Count) tables"
if(!$Apply){$cn.Close();return}
$offline="$root/release/test/backend/app_offline.htm"
if(Test-Path $offline){throw 'Existing maintenance file; refusing to replace'}
try {
 Set-Content $offline '<html>SPC test data refresh in progress.</html>'
 Start-Sleep -Seconds 12
 $stamp=Get-Date -Format yyyyMMdd-HHmmss
 $path=(Query "SELECT CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000)) P").Rows[0].P.TrimEnd('\')+"\PMR_SPC_TEST_before_refresh_$stamp.bak"
 $safe=$path.Replace("'","''")
 Exec "BACKUP DATABASE [PMR_SPC_TEST] TO DISK=N'$safe' WITH COPY_ONLY,CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$safe' WITH CHECKSUM;"
 $path|Set-Content "$folder/backup-path.txt"
 Write-Output "Backup verified: $path"
 Exec ($lines -join "`r`n")
 $counts=Query "SELECT 'VariableMeasurements' TableName,COUNT_BIG(*) Rows FROM dbo.VariableMeasurements UNION ALL SELECT 'CalibrationInstruments',COUNT_BIG(*) FROM dbo.CalibrationInstruments UNION ALL SELECT 'Operators',COUNT_BIG(*) FROM dbo.Operators UNION ALL SELECT '__EFMigrationsHistory',COUNT_BIG(*) FROM dbo.__EFMigrationsHistory"
 $counts|Format-Table|Out-String|Tee-Object -FilePath "$folder/result.txt"
 Write-Output 'COMMITTED: exact common-column comparison, constraints, and SHA256 preservation checks passed.'
} finally {if(Test-Path $offline){Remove-Item -LiteralPath $offline};$cn.Close()}
