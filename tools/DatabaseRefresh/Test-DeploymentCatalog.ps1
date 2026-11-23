[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourceConfigurationPath,
    [Parameter(Mandatory)][string]$TargetConfigurationPath
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Get-Catalog.ps1')
. (Join-Path $PSScriptRoot 'Compare-Catalog.ps1')

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
}
finally { $connection.Dispose() }

[pscustomobject]@{
    SourceDatabase = $comparison.SourceDatabase
    TargetDatabase = $comparison.TargetDatabase
    MigrationCount = $comparison.MigrationCount
    TableCount = $comparison.Tables.Count
    Policies = @($comparison.Tables | Group-Object Policy | Select-Object Name, Count)
    SourceWorkInventory = [pscustomobject]$inventory
    RequiresDispatchOrAttachmentIsolation = @($inventory.Values | Where-Object { $_ -gt 0 }).Count -gt 0
}
