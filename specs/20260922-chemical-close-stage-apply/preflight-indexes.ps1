$ErrorActionPreference = 'Stop'

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
        $table = [System.Data.DataTable]::new()
        $table.Load($command.ExecuteReader())
        return $table
    }
    finally { $connection.Dispose() }
}

$cs = Read-ConnectionString 'D:\SPC\release\production\backend\appsettings.json'
Guard $cs 'PMR_SPC_2026'

Write-Host '=== INDEXES ==='
(Query $cs @"
SELECT i.name AS IndexName, i.is_unique, i.is_primary_key,
       STUFF((
         SELECT ',' + c.name
         FROM sys.index_columns ic
         JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id
         WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.is_included_column=0
         ORDER BY ic.key_ordinal
         FOR XML PATH('')
       ),1,1,'') AS KeyColumns
FROM sys.indexes i
WHERE i.object_id=OBJECT_ID('dbo.VariableMeasurements')
  AND i.name IS NOT NULL
ORDER BY i.is_unique DESC, i.name
"@) | Format-Table -AutoSize | Out-String -Width 220 | Write-Host

Write-Host '=== PAIR COUNT same PPC+date OPEN GENERAL vs CLOSE GENERAL ==='
(Query $cs @"
SELECT COUNT(*) AS PairCount
FROM VariableMeasurements c
JOIN VariableMeasurements o
  ON o.Id<>c.Id
 AND o.PartProcessCharacteristicId=c.PartProcessCharacteristicId
 AND COALESCE(o.PortalDailyDate,CONVERT(date,o.MeasuredAt))=COALESCE(c.PortalDailyDate,CONVERT(date,c.MeasuredAt))
 AND o.SamplingPhase='OPEN'
 AND ISNULL(NULLIF(LTRIM(RTRIM(o.SamplingStage)),''),'GENERAL')='GENERAL'
JOIN PartProcessCharacteristics p ON p.Id=c.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(c.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND c.SamplingPhase='CLOSE'
  AND ISNULL(NULLIF(LTRIM(RTRIM(c.SamplingStage)),''),'GENERAL')='GENERAL'
"@) | Format-Table -AutoSize | Out-String | Write-Host

Write-Host '=== TARGET OPEN+CLOSE already exists ==='
(Query $cs @"
SELECT COUNT(*) AS ConflictCount
FROM VariableMeasurements c
JOIN VariableMeasurements t
  ON t.Id<>c.Id
 AND t.PartProcessCharacteristicId=c.PartProcessCharacteristicId
 AND COALESCE(t.PortalDailyDate,CONVERT(date,t.MeasuredAt))=COALESCE(c.PortalDailyDate,CONVERT(date,c.MeasuredAt))
 AND t.SamplingPhase='OPEN'
 AND t.SamplingStage='CLOSE'
JOIN PartProcessCharacteristics p ON p.Id=c.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(c.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND c.SamplingPhase='CLOSE'
  AND ISNULL(NULLIF(LTRIM(RTRIM(c.SamplingStage)),''),'GENERAL')='GENERAL'
"@) | Format-Table -AutoSize | Out-String | Write-Host
