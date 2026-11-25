$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Invoke-RefreshLifecycle.ps1')
foreach ($scenario in 'Success','BackupFailure','GuardFailure','ReplaceFailure','VerificationFailure','RecoveryFailure') {
    $steps = [Collections.Generic.List[string]]::new()
    $operations = @{
        EnterMaintenance = { $steps.Add('pause'); 'maintenance' }
        CreateRecoveryBackup = { $steps.Add('backup'); if ($scenario -eq 'BackupFailure') { throw 'backup failed' }; 'backup' }
        VerifyReadiness = { param($mode,$backup); if ($mode -ne 'maintenance' -or $backup -ne 'backup') { throw 'context missing' }; $steps.Add('guard'); if ($scenario -eq 'GuardFailure') { throw 'guard failed' } }
        ReplaceData = { $steps.Add('replace'); if ($scenario -eq 'ReplaceFailure') { throw 'replace failed' } }
        VerifyReplacement = { $steps.Add('verify'); if ($scenario -in 'VerificationFailure','RecoveryFailure') { throw 'verification failed' } }
        RestoreAndVerify = { $steps.Add('restore'); if ($scenario -eq 'RecoveryFailure') { throw 'restore failed' } }
        ExitMaintenance = { $steps.Add('resume') }
    }
    $failure = try { Invoke-RefreshLifecycle @operations; '' } catch { $_.Exception.Message }
    $expected = switch ($scenario) {
        'Success' { @('pause,backup,guard,replace,verify,resume','') }
        'BackupFailure' { @('pause,backup,resume','backup failed') }
        'GuardFailure' { @('pause,backup,guard,resume','guard failed') }
        'ReplaceFailure' { @('pause,backup,guard,replace,restore,resume','replace failed') }
        'VerificationFailure' { @('pause,backup,guard,replace,verify,restore,resume','verification failed') }
        'RecoveryFailure' { @('pause,backup,guard,replace,verify,restore','Refresh recovery failed. Test writes must remain paused for manual recovery.') }
    }
    if (($steps -join ',') -cne $expected[0] -or $failure -cne $expected[1]) {
        throw "Lifecycle scenario failed: $scenario. Steps: $($steps -join ',')."
    }
    Write-Output "Passed: $scenario"
}
