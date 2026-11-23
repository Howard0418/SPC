[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourceConfigurationPath,
    [Parameter(Mandatory)][string]$TargetConfigurationPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Get-Catalog.ps1')
. (Join-Path $PSScriptRoot 'Compare-Catalog.ps1')
. (Join-Path $PSScriptRoot 'Operator-Mapping.ps1')

$source = Get-RefreshCatalog -ConfigurationPath $SourceConfigurationPath -ExpectedDatabase PMR_SPC_2026
$target = Get-RefreshCatalog -ConfigurationPath $TargetConfigurationPath -ExpectedDatabase PMR_SPC_TEST
$policies = Import-PowerShellDataFile (Join-Path $PSScriptRoot 'Table-Policies.psd1')
$comparison = Compare-RefreshCatalog -Source $source -Target $target -TablePolicies $policies

$config = Get-Content -LiteralPath $SourceConfigurationPath -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new(
    ($config.ConnectionStrings.SqlServer -replace '(?i)trust\s+server\s+certificate', 'TrustServerCertificate'))
if ($builder.InitialCatalog -cne $comparison.SourceDatabase -or
    $builder.DataSource -notmatch '^(tcp:)?172\.16\.110\.16(,1433)?$') {
    throw 'Unexpected refresh source SQL endpoint.'
}
$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $connection.Open()
    if ($connection.Database -cne 'PMR_SPC_2026') { throw 'Unexpected connected refresh source.' }
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 30
    $command.CommandText = @'
SELECT (SELECT COUNT_BIG(*) FROM dbo.MesSyncMessages WHERE SyncStatus = N'Pending'),
       (SELECT COUNT_BIG(*) FROM dbo.CalibrationNotifications
        WHERE State = N'Pending' OR (State = N'Failed' AND NextAttemptAt IS NOT NULL)),
       (SELECT COUNT_BIG(*) FROM dbo.ChemicalStatisticsJobs WHERE Status <> N'Completed'),
       (SELECT COUNT_BIG(*) FROM dbo.CalibrationCertificates);
'@
    $reader = $command.ExecuteReader()
    if (!$reader.Read()) { throw 'External work inventory returned no result.' }
    $inventory = [ordered]@{
        PendingMes = $reader.GetInt64(0)
        DeliverableCalibrationNotifications = $reader.GetInt64(1)
        IncompleteStatistics = $reader.GetInt64(2)
        CertificateIndexes = $reader.GetInt64(3)
    }
    $reader.Dispose()
    $command.CommandText = 'SELECT Id, OperatorCode, Username FROM dbo.Operators;'
    $reader = $command.ExecuteReader()
    $sourceOperators = @(while ($reader.Read()) {
        [pscustomobject]@{ Id = $reader.GetInt32(0); OperatorCode = $reader[1]; Username = $reader[2] }
    })
}
finally { $connection.Dispose() }

$config = Get-Content -LiteralPath $TargetConfigurationPath -Raw | ConvertFrom-Json
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new(
    ($config.ConnectionStrings.SqlServer -replace '(?i)trust\s+server\s+certificate', 'TrustServerCertificate'))
if ($builder.InitialCatalog -cne $comparison.TargetDatabase -or
    $builder.DataSource -notmatch '^(tcp:)?172\.16\.110\.16(,1433)?$') {
    throw 'Unexpected refresh target SQL endpoint.'
}
$connection = [System.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
try {
    $connection.Open()
    if ($connection.Database -cne 'PMR_SPC_TEST') { throw 'Unexpected connected refresh target.' }
    $command = $connection.CreateCommand()
    $command.CommandTimeout = 30
    $command.CommandText = @'
SELECT Id, OperatorCode, Username FROM dbo.Operators;
SELECT Id FROM dbo.Operators WHERE NULLIF(LTRIM(RTRIM(PasswordHash)), N'') IS NOT NULL
UNION
SELECT CONVERT(int,j.value) FROM dbo.SpcReportSchedules CROSS APPLY OPENJSON(RecipientOperatorIdsJson) j
UNION
SELECT CONVERT(int,j.value) FROM dbo.SpcAlertNotificationSettings CROSS APPLY OPENJSON(RecipientOperatorIdsJson) j;
'@
    $reader = $command.ExecuteReader()
    $targetOperators = @(while ($reader.Read()) {
        [pscustomobject]@{ Id = $reader.GetInt32(0); OperatorCode = $reader[1]; Username = $reader[2] }
    })
    if (!$reader.NextResult()) { throw 'Missing retained test operator references.' }
    $requiredIds = @(while ($reader.Read()) { $reader.GetInt32(0) })
}
finally { $connection.Dispose() }
$operatorMapping = @(Get-RefreshOperatorMapping $sourceOperators $targetOperators $requiredIds)

[pscustomobject]@{
    SourceDatabase = $comparison.SourceDatabase
    TargetDatabase = $comparison.TargetDatabase
    MigrationCount = $comparison.MigrationCount
    TableCount = $comparison.Tables.Count
    Policies = @($comparison.Tables | Group-Object Policy | Select-Object Name, Count)
    SourceWorkInventory = [pscustomobject]$inventory
    RetainedTestOperatorMappings = $operatorMapping.Count
    RequiresDispatchOrAttachmentIsolation = @($inventory.Values | Where-Object { $_ -gt 0 }).Count -gt 0
}
