$ErrorActionPreference = 'Stop'
$outDir = 'D:\SPC\release-staging\etch-prod-inventory-20260922'
$specDir = 'D:\SPC\specs\20260922-etch-production-inventory'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
New-Item -ItemType Directory -Path $specDir -Force | Out-Null

function Get-Cs($path, $key) {
  $j = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
  return $j.ConnectionStrings.$key
}

function Assert-Db($cs, $expectedDb, $expectedServer) {
  $b = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($cs)
  if ($expectedDb -and $b.InitialCatalog -ne $expectedDb) { throw "Unexpected db $($b.InitialCatalog) expected $expectedDb" }
  if ($expectedServer -and $b.DataSource -ne $expectedServer) { throw "Unexpected server $($b.DataSource)" }
  return [pscustomobject]@{ Server = $b.DataSource; Database = $b.InitialCatalog }
}

function Query($cs, $sql) {
  $cn = [System.Data.SqlClient.SqlConnection]::new($cs)
  $cn.Open()
  try {
    $cmd = $cn.CreateCommand()
    $cmd.CommandText = $sql
    $cmd.CommandTimeout = 120
    $r = $cmd.ExecuteReader()
    $rows = @()
    while ($r.Read()) {
      $o = [ordered]@{}
      for ($i = 0; $i -lt $r.FieldCount; $i++) {
        $v = $r.GetValue($i)
        if ($v -is [DBNull]) { $v = $null }
        elseif ($v -is [DateTime]) { $v = $v.ToString('yyyy-MM-ddTHH:mm:ss') }
        $o[$r.GetName($i)] = $v
      }
      $rows += [pscustomobject]$o
    }
    return $rows
  }
  finally { $cn.Dispose() }
}

$spcTestCs = Get-Cs 'D:\SPC\release\test\backend\appsettings.json' 'SqlServer'
$spcProdCs = Get-Cs 'D:\SPC\release\production\backend\appsettings.json' 'SqlServer'
$portalTestCs = Get-Cs 'D:\PmrPortal\release\test\portal-api\appsettings.json' 'Test'
$portalProdCs = Get-Cs 'D:\PmrPortal\release\production\portal-api\appsettings.json' 'Production'

$envInfo = [ordered]@{
  spc_test = Assert-Db $spcTestCs 'PMR_SPC_TEST' '172.16.110.16'
  spc_prod = Assert-Db $spcProdCs 'PMR_SPC_2026' '172.16.110.16'
  portal_test = Assert-Db $portalTestCs 'PMR_PORTAL_TEST' $null
  portal_prod = Assert-Db $portalProdCs $null $null
}

$portalSql = @"
SELECT CONVERT(varchar(10), r.ReportDate, 23) AS [ReportDate],
       r.LineCode AS [LineCode],
       r.LineSpeed AS [LineSpeed],
       r.OperatorName AS [OperatorName],
       r.SpcSyncStatus AS [SpcSyncStatus],
       (SELECT COUNT(*) FROM etch_amount_points p WHERE p.ReportId = r.Id) AS [PointCnt]
FROM etch_amount_reports r
WHERE r.ReportDate >= '20260901' AND r.ReportDate < '20261001'
ORDER BY r.ReportDate, r.LineCode
"@

$spcMeasSql = @"
SELECT CONVERT(varchar(10), v.MeasuredAt, 23) AS [ReportDate],
       m.MachineCode AS [LineCode],
       c.CharacteristicCode AS [CharCode],
       COUNT(*) AS [RowCnt]
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id = v.PartProcessCharacteristicId
JOIN Machines m ON m.Id = p.MachineId
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
WHERE v.SourceReference LIKE 'ETCH:2026-09-%'
GROUP BY CONVERT(varchar(10), v.MeasuredAt, 23), m.MachineCode, c.CharacteristicCode
ORDER BY [ReportDate], [LineCode], [CharCode]
"@

$spcAnySql = @"
SELECT CONVERT(varchar(10), v.MeasuredAt, 23) AS [ReportDate],
       m.MachineCode AS [LineCode],
       c.CharacteristicCode AS [CharCode],
       COUNT(*) AS [RowCnt],
       SUM(CASE WHEN v.SourceReference LIKE 'ETCH:%' THEN 1 ELSE 0 END) AS [EtchRefCnt],
       SUM(CASE WHEN v.SourceReference IS NULL OR v.SourceReference NOT LIKE 'ETCH:%' THEN 1 ELSE 0 END) AS [OtherRefCnt]
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id = v.PartProcessCharacteristicId
JOIN Machines m ON m.Id = p.MachineId
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
WHERE m.MachineCode IN ('PT1','PT2','QE1','QE2')
  AND c.CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
  AND v.MeasuredAt >= '2026-09-01' AND v.MeasuredAt < '2026-10-01'
GROUP BY CONVERT(varchar(10), v.MeasuredAt, 23), m.MachineCode, c.CharacteristicCode
ORDER BY [ReportDate], [LineCode], [CharCode]
"@

$mapsSql = @"
SELECT p.Id AS [PpcId],
       m.MachineCode AS [LineCode],
       c.CharacteristicCode AS [CharCode],
       p.IsEnabled AS [IsEnabled],
       p.ControlScope AS [ControlScope],
       ct.ChartTypeCode AS [ChartType]
FROM PartProcessCharacteristics p
JOIN Machines m ON m.Id = p.MachineId
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
LEFT JOIN ControlChartTypes ct ON ct.Id = p.ChartTypeId
WHERE m.MachineCode IN ('PT1','PT2','QE1','QE2')
  AND c.CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
ORDER BY [LineCode], [CharCode]
"@

$machinesSql = @"
SELECT MachineCode AS [LineCode], COUNT(*) AS [Cnt]
FROM Machines
WHERE MachineCode IN ('PT1','PT2','QE1','QE2')
GROUP BY MachineCode
ORDER BY [LineCode]
"@

$charsSql = @"
SELECT CharacteristicCode AS [CharCode], COUNT(*) AS [Cnt]
FROM QualityCharacteristics
WHERE CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
GROUP BY CharacteristicCode
ORDER BY [CharCode]
"@

$xbarSql = @"
SELECT ChartTypeCode AS [ChartCode], IsEnabled AS [IsEnabled], COUNT(*) AS [Cnt]
FROM ControlChartTypes
WHERE ChartTypeCode = 'XBAR_S'
GROUP BY ChartTypeCode, IsEnabled
"@

Write-Host 'Querying Portal TEST...'
$portalTest = Query $portalTestCs $portalSql
Write-Host 'Querying Portal PROD...'
$portalProd = Query $portalProdCs $portalSql
Write-Host 'Querying SPC TEST...'
$spcTestMeas = Query $spcTestCs $spcMeasSql
$spcTestAny = Query $spcTestCs $spcAnySql
$spcTestMaps = Query $spcTestCs $mapsSql
$spcTestMachines = Query $spcTestCs $machinesSql
$spcTestChars = Query $spcTestCs $charsSql
$spcTestXbar = Query $spcTestCs $xbarSql
Write-Host 'Querying SPC PROD...'
$spcProdMeas = Query $spcProdCs $spcMeasSql
$spcProdAny = Query $spcProdCs $spcAnySql
$spcProdMaps = Query $spcProdCs $mapsSql
$spcProdMachines = Query $spcProdCs $machinesSql
$spcProdChars = Query $spcProdCs $charsSql
$spcProdXbar = Query $spcProdCs $xbarSql

$raw = [ordered]@{
  asOfLocal = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
  mode = 'read-only'
  environment = $envInfo
  portal = [ordered]@{
    test = $portalTest
    prod = $portalProd
  }
  spc = [ordered]@{
    test = [ordered]@{
      etchSourceGroups = $spcTestMeas
      septProcessGroups = $spcTestAny
      mappings = $spcTestMaps
      machines = $spcTestMachines
      characteristics = $spcTestChars
      xbarS = $spcTestXbar
    }
    prod = [ordered]@{
      etchSourceGroups = $spcProdMeas
      septProcessGroups = $spcProdAny
      mappings = $spcProdMaps
      machines = $spcProdMachines
      characteristics = $spcProdChars
      xbarS = $spcProdXbar
    }
  }
}

$rawPath = Join-Path $outDir 'raw-query.json'
$raw | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $rawPath -Encoding UTF8
Copy-Item $rawPath (Join-Path $specDir 'raw-query.json') -Force

Write-Output "Portal TEST=$($portalTest.Count) PROD=$($portalProd.Count)"
Write-Output "SPC TEST groups=$($spcTestMeas.Count) PROD groups=$($spcProdMeas.Count)"
Write-Output "SPC TEST maps=$($spcTestMaps.Count) PROD maps=$($spcProdMaps.Count)"
Write-Output "Portal PROD db=$($envInfo.portal_prod.Database)"
Write-Output "RAW=$rawPath"
