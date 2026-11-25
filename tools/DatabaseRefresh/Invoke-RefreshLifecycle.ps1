function Invoke-RefreshLifecycle {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][scriptblock]$EnterMaintenance,
        [Parameter(Mandatory)][scriptblock]$CreateRecoveryBackup,
        [Parameter(Mandatory)][scriptblock]$VerifyReadiness,
        [Parameter(Mandatory)][scriptblock]$ReplaceData,
        [Parameter(Mandatory)][scriptblock]$VerifyReplacement,
        [Parameter(Mandatory)][scriptblock]$RestoreAndVerify,
        [Parameter(Mandatory)][scriptblock]$ExitMaintenance
    )

    $ErrorActionPreference = 'Stop'
    # The maintenance adapter must verify restricted access and revert it if entry fails.
    $maintenance = & $EnterMaintenance
    $replacementStarted = $false
    try {
        $backup = & $CreateRecoveryBackup $maintenance
        & $VerifyReadiness $maintenance $backup
        $replacementStarted = $true
        & $ReplaceData $maintenance $backup
        & $VerifyReplacement $maintenance
    }
    catch {
        $failure = $_
        if ($replacementStarted) {
            try { & $RestoreAndVerify $maintenance $backup }
            catch { throw [InvalidOperationException]::new(
                'Refresh recovery failed. Test writes must remain paused for manual recovery.', $_.Exception) }
        }
        & $ExitMaintenance $maintenance
        throw $failure
    }
    & $ExitMaintenance $maintenance
}
