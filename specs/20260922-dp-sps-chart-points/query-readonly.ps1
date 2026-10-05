$ErrorActionPreference = 'Stop'

function Read-Cs([string]$path) {
    $config = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    return [string]$config.ConnectionStrings.SqlServer
}

function Guard([string]$cs, [string]$db) {
    $b = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($cs)
    $s = $b.DataSource
    if ($s.StartsWith('tcp:')) { $s = $s.Substring(4) }
    if ($s.Contains(',')) { $s = $s.Split(',')[0] }
    if ($s -ne '172.16.110.16' -or $b.InitialCatalog -ne $db) {
        throw "Unexpected $($b.DataSource)/$($b.InitialCatalog)"
    }
}

function Query([string]$cs, [string]$sql) {
    $cn = [System.Data.SqlClient.SqlConnection]::new($cs)
    $cn.Open()
    try {
        $cmd = $cn.CreateCommand()
        $cmd.CommandText = $sql
        $cmd.CommandTimeout = 120
        $table = [System.Data.DataTable]::new()
        $table.Load($cmd.ExecuteReader())
        return $table
    }
    finally { $cn.Dispose() }
}

$sqlPpc = @"
SELECT ppc.Id, m.MachineCode, t.TankName, c.CharacteristicName, ppc.Unit, ppc.IsEnabled,
       ppc.DisplayMode, ppc.ChartTypeId, ppc.SampleSize,
       (SELECT COUNT(*) FROM VariableMeasurements v WHERE v.PartProcessCharacteristicId=ppc.Id) AS MeasCount
FROM PartProcessCharacteristics ppc
JOIN Machines m ON m.Id=ppc.MachineId
JOIN QualityCharacteristics c ON c.Id=ppc.CharacteristicId
LEFT JOIN Tanks t ON t.Id=ppc.TankId
WHERE ppc.ControlScope='CHEM'
  AND UPPER(LTRIM(RTRIM(m.MachineCode)))='DP'
  AND (
        c.CharacteristicName LIKE N'%'+NCHAR(36942)+NCHAR(30827)+NCHAR(37240)+NCHAR(37385)+'%'
        OR c.CharacteristicCode LIKE '%SPS%'
        OR c.CharacteristicNameEn LIKE '%SPS%'
      )
ORDER BY ppc.Id
"@

$sqlMeas = @"
SELECT ppc.Id AS PpcId,
       v.Id,
       CONVERT(varchar(19), v.MeasuredAt, 120) AS MeasuredAt,
       CONVERT(varchar(10), v.PortalDailyDate, 23) AS PortalDailyDate,
       v.SamplingPhase,
       v.SamplingStage,
       v.MeasuredValue,
       v.SourceType
FROM VariableMeasurements v
JOIN PartProcessCharacteristics ppc ON ppc.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=ppc.MachineId
JOIN QualityCharacteristics c ON c.Id=ppc.CharacteristicId
WHERE ppc.ControlScope='CHEM'
  AND UPPER(LTRIM(RTRIM(m.MachineCode)))='DP'
  AND (
        c.CharacteristicName LIKE N'%'+NCHAR(36942)+NCHAR(30827)+NCHAR(37240)+NCHAR(37385)+'%'
        OR c.CharacteristicCode LIKE '%SPS%'
        OR c.CharacteristicNameEn LIKE '%SPS%'
      )
ORDER BY ppc.Id, COALESCE(v.PortalDailyDate, CONVERT(date,v.MeasuredAt)), v.SamplingPhase, v.Id
"@

foreach ($pair in @(
    @{ Name='TEST'; Path='D:\SPC\release\test\backend\appsettings.json'; Db='PMR_SPC_TEST' },
    @{ Name='PROD'; Path='D:\SPC\release\production\backend\appsettings.json'; Db='PMR_SPC_2026' }
)) {
    $cs = Read-Cs $pair.Path
    Guard $cs $pair.Db
    Write-Host "==== $($pair.Name) PPC ===="
    (Query $cs $sqlPpc) | Format-Table -AutoSize | Out-String -Width 240 | Write-Host
    Write-Host "==== $($pair.Name) DP COUNTS ===="
    (Query $cs @"
SELECT ppc.Id, m.MachineCode, c.CharacteristicName, ppc.Unit,
       (SELECT COUNT(*) FROM VariableMeasurements v WHERE v.PartProcessCharacteristicId=ppc.Id) AS MeasCount
FROM PartProcessCharacteristics ppc
JOIN Machines m ON m.Id=ppc.MachineId
JOIN QualityCharacteristics c ON c.Id=ppc.CharacteristicId
WHERE ppc.ControlScope='CHEM'
  AND UPPER(LTRIM(RTRIM(m.MachineCode)))='DP'
ORDER BY MeasCount DESC, ppc.Id
"@) | Format-Table -AutoSize | Out-String -Width 240 | Write-Host
    Write-Host "==== $($pair.Name) MEAS ===="
    (Query $cs $sqlMeas) | Format-Table -AutoSize | Out-String -Width 240 | Write-Host
}
