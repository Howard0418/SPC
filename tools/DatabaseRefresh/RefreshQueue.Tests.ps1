$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'RefreshQueue.ps1')
$root = Join-Path ([IO.Path]::GetTempPath()) ('spc-queue-'+[guid]::NewGuid().ToString('N'))

function Reject([scriptblock]$Action) {
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    if (!$rejected) { throw 'Expected queue rejection.' }
}

try {
    foreach ($name in 'proof','state','queue') { $null = New-Item -ItemType Directory -Path (Join-Path $root $name) }
    $backup = @{ Kind='ValidatedRefreshProof'; BackupId=[guid]::NewGuid(); SnapshotUtc=[datetimeoffset]::UtcNow; ReplacementTables=69; MeasurementRows=10 }
    $source = 'PMR_SPC_2026-REFRESH-SOURCE-1125-'+[guid]::NewGuid().ToString('N')+'.bak'
    $target = 'PMR_SPC_TEST-REFRESH-IDENTITY-1125-'+[guid]::NewGuid().ToString('N')+'.bak'
    Publish-RefreshQueueProof $root $backup $source $target
    $digest = (Get-FileHash -LiteralPath (Join-Path $root 'proof/preflight.json')).Hash
    $id = [guid]::NewGuid()
    $queueFile = Join-Path $root ('queue/'+$id.ToString('N')+'.json')
    $job = @{ Id=$id; OperatorId=27; PreflightDigest=$digest; Status='queued'; TargetDatabase='PMR_SPC_TEST' }
    Write-RefreshJson $queueFile $job
    Reject { Claim-RefreshQueueJob $root 28 }
    $job.TargetDatabase = 'PMR_SPC_2026'
    Write-RefreshJson $queueFile $job
    Reject { Claim-RefreshQueueJob $root 27 }
    $job.TargetDatabase = 'PMR_SPC_TEST'
    $job.PreflightDigest = 'A' * 64
    Write-RefreshJson $queueFile $job
    Reject { Claim-RefreshQueueJob $root 27 }
    $job.PreflightDigest = $digest
    Write-RefreshJson $queueFile $job
    $claimed = Claim-RefreshQueueJob $root 27
    if ($claimed.JobId -ne $id -or $claimed.BackupId -ne $backup.BackupId -or
        (Read-RefreshJson $queueFile).Status -cne 'running') { throw 'Claim did not match.' }
    Reject { Claim-RefreshQueueJob $root 27 }
    Complete-RefreshQueueJob $root $id $true
    if ((Read-RefreshJson $queueFile).Status -cne 'succeeded') { throw 'Result was not recorded.' }
    Reject { Complete-RefreshQueueJob $root $id $false }
    Reject { Publish-RefreshQueueProof $root $backup $source $target }
    $fresh = $backup.Clone()
    $fresh.BackupId = [guid]::NewGuid()
    Publish-RefreshQueueProof $root $fresh $source $target
    $publicPath = Join-Path $root 'proof/preflight.json'
    $planPath = Join-Path $root 'state/plan.json'
    $proof = Read-RefreshJson $publicPath
    $proof.snapshotUtc = [datetimeoffset]::UtcNow.AddMinutes(-16).ToString('O')
    Write-RefreshJson $publicPath $proof
    $plan = Read-RefreshJson $planPath
    $plan.digest = (Get-FileHash -LiteralPath $publicPath).Hash
    Write-RefreshJson $planPath $plan
    $job.Id = [guid]::NewGuid()
    $job.PreflightDigest = $plan.digest
    $queueFile = Join-Path $root ('queue/'+$job.Id.ToString('N')+'.json')
    Write-RefreshJson $queueFile $job
    if ((Claim-RefreshQueueJob $root 27) -or (Read-RefreshJson $queueFile).Status -cne 'failed') {
        throw 'Expired job should terminate without execution.'
    }
    'Passed: proof publication, actor/target/digest rejection, single claim and result.'
}
finally {
    if (Test-Path -LiteralPath $root) {
        $resolved = (Resolve-Path -LiteralPath $root).Path
        if ($resolved -ceq $root -and (Split-Path $resolved -Leaf) -cmatch '^spc-queue-[a-f0-9]{32}$') {
            Remove-Item -LiteralPath $resolved -Recurse -Force
        }
    }
}
