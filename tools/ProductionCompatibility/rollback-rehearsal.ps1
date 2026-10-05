$ErrorActionPreference='Stop'
$out='D:\SPC\release-staging\production-compatibility-20260918\rehearsal'
$baseline=Get-Content "$out\spc-result.json" -Raw|ConvertFrom-Json
$cn=[System.Data.SqlClient.SqlConnection]::new('Server=localhost;Database=master;Integrated Security=True;Encrypt=False');$cn.Open()
try{
 $cmd=$cn.CreateCommand();$cmd.CommandTimeout=180
 $cmd.CommandText="SELECT DB_ID('PMR_COMPAT_SPC_ROLLBACK_20260918')";if($cmd.ExecuteScalar() -isnot [DBNull]){throw 'Rollback target already exists; refusing overwrite'}
 $cmd.CommandText='RESTORE VERIFYONLY FROM DISK=@backup WITH CHECKSUM';$null=$cmd.Parameters.AddWithValue('@backup',[string]$baseline.BaselineBackup);$cmd.ExecuteNonQuery()|Out-Null
 $cmd.CommandText='RESTORE FILELISTONLY FROM DISK=@backup';$files=[System.Data.DataTable]::new();$files.Load($cmd.ExecuteReader())
 $cmd.Parameters.Clear();$cmd.CommandText="SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'))";$dataDir=[string]$cmd.ExecuteScalar()
 $moves=@();$i=0
 $null=$cmd.Parameters.AddWithValue('@backup',[string]$baseline.BaselineBackup)
 foreach($file in $files.Rows){
  $ext=if($file.Type -eq 'L'){'.ldf'}else{'.mdf'}
  $physical=Join-Path $dataDir "PMR_COMPAT_SPC_ROLLBACK_20260918_$i$ext"
  $null=$cmd.Parameters.AddWithValue("@logical$i",[string]$file.LogicalName);$null=$cmd.Parameters.AddWithValue("@physical$i",$physical)
  $moves+="MOVE @logical$i TO @physical$i";$i++
 }
 $cmd.CommandText='RESTORE DATABASE [PMR_COMPAT_SPC_ROLLBACK_20260918] FROM DISK=@backup WITH '+($moves -join ',')+',RECOVERY,CHECKSUM'
 $cmd.ExecuteNonQuery()|Out-Null
 $cmd.Parameters.Clear();$cn.ChangeDatabase('PMR_COMPAT_SPC_ROLLBACK_20260918')
 $cmd.CommandText="SELECT COL_LENGTH('dbo.VariableMeasurements','SamplingStage')";if($cmd.ExecuteScalar() -isnot [DBNull]){throw 'Rollback retained new stage schema'}
 $cmd.CommandText='SELECT COUNT(*) FROM dbo.VariableMeasurements';if([int]$cmd.ExecuteScalar() -ne $baseline.FixtureRows){throw 'Rollback data count mismatch'}
 $cmd.CommandText="SELECT COUNT(*) FROM dbo.VariableMeasurements WHERE SamplingPhase IN ('OPEN','CLOSE','MIDDLE')";if([int]$cmd.ExecuteScalar() -ne $baseline.FixtureRows){throw 'Legacy phases missing'}
 $cmd.CommandText="SELECT COUNT(*) FROM __EFMigrationsHistory WHERE MigrationId>='20260911040000_AddCalibrationModule'";if([int]$cmd.ExecuteScalar() -ne 0){throw 'Rollback retained upgraded migration history'}
 @{Target='localhost/PMR_COMPAT_SPC_ROLLBACK_20260918';ChecksumVerified=$true;DatabaseRestored=$true;LegacyRows=[int]$baseline.FixtureRows;StageColumnAbsent=$true;UpgradeMigrationsAbsent=$true;ProductionUntouched=$true}|ConvertTo-Json|Set-Content "$out\rollback-result.json" -Encoding utf8
 'Rollback rehearsal passed: restored baseline schema/history and all 507 legacy rows to a separate local database.'
}finally{$cn.Dispose()}
