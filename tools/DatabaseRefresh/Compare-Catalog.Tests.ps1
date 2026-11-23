param([Parameter(Mandatory)][string]$Root)
$ErrorActionPreference = 'Stop'
. (Join-Path $Root 'Compare-Catalog.ps1')

function Assert-Equal($Actual, $Expected) {
    if ($Actual -cne $Expected) { throw "Expected '$Expected'; got '$Actual'." }
}
function Assert-Rejected([scriptblock]$Test, [string]$Expected) {
    try { & $Test | Out-Null }
    catch {
        if ($_.Exception.Message -like "*$Expected*") { return }
        throw
    }
    throw "Expected rejection: $Expected"
}
function New-Case([string]$Database) {
    @{
        Database = $Database
        Server = 'SQL-SYNTHETIC'
        Collation = 'Latin1_General_100_CI_AS'
        Migrations = @('20261120134446_AllowBusinessActionClientEvent')
        Tables = @(@{Name = 'Operators'; Signature = 'operator-v1'; Rows = 2},
                    @{Name = 'AuditEvents'; Signature = 'audit-v1'; Rows = 5},
                    @{Name = 'SpcAlertNotificationSettings'; Signature = 'alert-v1'; Rows = 1})
    }
}

$source = New-Case 'PMR_SPC_2026'
$target = New-Case 'PMR_SPC_TEST'
$policies = @{Operators = 'PreserveTestCredentials'; AuditEvents = 'CopyHistoricalAudit'; SpcAlertNotificationSettings = 'KeepTestSettings'}
$plan = Compare-RefreshCatalog $source $target $policies
Assert-Equal $plan.SourceDatabase 'PMR_SPC_2026'
Assert-Equal $plan.TargetDatabase 'PMR_SPC_TEST'
Assert-Equal @($plan.Tables).Count 3
Assert-Equal ($plan.Tables | Where-Object Name -eq 'Operators').Policy 'PreserveTestCredentials'
Assert-Rejected { Compare-RefreshCatalog $target $source $policies } 'direction'
Assert-Rejected { Compare-RefreshCatalog $source $target @{Operators = 'ReplaceBusinessData'} } 'classification'

$target.Tables[0].Signature = 'drift'
Assert-Rejected { Compare-RefreshCatalog $source $target $policies } 'Operators'
$target = New-Case 'PMR_SPC_TEST'
$target.Migrations = @('different')
Assert-Rejected { Compare-RefreshCatalog $source $target $policies } 'migration'
$target = New-Case 'PMR_SPC_TEST'
$target.Tables += $target.Tables[0]
Assert-Rejected { Compare-RefreshCatalog $source $target $policies } 'duplicate'
$target = New-Case 'PMR_SPC_TEST'
$target.Server = 'OTHER'
Assert-Rejected { Compare-RefreshCatalog $source $target $policies } 'server'
'7 refresh catalogue checks passed.'
