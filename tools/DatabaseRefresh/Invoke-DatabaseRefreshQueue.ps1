[CmdletBinding(DefaultParameterSetName='Run')]
param(
    [Parameter(Mandatory,ParameterSetName='Publish')][switch]$PublishPreflight,
    [Parameter(Mandatory,ParameterSetName='Publish')]
    [ValidatePattern('^PMR_SPC_2026-REFRESH-SOURCE-[0-9]{4}-[a-f0-9]{32}\.bak$')][string]$SourceBackup,
    [Parameter(Mandatory,ParameterSetName='Publish')]
    [ValidatePattern('^PMR_SPC_TEST-REFRESH-IDENTITY-[0-9]{4}-[a-f0-9]{32}\.bak$')][string]$TargetBackup,
    [Parameter(Mandatory,ParameterSetName='Run')][ValidateSet('PMR_SPC_TEST')][string]$LiveTargetConfirmation,
    [Parameter(Mandatory,ParameterSetName='Run')][ValidateRange(1,2147483647)][int]$OperatorId
)

$ErrorActionPreference = 'Stop'
$paired = Join-Path $PSScriptRoot 'Test-PairedReplacement.ps1'
$module = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'RefreshQueue.ps1') -Raw
$hostName = 'PMR-SPC-SRV.pmr.com.tw'
$remote = {
    param($module,$action,$value,$source,$target)
    $ErrorActionPreference = 'Stop'
    if ($env:COMPUTERNAME -cne 'PMR-SPC-SRV') { throw 'Unexpected refresh queue host.' }
    . ([scriptblock]::Create($module))
    $root = 'C:/ProgramData/PMR-SPC-Refresh'
    switch ($action) {
        'publish' { Publish-RefreshQueueProof $root $value $source $target }
        'claim' { Claim-RefreshQueueJob $root $value }
        'complete' { Complete-RefreshQueueJob $root ([guid]$value) ([bool]$source) }
        default { throw 'Unknown refresh queue operation.' }
    }
}

if ($PublishPreflight) {
    $results = @(& $paired -SourceBackup $SourceBackup -TargetBackup $TargetBackup -ValidateLivePrerequisites)
    $proof = @($results | Where-Object { $_.Kind -eq 'ValidatedRefreshProof' })
    if ($proof.Count -ne 1) { throw 'Expected one validated rehearsal artifact.' }
    Invoke-Command -ComputerName $hostName -Authentication Kerberos -ScriptBlock $remote -ArgumentList $module,'publish',$proof[0],$SourceBackup,$TargetBackup
    return
}

$job = Invoke-Command -ComputerName $hostName -Authentication Kerberos -ScriptBlock $remote -ArgumentList $module,'claim',$OperatorId
if (!$job) {
    'No confirmed refresh job.'
    return
}
$succeeded = $false
try {
    & $paired -SourceBackup $job.SourceBackup -TargetBackup $job.TargetBackup -ExpectedSourceBackupId $job.BackupId -LiveTargetConfirmation $LiveTargetConfirmation
    $succeeded = $true
}
finally {
    Invoke-Command -ComputerName $hostName -Authentication Kerberos -ScriptBlock $remote -ArgumentList $module,'complete',$job.JobId,$succeeded
}
