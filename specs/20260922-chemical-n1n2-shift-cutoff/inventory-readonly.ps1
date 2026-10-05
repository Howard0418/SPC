$ErrorActionPreference = 'Stop'
$outputDirectory = 'D:\SPC\release-staging\chemical-n1n2-shift-cutoff-20260922'
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
                $item[$reader.GetName($i)] = $value
            }
            [void]$rows.Add([pscustomobject]$item)
        }
        return $rows
    }
    finally { $connection.Dispose() }
}

$sql = @"
SELECT
  CASE WHEN COALESCE(v.PortalDailyDate, CONVERT(date,v.MeasuredAt)) < '2026-09-01' THEN 'BEFORE_SEP' ELSE 'FROM_SEP' END AS Period,
  CONVERT(varchar(7), COALESCE(v.PortalDailyDate, CONVERT(date,v.MeasuredAt)), 126) AS YearMonth,
  m.MachineCode AS LineCode,
  LTRIM(RTRIM(v.SamplingPhase)) AS SamplingPhase,
  ISNULL(NULLIF(LTRIM(RTRIM(v.SamplingStage)),''),'GENERAL') AS SamplingStage,
  COUNT(*) AS Cnt,
  MIN(CONVERT(varchar(10), COALESCE(v.PortalDailyDate, CONVERT(date,v.MeasuredAt)), 23)) AS MinDate,
  MAX(CONVERT(varchar(10), COALESCE(v.PortalDailyDate, CONVERT(date,v.MeasuredAt)), 23)) AS MaxDate
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
GROUP BY
  CASE WHEN COALESCE(v.PortalDailyDate, CONVERT(date,v.MeasuredAt)) < '2026-09-01' THEN 'BEFORE_SEP' ELSE 'FROM_SEP' END,
  CONVERT(varchar(7), COALESCE(v.PortalDailyDate, CONVERT(date,v.MeasuredAt)), 126),
  m.MachineCode,
  LTRIM(RTRIM(v.SamplingPhase)),
  ISNULL(NULLIF(LTRIM(RTRIM(v.SamplingStage)),''),'GENERAL')
ORDER BY YearMonth, LineCode, SamplingPhase, SamplingStage
"@

$testCs = Read-ConnectionString 'D:\SPC\release\test\backend\appsettings.json'
$prodCs = Read-ConnectionString 'D:\SPC\release\production\backend\appsettings.json'
Guard $testCs 'PMR_SPC_TEST'
Guard $prodCs 'PMR_SPC_2026'

$result = [ordered]@{
    AsOfUtc = [DateTimeOffset]::UtcNow.ToString('o')
    Mode = 'read-only'
    Cutoff = '2026-09-01'
    Test = [ordered]@{ Name = 'PMR_SPC_TEST'; Rows = @(Query $testCs $sql) }
    Production = [ordered]@{ Name = 'PMR_SPC_2026'; Rows = @(Query $prodCs $sql) }
}

$path = Join-Path $outputDirectory 'distribution.json'
$json = $result | ConvertTo-Json -Depth 8
[IO.File]::WriteAllText($path, $json, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText(
    'D:\SPC\specs\20260922-chemical-n1n2-shift-cutoff\distribution.json',
    $json,
    [Text.UTF8Encoding]::new($false))

Write-Host 'TEST'
$result.Test.Rows | Format-Table -AutoSize | Out-String -Width 220 | Write-Host
Write-Host 'PROD'
$result.Production.Rows | Format-Table -AutoSize | Out-String -Width 220 | Write-Host
Write-Host "EVIDENCE=$path"
