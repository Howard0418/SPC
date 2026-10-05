param([string]$OutputDirectory = $PSScriptRoot)
$ErrorActionPreference = 'Stop'
$outDir = $OutputDirectory
if (!(Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
foreach ($envName in @('test','production')) {
    $cfg = Get-Content "D:/SPC/release/$envName/backend/appsettings.json" -Raw | ConvertFrom-Json
    $con = [System.Data.SqlClient.SqlConnection]::new($cfg.ConnectionStrings.SqlServer)
    $con.Open()
    try {
        $queries = [ordered]@{
            environment = "SELECT @@SERVERNAME ServerName, DB_NAME() DatabaseName, CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath')) BackupPath FOR JSON PATH"
            batches = "SELECT b.UploadBatchId,b.UploadType,b.SourceType,b.ImportStatus,b.OriginalFileName,b.TotalRows,b.ValidRows,b.ErrorRows,b.CreatedAt, (SELECT COUNT(*) FROM VariableMeasurements v JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId WHERE v.UploadBatchId=b.UploadBatchId AND p.ControlScope='CHEM') ChemicalMeasurementCount FROM UploadBatches b WHERE EXISTS (SELECT 1 FROM VariableMeasurements v JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId WHERE v.UploadBatchId=b.UploadBatchId AND p.ControlScope='CHEM') ORDER BY b.CreatedAt FOR JSON PATH, INCLUDE_NULL_VALUES"
            schema = "SELECT TABLE_NAME,COLUMN_NAME,DATA_TYPE,IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ('VariableMeasurements','UploadBatches','UploadDetails','PartProcessCharacteristics','SpcCalculationResults','AlertEvents') ORDER BY TABLE_NAME,ORDINAL_POSITION FOR JSON PATH"
            measurements = "SELECT v.* FROM VariableMeasurements v JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId WHERE p.ControlScope='CHEM' ORDER BY v.Id FOR JSON PATH, INCLUDE_NULL_VALUES"
            mappings = "SELECT p.Id,p.ControlScope,p.PartId,p.ProcessId,p.MachineId,p.TankId,p.CharacteristicId,p.Unit,p.IsEnabled,pr.ProcessCode,m.MachineCode,t.TankCode,t.TankName,t.LineId TankLineId,c.CharacteristicCode,c.CharacteristicName FROM PartProcessCharacteristics p LEFT JOIN Processes pr ON pr.Id=p.ProcessId LEFT JOIN Machines m ON m.Id=p.MachineId LEFT JOIN Tanks t ON t.Id=p.TankId LEFT JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId WHERE p.ControlScope='CHEM' ORDER BY p.Id FOR JSON PATH, INCLUDE_NULL_VALUES"
            details = "SELECT d.* FROM UploadDetails d WHERE EXISTS (SELECT 1 FROM VariableMeasurements v JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId WHERE v.UploadBatchId=d.UploadBatchId AND p.ControlScope='CHEM') AND ISJSON(d.PayloadJson)=1 AND JSON_VALUE(d.PayloadJson,'$.ControlScope') IN ('CHEM','CHEMICAL') ORDER BY d.Id FOR JSON PATH, INCLUDE_NULL_VALUES"
            migrations = 'SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId FOR JSON PATH'
            triggers = "SELECT name,OBJECT_NAME(parent_id) TableName,is_disabled FROM sys.triggers WHERE parent_class=1 AND OBJECT_NAME(parent_id) IN ('VariableMeasurements','UploadBatches','UploadDetails','SpcCalculationResults','AlertEvents') FOR JSON PATH"
        }
        foreach ($entry in $queries.GetEnumerator()) {
            $cmd=$con.CreateCommand(); $cmd.CommandText=$entry.Value; $cmd.CommandTimeout=60
            $reader=$cmd.ExecuteReader(); $builder=[System.Text.StringBuilder]::new()
            while ($reader.Read()) { [void]$builder.Append($reader.GetString(0)) }; $reader.Close()
            $json=$builder.ToString(); if (!$json) {$json='[]'}
            [System.IO.File]::WriteAllText((Join-Path $outDir "$envName-$($entry.Key).json"),$json)
            if ($entry.Key -eq 'environment') { Write-Output $json }
        }
    } finally { $con.Dispose() }
}
