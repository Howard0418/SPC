$ErrorActionPreference = 'Stop'
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
        $o[$r.GetName($i)] = $v
      }
      $rows += [pscustomobject]$o
    }
    return ,$rows
  }
  finally { $cn.Dispose() }
}

$spcTest = (Get-Content 'D:\SPC\release\test\backend\appsettings.json' -Raw | ConvertFrom-Json).ConnectionStrings.SqlServer
$spcProd = (Get-Content 'D:\SPC\release\production\backend\appsettings.json' -Raw | ConvertFrom-Json).ConnectionStrings.SqlServer
$outDir = 'D:\SPC\release-staging\etch-prod-inventory-20260922'

$sqlRef = @"
SELECT LEFT(ISNULL(v.SourceReference,''),48) AS RefPrefix,
       c.CharacteristicCode AS CharCode,
       COUNT(*) AS Cnt
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id = v.PartProcessCharacteristicId
JOIN Machines m ON m.Id = p.MachineId
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
WHERE m.MachineCode IN ('PT1','PT2','QE1','QE2')
  AND c.CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
  AND v.MeasuredAt >= '2026-09-01' AND v.MeasuredAt < '2026-10-01'
GROUP BY LEFT(ISNULL(v.SourceReference,''),48), c.CharacteristicCode
ORDER BY CharCode, Cnt DESC
"@

$sqlPpc = @"
SELECT m.MachineCode AS LineCode,
       c.CharacteristicCode AS CharCode,
       p.Id AS PpcId,
       p.IsEnabled AS IsEnabled,
       p.ControlScope AS ControlScope,
       ct.ChartTypeCode AS ChartType,
       COUNT(v.Id) AS MeasCnt
FROM PartProcessCharacteristics p
JOIN Machines m ON m.Id = p.MachineId
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
LEFT JOIN ControlChartTypes ct ON ct.Id = p.ChartTypeId
LEFT JOIN VariableMeasurements v ON v.PartProcessCharacteristicId = p.Id
  AND v.MeasuredAt >= '2026-09-01' AND v.MeasuredAt < '2026-10-01'
WHERE m.MachineCode IN ('PT1','PT2','QE1','QE2')
  AND c.CharacteristicCode IN ('ETCH_A_AVG','ETCH_B_AVG','ETCH_RATE','ETCH_LINE_SPEED')
GROUP BY m.MachineCode, c.CharacteristicCode, p.Id, p.IsEnabled, p.ControlScope, ct.ChartTypeCode
ORDER BY LineCode, CharCode
"@

$sqlTotal = @"
SELECT COUNT(*) AS Cnt
FROM VariableMeasurements v
WHERE v.SourceReference LIKE 'ETCH:2026-09-%'
"@

$result = [ordered]@{
  test = [ordered]@{
    refs = @(Query $spcTest $sqlRef)
    ppc = @(Query $spcTest $sqlPpc)
    etchSourceTotal = @((Query $spcTest $sqlTotal))[0].Cnt
  }
  prod = [ordered]@{
    refs = @(Query $spcProd $sqlRef)
    ppc = @(Query $spcProd $sqlPpc)
    etchSourceTotal = @((Query $spcProd $sqlTotal))[0].Cnt
  }
}

$path = Join-Path $outDir 'deep-query.json'
# Force arrays with Newtonsoft-style via ConvertTo-Json Depth; wrap singles
($result | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $path -Encoding UTF8
Copy-Item $path 'D:\SPC\specs\20260922-etch-production-inventory\deep-query.json' -Force
Write-Output "TEST etchSourceTotal=$($result.test.etchSourceTotal) ppc=$($result.test.ppc.Count)"
Write-Output "PROD etchSourceTotal=$($result.prod.etchSourceTotal) ppc=$($result.prod.ppc.Count)"
Write-Output "PATH=$path"
