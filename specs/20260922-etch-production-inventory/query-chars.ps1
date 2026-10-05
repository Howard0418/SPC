$ErrorActionPreference = 'Stop'
function Query($cs, $sql) {
  $cn = [System.Data.SqlClient.SqlConnection]::new($cs)
  $cn.Open()
  try {
    $cmd = $cn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 120
    $r = $cmd.ExecuteReader(); $rows = New-Object System.Collections.Generic.List[object]
    while ($r.Read()) {
      $o = [ordered]@{}
      for ($i = 0; $i -lt $r.FieldCount; $i++) {
        $v = $r.GetValue($i); if ($v -is [DBNull]) { $v = $null }
        $o[$r.GetName($i)] = $v
      }
      [void]$rows.Add([pscustomobject]$o)
    }
    return $rows
  } finally { $cn.Dispose() }
}

$spcTest = (Get-Content 'D:\SPC\release\test\backend\appsettings.json' -Raw | ConvertFrom-Json).ConnectionStrings.SqlServer
$spcProd = (Get-Content 'D:\SPC\release\production\backend\appsettings.json' -Raw | ConvertFrom-Json).ConnectionStrings.SqlServer
$out = 'D:\SPC\release-staging\etch-prod-inventory-20260922\char-breakdown.json'

$sql = @"
SELECT ISNULL(c.CharacteristicCode,'(null)') AS CharCode,
       ISNULL(m.MachineCode,'(null)') AS LineCode,
       COUNT(*) AS Cnt
FROM VariableMeasurements v
LEFT JOIN PartProcessCharacteristics p ON p.Id = v.PartProcessCharacteristicId
LEFT JOIN QualityCharacteristics c ON c.Id = COALESCE(v.CharacteristicId, p.CharacteristicId)
LEFT JOIN Machines m ON m.Id = COALESCE(v.MachineId, p.MachineId)
WHERE v.SourceReference LIKE 'ETCH:2026-09-%'
GROUP BY ISNULL(c.CharacteristicCode,'(null)'), ISNULL(m.MachineCode,'(null)')
ORDER BY CharCode, LineCode
"@

$sqlAllPpc = @"
SELECT m.MachineCode AS LineCode,
       c.CharacteristicCode AS CharCode,
       p.Id AS PpcId,
       p.IsEnabled,
       p.ControlScope,
       ct.ChartTypeCode AS ChartType
FROM PartProcessCharacteristics p
JOIN QualityCharacteristics c ON c.Id = p.CharacteristicId
LEFT JOIN Machines m ON m.Id = p.MachineId
LEFT JOIN ControlChartTypes ct ON ct.Id = p.ChartTypeId
WHERE c.CharacteristicCode LIKE 'ETCH%'
ORDER BY LineCode, CharCode, PpcId
"@

$payload = [ordered]@{
  test = [ordered]@{
    byCharLine = @(Query $spcTest $sql)
    allEtchPpc = @(Query $spcTest $sqlAllPpc)
  }
  prod = [ordered]@{
    byCharLine = @(Query $spcProd $sql)
    allEtchPpc = @(Query $spcProd $sqlAllPpc)
  }
}
$payload | ConvertTo-Json -Depth 6 | Set-Content $out -Encoding UTF8
Copy-Item $out 'D:\SPC\specs\20260922-etch-production-inventory\char-breakdown.json' -Force
Write-Output "TEST byCharLine=$($payload.test.byCharLine.Count) allEtchPpc=$($payload.test.allEtchPpc.Count)"
Write-Output "PROD byCharLine=$($payload.prod.byCharLine.Count) allEtchPpc=$($payload.prod.allEtchPpc.Count)"
$payload.test.byCharLine | Group-Object CharCode | ForEach-Object { "TEST $($_.Name)=$($_.Group | Measure-Object Cnt -Sum | Select-Object -ExpandProperty Sum)" }
$payload.prod.allEtchPpc | ForEach-Object { "PROD PPC $($_.LineCode) $($_.CharCode) enabled=$($_.IsEnabled) chart=$($_.ChartType)" }
