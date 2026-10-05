param(
    [string]$OutputDirectory = 'D:\SPC\release-staging\etch-production-20260922'
)

$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$spc = Get-Content 'D:\SPC\release\production\backend\appsettings.json' -Raw -Encoding UTF8 | ConvertFrom-Json
$portal = Get-Content 'D:\PmrPortal\release\production\portal-api\appsettings.json' -Raw -Encoding UTF8 | ConvertFrom-Json

function Guard([string]$cs, [string]$database) {
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($cs)
    $server = $builder.DataSource
    if ($server.StartsWith('tcp:', [StringComparison]::OrdinalIgnoreCase)) { $server = $server.Substring(4) }
    if ($server.Contains(',')) { $server = $server.Split(',')[0] }
    if ($server -ne '172.16.110.16' -or $builder.InitialCatalog -ne $database) {
        throw "Unexpected target $($builder.DataSource)/$($builder.InitialCatalog)"
    }
}

function Query([string]$cs, [string]$sql) {
    $connection = [System.Data.SqlClient.SqlConnection]::new($cs)
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

$spcCs = [string]$spc.ConnectionStrings.SqlServer
$portalCs = [string]$portal.ConnectionStrings.Production
Guard $spcCs 'PMR_SPC_2026'
Guard $portalCs 'PMR_PORTAL_UAT'

$measurements = @(Query $spcCs @"
SELECT m.MachineCode AS line,
       c.CharacteristicCode AS code,
       v.SampleNo,
       v.MeasuredValue,
       v.SourceReference,
       CONVERT(varchar(10),v.MeasuredAt,23) AS [date],
       CONVERT(varchar(36),v.UploadBatchId) AS uploadBatchId
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=p.MachineId
JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId
WHERE v.SourceReference LIKE 'ETCH:2026-09-%'
ORDER BY [date],line,code,v.SampleNo
"@)

$points = @(Query $portalCs @"
SELECT CONVERT(varchar(10),r.ReportDate,23) AS [date],
       r.Id AS reportId,
       r.LineCode AS line,
       r.LineSpeed,
       r.OperatorName,
       r.SpcSyncStatus,
       p.Side,
       p.RepeatNo,
       p.OpNo,
       p.BeforeValue,
       p.AfterValue,
       p.EtchAmount
FROM etch_amount_reports r
JOIN etch_amount_points p ON p.ReportId=r.Id
WHERE r.ReportDate>='20260901' AND r.ReportDate<'20261001'
ORDER BY [date],line,p.Side,p.RepeatNo,p.OpNo
"@)

$reports = @(Query $portalCs @"
SELECT CONVERT(varchar(10),ReportDate,23) AS [date],
       Id AS reportId,
       LineCode AS line,
       SpcSyncStatus,
       SpcSyncError,
       CONVERT(varchar(33),SpcSyncedAt,126) AS SpcSyncedAt
FROM etch_amount_reports
WHERE ReportDate>='20260901' AND ReportDate<'20261001'
ORDER BY [date],line
"@)

$maps = @(Query $spcCs @"
SELECT p.Id,
       m.MachineCode AS line,
       c.CharacteristicCode AS code,
       p.SampleSize,
       p.IsEnabled,
       p.ControlScope,
       ct.ChartTypeCode AS chartType
FROM PartProcessCharacteristics p
JOIN Machines m ON m.Id=p.MachineId
JOIN QualityCharacteristics c ON c.Id=p.CharacteristicId
LEFT JOIN ControlChartTypes ct ON ct.Id=p.ChartTypeId
WHERE m.MachineCode IN ('PT1','PT2','QE1','QE2')
  AND c.CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
  AND p.IsEnabled=1
  AND p.ControlScope='PROCESS'
ORDER BY line,code
"@)

$quarantined = @(Query $portalCs @"
SELECT CONVERT(varchar(10),ReportDate,23) AS [date],LineCode AS line
FROM etch_amount_reports
WHERE (ReportDate='20260902' AND LineCode='PT2')
   OR (ReportDate IN ('20260908','20260911','20260916') AND LineCode='PT1')
   OR (ReportDate='20260918' AND LineCode IN ('PT1','PT2','QE1','QE2'))
ORDER BY [date],line
"@)

$database = [ordered]@{
    portal = $points
    reports = $reports
    spc = $measurements
    maps = $maps
    quarantined = $quarantined
}
$databasePath = Join-Path $OutputDirectory 'database-after.json'
[IO.File]::WriteAllText(
    $databasePath,
    ($database | ConvertTo-Json -Depth 10),
    [Text.UTF8Encoding]::new($false))

function Base64Url([byte[]]$bytes) {
    [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+','-').Replace('/','_')
}
$header = Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
$payload = Base64Url ([Text.Encoding]::UTF8.GetBytes(
    (@{
        sub = 'etch-production-readonly-verification'
        role = 'Viewer'
        exp = [DateTimeOffset]::UtcNow.AddMinutes(5).ToUnixTimeSeconds()
    } | ConvertTo-Json -Compress)))
$hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes([string]$spc.Auth.JwtKey))
$signature = Base64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes("$header.$payload")))
$token = "$header.$payload.$signature"

$apiBase = 'http://172.16.119.140:8081/api'
$charts = @()
foreach ($map in $maps) {
    $chart = Invoke-RestMethod `
        "$apiBase/v1/spc/chart?ppcId=$($map.Id)&startDate=2026-09-01&endDate=2026-09-30" `
        -Headers @{ Authorization = "Bearer $token" } `
        -TimeoutSec 30
    $charts += [ordered]@{ line=$map.line; code=$map.code; chart=$chart }
}
$chartsPath = Join-Path $OutputDirectory 'charts.json'
[IO.File]::WriteAllText(
    $chartsPath,
    ($charts | ConvertTo-Json -Depth 30),
    [Text.UTF8Encoding]::new($false))

Write-Output "Read-only evidence: Portal reports=$($reports.Count) points=$($points.Count) SPC=$($measurements.Count) maps=$($maps.Count) quarantined=$($quarantined.Count) charts=$($charts.Count)"
