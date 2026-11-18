param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$configs = @{
  test = (Get-Content 'D:/SPC/release/test/backend/appsettings.json' -Raw | ConvertFrom-Json).ConnectionStrings.SqlServer
  production = (Get-Content 'D:/SPC/release/production/backend/appsettings.json' -Raw | ConvertFrom-Json).ConnectionStrings.SqlServer
}

$queries = [ordered]@{
  environment = "SELECT @@SERVERNAME ServerName, DB_NAME() DatabaseName, SYSUTCDATETIME() QueriedAtUtc"
  lineSummary = @"
SELECT m.MachineCode,m.MachineName,
 COUNT(*) MappingTotal,SUM(CASE WHEN p.IsEnabled=1 AND p.IsDeleted=0 THEN 1 ELSE 0 END) EnabledMappings,
 COUNT(DISTINCT p.TankId) Tanks,COUNT(DISTINCT p.CharacteristicId) Characteristics,
 SUM(CASE WHEN pol.PartProcessCharacteristicId IS NOT NULL THEN 1 ELSE 0 END) FormulaPolicies,
 SUM(CASE WHEN pol.IsEnabled=1 THEN 1 ELSE 0 END) EnabledFormulaPolicies,
 (SELECT COUNT(*) FROM VariableMeasurements v JOIN PartProcessCharacteristics px ON px.Id=v.PartProcessCharacteristicId WHERE px.ControlScope='CHEM' AND px.MachineId=m.Id) MeasurementsTotal,
 (SELECT COUNT(*) FROM VariableMeasurements v JOIN PartProcessCharacteristics px ON px.Id=v.PartProcessCharacteristicId WHERE px.ControlScope='CHEM' AND px.MachineId=m.Id AND v.IsDeleted=0) MeasurementsActive,
 (SELECT COUNT(*) FROM VariableMeasurements v JOIN PartProcessCharacteristics px ON px.Id=v.PartProcessCharacteristicId WHERE px.ControlScope='CHEM' AND px.MachineId=m.Id AND v.IsDeleted=1) MeasurementsDeleted,
 (SELECT MIN(v.MeasuredAt) FROM VariableMeasurements v JOIN PartProcessCharacteristics px ON px.Id=v.PartProcessCharacteristicId WHERE px.ControlScope='CHEM' AND px.MachineId=m.Id AND v.IsDeleted=0) FirstMeasuredAt,
 (SELECT MAX(v.MeasuredAt) FROM VariableMeasurements v JOIN PartProcessCharacteristics px ON px.Id=v.PartProcessCharacteristicId WHERE px.ControlScope='CHEM' AND px.MachineId=m.Id AND v.IsDeleted=0) LastMeasuredAt,
 (SELECT COUNT(*) FROM ChemicalCalculationReceipts r JOIN PartProcessCharacteristics px ON px.Id=r.PartProcessCharacteristicId WHERE px.ControlScope='CHEM' AND px.MachineId=m.Id) Receipts,
 (SELECT COUNT(*) FROM ChemicalCalculationTraces tr JOIN VariableMeasurements v ON v.Id=tr.VariableMeasurementId JOIN PartProcessCharacteristics px ON px.Id=v.PartProcessCharacteristicId WHERE px.ControlScope='CHEM' AND px.MachineId=m.Id) Traces
FROM PartProcessCharacteristics p JOIN Machines m ON m.Id=p.MachineId
LEFT JOIN ChemicalFormulaPolicies pol ON pol.PartProcessCharacteristicId=p.Id
WHERE p.ControlScope='CHEM'
GROUP BY m.Id,m.MachineCode,m.MachineName ORDER BY m.MachineCode
"@
  mappings = @"
SELECT m.MachineCode,m.MachineName,pr.ProcessCode,pr.ProcessName,t.TankCode,t.TankName,
 c.CharacteristicCode,c.CharacteristicName,p.Unit,p.IsEnabled,p.IsDeleted,p.USL,p.LSL,p.TargetValue,p.UCL,p.CL,p.LCL,
 p.SampleSize,p.ChartTypeId,p.IsRequired,p.DisplayMode,p.SequenceNo,c.DecimalPlaces,c.InputMode,c.ValueLabel,
 CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),ISNULL(p.FormulaConfigJson,''))),2) FormulaConfigHash,
 CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),ISNULL(p.ChemicalAnalysisConfigJson,''))),2) AnalysisConfigHash,
 CASE WHEN pol.PartProcessCharacteristicId IS NULL THEN 0 ELSE 1 END HasPolicy,pol.IsEnabled PolicyEnabled,rev.RevisionNo ActiveRevisionNo
FROM PartProcessCharacteristics p
JOIN Machines m ON m.Id=p.MachineId LEFT JOIN Processes pr ON pr.Id=p.ProcessId LEFT JOIN Tanks t ON t.Id=p.TankId
LEFT JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId
LEFT JOIN ChemicalFormulaPolicies pol ON pol.PartProcessCharacteristicId=p.Id
LEFT JOIN ChemicalFormulaScheduleRevisions rev ON rev.Id=pol.ActiveScheduleRevisionId
WHERE p.ControlScope='CHEM'
ORDER BY m.MachineCode,pr.ProcessCode,t.TankCode,c.CharacteristicCode,p.Id
"@
  effectivities = @"
SELECT m.MachineCode,pr.ProcessCode,t.TankCode,c.CharacteristicCode,rev.RevisionNo,e.EffectiveFromUtc,e.EffectiveUntilUtcExclusive,
 fv.VersionNo FormulaVersionNo,CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),ISNULL(fv.ConfigJson,''))),2) FormulaConfigHash,
 fh.VersionNo FHistoryVersionNo,fh.VersionCode FVersionCode,
 CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varbinary(max),ISNULL(fh.CellsJson,''))),2) FCellsHash
FROM ChemicalFormulaEffectivities e
JOIN PartProcessCharacteristics p ON p.Id=e.PartProcessCharacteristicId JOIN Machines m ON m.Id=p.MachineId
LEFT JOIN Processes pr ON pr.Id=p.ProcessId LEFT JOIN Tanks t ON t.Id=p.TankId LEFT JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId
JOIN ChemicalFormulaScheduleRevisions rev ON rev.Id=e.ScheduleRevisionId
LEFT JOIN ChemicalAnalysisFormulaVersions fv ON fv.Id=e.FormulaVersionId
LEFT JOIN ChemicalFTableVersionHistories fh ON fh.Id=e.FTableHistoryId
WHERE p.ControlScope='CHEM'
ORDER BY m.MachineCode,pr.ProcessCode,t.TankCode,c.CharacteristicCode,e.EffectiveFromUtc
"@
  fReferences = @"
SELECT m.MachineCode,pr.ProcessCode,t.TankCode,c.CharacteristicCode,v.VersionCode,v.DisplayName,r.NormalizedCellAddress,r.SourceFormula,r.SourceSheet,r.SourceRow,r.ReferenceContext
FROM ChemicalFTableReferences r JOIN ChemicalFTableVersions v ON v.Id=r.VersionId
JOIN PartProcessCharacteristics p ON p.Id=r.PartProcessCharacteristicId JOIN Machines m ON m.Id=p.MachineId
LEFT JOIN Processes pr ON pr.Id=p.ProcessId LEFT JOIN Tanks t ON t.Id=p.TankId LEFT JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId
WHERE p.ControlScope='CHEM' AND r.IsDeleted=0 AND v.IsDeleted=0
ORDER BY m.MachineCode,pr.ProcessCode,t.TankCode,c.CharacteristicCode,v.VersionCode,r.NormalizedCellAddress
"@
  measurements = @"
SELECT m.MachineCode,pr.ProcessCode,t.TankCode,c.CharacteristicCode,v.MeasuredAt,v.SampleNo,v.MeasuredValue,v.RecheckValue,
 v.SamplingPhase,v.SamplingStage,v.SourceType,v.SourceReference,v.PortalDailyDate,v.AdjustAction,v.AdjustAmount,v.Operator,
 v.IsDeleted,CONVERT(varchar(64),v.ChemicalSourceKeyHash,2) SourceKeyHash,
 CASE WHEN v.ChemicalReceiptId IS NULL THEN 0 ELSE 1 END HasReceipt,
 (SELECT COUNT(*) FROM ChemicalCalculationTraces tr WHERE tr.VariableMeasurementId=v.Id) TraceCount
FROM VariableMeasurements v JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=p.MachineId LEFT JOIN Processes pr ON pr.Id=p.ProcessId LEFT JOIN Tanks t ON t.Id=p.TankId LEFT JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId
WHERE p.ControlScope='CHEM'
ORDER BY m.MachineCode,v.MeasuredAt,pr.ProcessCode,t.TankCode,c.CharacteristicCode,v.Id
"@
}

foreach ($name in @('test','production')) {
  $connectionString = $configs[$name] -replace 'Trust Server Certificate','TrustServerCertificate'
  $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
  $connection.Open()
  try {
    $result = [ordered]@{}
    foreach ($entry in $queries.GetEnumerator()) {
      $command = $connection.CreateCommand(); $command.CommandTimeout = 180; $command.CommandText = $entry.Value
      $reader = $command.ExecuteReader(); $table = [System.Data.DataTable]::new(); $table.Load($reader)
      $rows = @(); foreach ($row in $table.Rows) { $item=[ordered]@{}; foreach ($column in $table.Columns) { $value=$row[$column.ColumnName]; $item[$column.ColumnName] = if ($value -is [DBNull]) { $null } else { $value } }; $rows += [pscustomobject]$item }
      $result[$entry.Key] = $rows
    }
    $result | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $OutputDirectory "$name.json") -Encoding utf8
  } finally { $connection.Dispose() }
}
