$ErrorActionPreference='Stop'
$out='D:\SPC\release-staging\production-compatibility-20260918\rehearsal'
$cfg=[IO.File]::ReadAllText('D:\SPC\release\production\backend\appsettings.json')|ConvertFrom-Json
$sb=[System.Data.SqlClient.SqlConnectionStringBuilder]::new($cfg.ConnectionStrings.SqlServer)
if($sb.DataSource -ne '172.16.110.16' -or $sb.InitialCatalog -ne 'PMR_SPC_2026'){throw 'Unexpected read-only source'}
$source=[System.Data.SqlClient.SqlConnection]::new($sb.ConnectionString)
$target=[System.Data.SqlClient.SqlConnection]::new('Server=localhost;Database=PMR_COMPAT_SPC_20260918;Integrated Security=True;Encrypt=False')
function Query($cn,$sql){$cmd=$cn.CreateCommand();$cmd.CommandTimeout=120;$cmd.CommandText=$sql;$t=[System.Data.DataTable]::new();$t.Load($cmd.ExecuteReader());return ,$t}
function Execute($sql){$cmd=$target.CreateCommand();$cmd.CommandTimeout=120;$cmd.CommandText=$sql;return $cmd.ExecuteNonQuery()}
function DataRows($t){@($t.Rows|ForEach-Object{$r=$_;$o=[ordered]@{};foreach($c in $t.Columns){if($c.ColumnName -ne 'RowVersion'){$o[$c.ColumnName]=if($r[$c] -is [DBNull]){$null}else{$r[$c]}}};[pscustomobject]$o})}
$source.Open();$target.Open()
try{
 $before=Query $source "SELECT v.* FROM dbo.VariableMeasurements v JOIN dbo.PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId JOIN dbo.Machines m ON m.Id=p.MachineId WHERE p.ControlScope='CHEM' AND m.MachineCode IN ('N1','N2') AND v.SamplingPhase IN ('OPEN','CLOSE','MIDDLE') ORDER BY v.Id"
 if($before.Rows.Count -eq 0){throw 'No source fixtures'}
 if((Query $target 'SELECT COUNT(*) AS N FROM dbo.VariableMeasurements').Rows[0].N -ne 0){throw 'Rehearsal fixtures already exist; refusing overwrite'}
 # Recursively collect only parents of the selected measurements. Parent identifiers come from schema, not user input.
 $tables=@{VariableMeasurements=$before};$queue=[Collections.Generic.Queue[string]]::new();$queue.Enqueue('VariableMeasurements')
 $fks=Query $source "SELECT OBJECT_NAME(f.parent_object_id) ParentTable, pc.name ParentColumn, OBJECT_NAME(f.referenced_object_id) RefTable, rc.name RefColumn FROM sys.foreign_key_columns f JOIN sys.columns pc ON pc.object_id=f.parent_object_id AND pc.column_id=f.parent_column_id JOIN sys.columns rc ON rc.object_id=f.referenced_object_id AND rc.column_id=f.referenced_column_id"
 while($queue.Count){$table=$queue.Dequeue();foreach($fk in @($fks.Rows|Where-Object ParentTable -eq $table)){
   foreach($name in @($fk.RefTable,$fk.RefColumn,$fk.ParentColumn)){if($name -notmatch '^\w+$'){throw 'Unsupported schema identifier'}}
   $values=@($tables[$table].Rows|ForEach-Object {$_[$fk.ParentColumn]}|Where-Object {$_ -isnot [DBNull]}|Sort-Object -Unique)
   if(!$values.Count){continue}
   if($values|Where-Object {$_ -isnot [int] -and $_ -isnot [long] -and $_ -isnot [guid]}){throw 'Unsupported FK type requires explicit fixture handling'}
   $ids=($values|ForEach-Object {if($_ -is [guid]){"'$($_.ToString('D'))'"}else{[long]$_}}) -join ','
   $parents=Query $source "SELECT * FROM dbo.[$($fk.RefTable)] WHERE [$($fk.RefColumn)] IN ($ids)"
   if(!$tables.ContainsKey($fk.RefTable)){$tables[$fk.RefTable]=$parents;$queue.Enqueue($fk.RefTable)}else{
    $existing=$tables[$fk.RefTable];$new=0
    foreach($row in $parents.Rows){if(!@($existing.Rows|Where-Object {$_[$fk.RefColumn] -eq $row[$fk.RefColumn]}).Count){$existing.ImportRow($row);$new++}}
    if($new){$queue.Enqueue($fk.RefTable)}
   }
 }}
 foreach($name in $tables.Keys){Execute "ALTER TABLE dbo.[$name] NOCHECK CONSTRAINT ALL"|Out-Null}
 foreach($name in $tables.Keys){
  $bulk=[System.Data.SqlClient.SqlBulkCopy]::new($target,[System.Data.SqlClient.SqlBulkCopyOptions]::KeepIdentity,$null);$bulk.DestinationTableName="dbo.[$name]"
  $columns=Query $target "SELECT name FROM sys.columns WHERE object_id=OBJECT_ID('dbo.$name') AND is_computed=0 AND system_type_id<>189"
  foreach($column in $columns.Rows){$null=$bulk.ColumnMappings.Add($column.name,$column.name)}
  $bulk.WriteToServer($tables[$name]);$bulk.Dispose()
 }
 foreach($name in $tables.Keys){Execute "ALTER TABLE dbo.[$name] WITH CHECK CHECK CONSTRAINT ALL"|Out-Null}
 $original=DataRows $before
 $original|ConvertTo-Json -Depth 12|Set-Content "$out\legacy-before.json" -Encoding utf8
 $backupDir=[string](Query $target "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')) AS P").Rows[0].P
 $backup=Join-Path $backupDir ('PMR_COMPAT_SPC_20260918-'+(Get-Date -Format yyyyMMddHHmmss)+'.bak')
 $cmd=$target.CreateCommand();$cmd.CommandTimeout=120;$cmd.CommandText='BACKUP DATABASE [PMR_COMPAT_SPC_20260918] TO DISK=@path WITH COPY_ONLY,CHECKSUM';$null=$cmd.Parameters.AddWithValue('@path',$backup);$cmd.ExecuteNonQuery()|Out-Null
 foreach($file in Get-ChildItem 'D:\SPC\release-staging\production-compatibility-20260918\migrations' -Filter '*.sql'|Sort-Object Name){foreach($batch in ([IO.File]::ReadAllText($file.FullName) -split '(?im)^GO\s*$')){if($batch.Trim()){Execute $batch|Out-Null}}}
 $sql=[IO.File]::ReadAllText((Join-Path $PSScriptRoot 'legacy-stage.sql'))
 $changed=[int](Query $target $sql).Rows[0].ChangedRows
 if($changed -ne $before.Rows.Count){throw 'Conversion count mismatch'}
 $after=DataRows (Query $target 'SELECT * FROM dbo.VariableMeasurements ORDER BY Id')
 for($i=0;$i -lt $original.Count;$i++){
  $old=$original[$i];$new=$after[$i]
  $stage=if($old.SamplingPhase -eq 'OPEN'){'OPEN'}else{'CLOSE'};$phase=if($old.SamplingPhase -eq 'MIDDLE'){'MIDDLE'}else{'OPEN'}
  if($new.SamplingStage -ne $stage -or $new.SamplingPhase -ne $phase){throw 'Mapping mismatch'}
  foreach($prop in $old.PSObject.Properties){if($prop.Name -ne 'SamplingPhase' -and ($prop.Value|ConvertTo-Json -Compress -Depth 10) -cne ($new.($prop.Name)|ConvertTo-Json -Compress -Depth 10)){throw "Unrelated value changed: $($prop.Name)"}}
 }
 $rerun=[int](Query $target $sql).Rows[0].ChangedRows;if($rerun -ne 0){throw 'Conversion not idempotent'}
 foreach($file in Get-ChildItem 'D:\SPC\release-staging\production-compatibility-20260918\migrations' -Filter '*.sql'|Sort-Object Name){foreach($batch in ([IO.File]::ReadAllText($file.FullName) -split '(?im)^GO\s*$')){if($batch.Trim()){Execute $batch|Out-Null}}}
 @{Target='localhost/PMR_COMPAT_SPC_20260918';FixtureRows=$before.Rows.Count;Tables=@($tables.Keys);Migrations=8;Changed=$changed;RerunChanged=$rerun;OtherFieldsPreserved=$true;BaselineBackup=$backup}|ConvertTo-Json -Depth 5|Set-Content "$out\spc-result.json" -Encoding utf8
 Write-Output "SPC rehearsal passed: 8 migrations; $changed rows mapped; rerun 0; original fields preserved."
}finally{$source.Dispose();$target.Dispose()}
