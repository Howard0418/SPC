$ErrorActionPreference = 'Stop'
$outputDirectory = 'D:\SPC\release-staging\chemical-close-stage-inventory-20260922'
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

function Read-ConnectionString([string]$path) {
    $config = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    return [string]$config.ConnectionStrings.SqlServer
}

function Guard([string]$connectionString, [string]$database) {
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
    $server = $builder.DataSource
    if ($server.StartsWith('tcp:', [StringComparison]::OrdinalIgnoreCase)) { $server = $server.Substring(4) }
    if ($server.Contains(',')) { $server = $server.Split(',')[0] }
    if ($server -ne '172.16.110.16' -or $builder.InitialCatalog -ne $database) {
        throw "Unexpected target $($builder.DataSource)/$($builder.InitialCatalog)"
    }
}

function Query([string]$connectionString, [string]$sql) {
    $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $connection.Open()
    try {
        $command = $connection.CreateCommand()
        $command.CommandText = $sql
        $command.CommandTimeout = 120
        $reader = $command.ExecuteReader()
        $rows = New-Object System.Collections.Generic.List[object]
        while ($reader.Read()) {
            $item = [ordered]@{}
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                $value = $reader.GetValue($i)
                if ($value -is [DBNull]) { $value = $null }
                elseif ($value -is [DateTime]) { $value = $value.ToString('yyyy-MM-ddTHH:mm:ss') }
                $item[$reader.GetName($i)] = $value
            }
            [void]$rows.Add([pscustomobject]$item)
        }
        return $rows
    }
    finally { $connection.Dispose() }
}

function Inventory([string]$name, [string]$connectionString) {
    $schema = @(Query $connectionString @"
SELECT
  CASE WHEN COL_LENGTH('dbo.VariableMeasurements','SamplingStage') IS NULL THEN 0 ELSE 1 END AS HasSamplingStage,
  CASE WHEN COL_LENGTH('dbo.VariableMeasurements','PortalDailyDate') IS NULL THEN 0 ELSE 1 END AS HasPortalDailyDate,
  (SELECT COUNT(*) FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.VariableMeasurements') AND name LIKE '%SamplingStage%') AS StageIndexCount
"@)[0]

    $hasStage = [bool]$schema.HasSamplingStage
    $stageSelect = if ($hasStage) { "ISNULL(NULLIF(LTRIM(RTRIM(v.SamplingStage)),''),'GENERAL')" } else { "'(COLUMN_MISSING)'" }
    $stageFilter = if ($hasStage) { "AND ISNULL(NULLIF(LTRIM(RTRIM(v.SamplingStage)),''),'GENERAL')='GENERAL'" } else { "" }
    $targetConflict = if ($hasStage) {
@"
CAST(CASE WHEN EXISTS (
  SELECT 1
  FROM VariableMeasurements t
  WHERE t.Id<>v.Id
    AND t.PartProcessCharacteristicId=v.PartProcessCharacteristicId
    AND COALESCE(t.PortalDailyDate,CONVERT(date,t.MeasuredAt))=COALESCE(v.PortalDailyDate,CONVERT(date,v.MeasuredAt))
    AND t.SamplingPhase='OPEN'
    AND t.SamplingStage='CLOSE'
) THEN 1 ELSE 0 END AS bit)
"@
    } else { "CAST(0 AS bit)" }

    $candidateSql = @"
SELECT v.Id,
       m.MachineCode AS LineCode,
       CONVERT(varchar(10),COALESCE(v.PortalDailyDate,v.MeasuredAt),23) AS DailyDate,
       v.SamplingPhase,
       $stageSelect AS SamplingStage,
       v.SourceType,
       CONVERT(varchar(36),v.UploadBatchId) AS UploadBatchId,
       b.SourceType AS BatchSourceType,
       b.OriginalFileName,
       v.PartProcessCharacteristicId,
       $targetConflict AS HasExistingOpenCloseTarget,
       COUNT(*) OVER (
         PARTITION BY v.PartProcessCharacteristicId,COALESCE(v.PortalDailyDate,CONVERT(date,v.MeasuredAt))
       ) AS CandidateRowsPerTargetKey
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
LEFT JOIN UploadBatches b ON b.UploadBatchId=v.UploadBatchId
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND LTRIM(RTRIM(v.SamplingPhase))='CLOSE'
  $stageFilter
ORDER BY DailyDate,LineCode,v.PartProcessCharacteristicId,v.Id
"@
    $candidates = @(Query $connectionString $candidateSql)

    $allCloseSql = @"
SELECT m.MachineCode AS LineCode,
       v.SourceType,
       COUNT(*) AS [RowCount]
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND LTRIM(RTRIM(v.SamplingPhase))='CLOSE'
GROUP BY m.MachineCode,v.SourceType
ORDER BY LineCode,v.SourceType
"@
    $allClose = @(Query $connectionString $allCloseSql)

    $converted = if ($hasStage) {
        @(Query $connectionString @"
SELECT m.MachineCode AS LineCode,
       COUNT(*) AS [RowCount]
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND LTRIM(RTRIM(v.SamplingPhase))='OPEN'
  AND LTRIM(RTRIM(v.SamplingStage))='CLOSE'
GROUP BY m.MachineCode
ORDER BY LineCode
"@)
    } else { @() }

    return [ordered]@{
        Name = $name
        Schema = $schema
        CandidateCount = $candidates.Count
        NonManualCandidateCount = @($candidates | Where-Object { $_.SourceType -ne 1 }).Count
        PortalDailyBatchCandidateCount = @($candidates | Where-Object { $_.BatchSourceType -eq 'PortalDaily' }).Count
        ExistingTargetConflictCount = @($candidates | Where-Object HasExistingOpenCloseTarget).Count
        DuplicateCandidateKeyRows = @($candidates | Where-Object { $_.CandidateRowsPerTargetKey -gt 1 }).Count
        ByLine = @($candidates | Group-Object LineCode | ForEach-Object {
            [ordered]@{ LineCode=$_.Name; RowCount=$_.Count }
        })
        BySourceType = @($candidates | Group-Object SourceType | ForEach-Object {
            [ordered]@{ SourceType=[int]$_.Name; RowCount=$_.Count }
        })
        ByBatch = @($candidates | Group-Object UploadBatchId | ForEach-Object {
            $sample = $_.Group[0]
            [ordered]@{
                UploadBatchId=$_.Name
                RowCount=$_.Count
                BatchSourceType=$sample.BatchSourceType
                OriginalFileName=$sample.OriginalFileName
            }
        })
        ExistingOpenCloseCount = @($converted | Measure-Object RowCount -Sum).Sum
        ExistingOpenCloseByLine = $converted
        AllCloseByLineAndSource = $allClose
        Candidates = $candidates
    }
}

$testCs = Read-ConnectionString 'D:\SPC\release\test\backend\appsettings.json'
$prodCs = Read-ConnectionString 'D:\SPC\release\production\backend\appsettings.json'
Guard $testCs 'PMR_SPC_TEST'
Guard $prodCs 'PMR_SPC_2026'

$result = [ordered]@{
    AsOfUtc = [DateTimeOffset]::UtcNow.ToString('o')
    Mode = 'read-only'
    SourceTypeMap = [ordered]@{
        Manual = 1
        Csv = 2
        Excel = 3
        Mes = 4
        Api = 5
    }
    Test = Inventory 'PMR_SPC_TEST' $testCs
    Production = Inventory 'PMR_SPC_2026' $prodCs
}

$path = Join-Path $outputDirectory 'inventory.json'
[IO.File]::WriteAllText(
    $path,
    ($result | ConvertTo-Json -Depth 10),
    [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText(
    'D:\SPC\specs\20260922-chemical-close-stage-inventory\inventory.json',
    ($result | ConvertTo-Json -Depth 10),
    [Text.UTF8Encoding]::new($false))

Write-Output "TEST schemaStage=$($result.Test.Schema.HasSamplingStage) candidates=$($result.Test.CandidateCount) conflicts=$($result.Test.ExistingTargetConflictCount) duplicateKeyRows=$($result.Test.DuplicateCandidateKeyRows)"
Write-Output "PROD schemaStage=$($result.Production.Schema.HasSamplingStage) candidates=$($result.Production.CandidateCount) conflicts=$($result.Production.ExistingTargetConflictCount) duplicateKeyRows=$($result.Production.DuplicateCandidateKeyRows)"
Write-Output "EVIDENCE=$path"
