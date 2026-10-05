param(
    [switch]$Execute,
    [string]$ConfirmSpc,
    [string]$BackupProof
)

$ErrorActionPreference = 'Stop'
$expectedServer = '172.16.110.16'
$expectedSpc = 'PMR_SPC_2026'
$expectedCount = 198
$outputDirectory = 'D:\SPC\release-staging\chemical-close-stage-apply-20260922'
$candidateSql = @"
SELECT v.*
FROM VariableMeasurements v WITH (UPDLOCK, HOLDLOCK)
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND LTRIM(RTRIM(v.SamplingPhase))='CLOSE'
  AND ISNULL(NULLIF(LTRIM(RTRIM(v.SamplingStage)),''),'GENERAL')='GENERAL'
ORDER BY v.Id
"@

function Read-ConnectionString([string]$path) {
    $config = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($config.AppEnvironment -cne 'production') {
        throw "Unexpected AppEnvironment '$($config.AppEnvironment)'"
    }
    return [string]$config.ConnectionStrings.SqlServer
}

function Normalize-Server([string]$value) {
    $server = $value.Trim()
    if ($server.StartsWith('tcp:', [StringComparison]::OrdinalIgnoreCase)) {
        $server = $server.Substring(4)
    }
    $comma = $server.IndexOf(',')
    if ($comma -ge 0) { $server = $server.Substring(0, $comma) }
    return $server
}

function Convert-Rows([System.Data.DataTable]$table) {
    return @($table.Rows | ForEach-Object {
        $row = $_
        $item = [ordered]@{}
        foreach ($column in $table.Columns) {
            $value = $row[$column]
            if ($value -is [DBNull]) { $value = $null }
            elseif ($value -is [byte[]]) { $value = [Convert]::ToBase64String($value) }
            elseif ($value -is [DateTime]) { $value = $value.ToString('o') }
            $item[$column.ColumnName] = $value
        }
        [pscustomobject]$item
    })
}

function Values-Equal($left, $right) {
    if ($null -eq $left -and $null -eq $right) { return $true }
    if ($null -eq $left -or $null -eq $right) { return $false }
    if ($left -is [byte[]] -or $right -is [byte[]]) {
        if ($left -is [byte[]]) { $left = [Convert]::ToBase64String($left) }
        if ($right -is [byte[]]) { $right = [Convert]::ToBase64String($right) }
    }
    if ($left -is [DateTime]) { $left = $left.ToString('o') }
    if ($right -is [DateTime]) { $right = $right.ToString('o') }
    return "$left" -ceq "$right"
}

$configPath = 'D:\SPC\release\production\backend\appsettings.json'
$connectionString = Read-ConnectionString $configPath
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
if ((Normalize-Server $builder.DataSource) -ne $expectedServer -or $builder.InitialCatalog -ne $expectedSpc) {
    throw "Unexpected database target '$($builder.DataSource)/$($builder.InitialCatalog)'"
}

if (-not $Execute) {
    Write-Output "PREVIEW only: would update $expectedCount rows on $expectedSpc"
    return
}

if ($ConfirmSpc -cne $expectedSpc) {
    throw 'Apply requires exact -ConfirmSpc PMR_SPC_2026'
}

if ([string]::IsNullOrWhiteSpace($BackupProof) -or -not (Test-Path -LiteralPath $BackupProof)) {
    throw 'Apply requires an existing -BackupProof file'
}

$proof = Get-Content -LiteralPath $BackupProof -Raw -Encoding UTF8 | ConvertFrom-Json
if ($proof.Version -ne 1 -or
    (Normalize-Server $proof.Server) -ne $expectedServer -or
    $proof.Spc.Database -cne $expectedSpc -or
    -not $proof.Spc.CopyOnly -or
    -not $proof.Spc.Checksum -or
    -not $proof.Spc.VerifyOnly) {
    throw 'Backup proof is invalid for PMR_SPC_2026 COPY_ONLY/CHECKSUM/VERIFYONLY'
}

$created = [DateTimeOffset]::Parse($proof.CreatedAtUtc)
$age = [DateTimeOffset]::UtcNow - $created
if ($age.TotalHours -gt 24 -or $age.TotalMinutes -lt -5) {
    throw 'Backup proof must be created within 24 hours of apply'
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
$run = Join-Path $outputDirectory ('prod-' + (Get-Date -Format yyyyMMdd-HHmmss))
New-Item -ItemType Directory -Path $run | Out-Null

$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
$connection.Open()
$transaction = $connection.BeginTransaction([System.Data.IsolationLevel]::Serializable)
function Invoke-Query([string]$sql) {
    $command = $connection.CreateCommand()
    $command.Transaction = $transaction
    $command.CommandText = $sql
    $command.CommandTimeout = 120
    $table = [System.Data.DataTable]::new()
    $table.Load($command.ExecuteReader())
    return ,$table
}

try {
    $beforeTable = Invoke-Query $candidateSql
    if ($beforeTable.Rows.Count -ne $expectedCount) {
        throw "Expected $expectedCount rows, got $($beforeTable.Rows.Count); no changes"
    }

    $conflictTable = Invoke-Query @"
SELECT COUNT(*) AS ConflictCount
FROM VariableMeasurements c WITH (HOLDLOCK)
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
  AND LTRIM(RTRIM(c.SamplingPhase))='CLOSE'
  AND ISNULL(NULLIF(LTRIM(RTRIM(c.SamplingStage)),''),'GENERAL')='GENERAL'
"@
    if ([int]$conflictTable.Rows[0].ConflictCount -ne 0) {
        throw "OPEN+CLOSE target conflict $($conflictTable.Rows[0].ConflictCount)"
    }

    $original = Convert-Rows $beforeTable
    [IO.File]::WriteAllText(
        (Join-Path $run 'before.json'),
        ($original | ConvertTo-Json -Depth 6),
        [Text.UTF8Encoding]::new($false))

    $openCloseBefore = Invoke-Query @"
SELECT COUNT(*) AS Cnt
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND v.SamplingPhase='OPEN'
  AND v.SamplingStage='CLOSE'
"@
    $openCloseBeforeCount = [int]$openCloseBefore.Rows[0].Cnt

    $update = $connection.CreateCommand()
    $update.Transaction = $transaction
    $update.CommandTimeout = 120
    $update.CommandText = @"
UPDATE v
SET SamplingPhase='OPEN',
    SamplingStage='CLOSE'
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND LTRIM(RTRIM(v.SamplingPhase))='CLOSE'
  AND ISNULL(NULLIF(LTRIM(RTRIM(v.SamplingStage)),''),'GENERAL')='GENERAL'
"@
    $changed = $update.ExecuteNonQuery()
    if ($changed -ne $expectedCount) { throw "Update count mismatch: $changed" }

    $ids = ($original | ForEach-Object { [long]$_.Id }) -join ','
    $afterTable = Invoke-Query "SELECT v.* FROM VariableMeasurements v WHERE v.Id IN ($ids) ORDER BY v.Id"
    $updated = Convert-Rows $afterTable
    if ($updated.Count -ne $original.Count) { throw 'Missing rows after update' }

    $n1 = 0
    $n2 = 0
    for ($i = 0; $i -lt $original.Count; $i++) {
        $old = $original[$i]
        $new = $updated[$i]
        if ([long]$old.Id -ne [long]$new.Id) { throw 'Row order mismatch' }
        if ($new.SamplingPhase -cne 'OPEN' -or $new.SamplingStage -cne 'CLOSE') {
            throw "Stage mapping mismatch for Id $($new.Id)"
        }
        foreach ($property in $old.PSObject.Properties) {
            if ($property.Name -in @('SamplingPhase', 'SamplingStage', 'RowVersion')) { continue }
            if (-not (Values-Equal $property.Value $new.($property.Name))) {
                throw "Unrelated field changed: $($property.Name) Id=$($new.Id)"
            }
        }
    }

    $lineTable = Invoke-Query @"
SELECT m.MachineCode AS LineCode, COUNT(*) AS Cnt
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE v.Id IN ($ids)
GROUP BY m.MachineCode
"@
    foreach ($row in $lineTable.Rows) {
        if ($row.LineCode -eq 'N1') { $n1 = [int]$row.Cnt }
        elseif ($row.LineCode -eq 'N2') { $n2 = [int]$row.Cnt }
    }
    if ($n1 -ne 16 -or $n2 -ne 182) { throw "Line counts mismatch N1=$n1 N2=$n2" }

    $remaining = Invoke-Query @"
SELECT COUNT(*) AS Remaining
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND LTRIM(RTRIM(v.SamplingPhase))='CLOSE'
  AND ISNULL(NULLIF(LTRIM(RTRIM(v.SamplingStage)),''),'GENERAL')='GENERAL'
"@
    if ([int]$remaining.Rows[0].Remaining -ne 0) { throw 'Remaining candidates not zero' }

    $openCloseAfter = Invoke-Query @"
SELECT COUNT(*) AS Cnt
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND v.SamplingPhase='OPEN'
  AND v.SamplingStage='CLOSE'
"@
    $openCloseAfterCount = [int]$openCloseAfter.Rows[0].Cnt
    if ($openCloseAfterCount -ne ($openCloseBeforeCount + $expectedCount)) {
        throw "OPEN+CLOSE count $($openCloseAfterCount) != $($openCloseBeforeCount + $expectedCount)"
    }

    $middleUnchanged = Invoke-Query @"
SELECT COUNT(*) AS Cnt
FROM VariableMeasurements v
JOIN PartProcessCharacteristics p ON p.Id=v.PartProcessCharacteristicId
JOIN Machines m ON m.Id=COALESCE(v.MachineId,p.MachineId)
WHERE p.ControlScope='CHEM'
  AND m.MachineCode IN ('N1','N2')
  AND v.SamplingPhase='MIDDLE'
"@

    [IO.File]::WriteAllText(
        (Join-Path $run 'after.json'),
        ($updated | ConvertTo-Json -Depth 6),
        [Text.UTF8Encoding]::new($false))

    $result = [ordered]@{
        Database = $expectedSpc
        Changed = $changed
        N1 = $n1
        N2 = $n2
        OpenCloseBefore = $openCloseBeforeCount
        OpenCloseAfter = $openCloseAfterCount
        RemainingCloseGeneral = 0
        MiddleUnchangedCount = [int]$middleUnchanged.Rows[0].Cnt
        UnrelatedBusinessFieldsUnchanged = $true
        RowVersion = 'SQL-managed increment'
        Committed = $true
        BackupProof = $BackupProof
    }
    [IO.File]::WriteAllText(
        (Join-Path $run 'result.json'),
        ($result | ConvertTo-Json -Depth 6),
        [Text.UTF8Encoding]::new($false))

    $transaction.Commit()
    Write-Output "Committed PRODUCTION $expectedSpc : $changed rows (N1=$n1 N2=$n2). Evidence: $run"
}
catch {
    try { $transaction.Rollback() } catch {}
    throw
}
finally {
    $connection.Dispose()
}
