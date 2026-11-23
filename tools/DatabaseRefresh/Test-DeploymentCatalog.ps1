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

[pscustomobject]@{
    SourceDatabase = $comparison.SourceDatabase
    TargetDatabase = $comparison.TargetDatabase
    MigrationCount = $comparison.MigrationCount
    TableCount = $comparison.Tables.Count
    Policies = @($comparison.Tables | Group-Object Policy | Select-Object Name, Count)
}
