function Assert-RefreshQueuePaths([string]$Root) {
    foreach ($name in 'proof','state','queue') {
        $current = Get-Item -LiteralPath (Join-Path $Root $name) -ErrorAction Stop
        if (!$current.PSIsContainer) { throw 'Refresh storage must be a directory.' }
        while ($current) {
            if ($current.Attributes -band [IO.FileAttributes]::ReparsePoint) {
                throw 'Refresh storage cannot use reparse points.'
            }
            $current = $current.Parent
        }
    }
}

function Open-RefreshQueueLease([string]$Root) {
    [IO.File]::Open((Join-Path $Root 'queue/handoff.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
}

function Write-RefreshJson([string]$Path, $Value) {
    $temporary = $Path + '.' + [guid]::NewGuid().ToString('N') + '.tmp'
    try {
        [IO.File]::WriteAllText($temporary, ($Value | ConvertTo-Json -Depth 8 -Compress), [Text.UTF8Encoding]::new($false))
        if ([IO.File]::Exists($Path)) {
            [IO.File]::Replace($temporary, $Path, [NullString]::Value)
        }
        else {
            [IO.File]::Move($temporary, $Path)
        }
    }
    finally {
        if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
    }
}

function Read-RefreshJson([string]$Path) {
    $item = Get-Item -LiteralPath $Path -ErrorAction Stop
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -or $item.Length -gt 4096) {
        throw 'Invalid refresh queue file.'
    }
    Get-Content -LiteralPath $item.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
}

function Publish-RefreshQueueProof([string]$Root, $Validated, [string]$SourceBackup, [string]$TargetBackup) {
    Assert-RefreshQueuePaths $Root
    $lease = Open-RefreshQueueLease $Root
    try {
        $active = @(Get-ChildItem -LiteralPath (Join-Path $Root 'queue') -File -Filter '*.json' |
            ForEach-Object { Read-RefreshJson $_.FullName } | Where-Object { $_.Status -in 'queued','running' })
        if ($active.Count) { throw 'A refresh job is already active.' }
        $snapshotUtc = [datetimeoffset]$Validated.SnapshotUtc
        $now = [datetimeoffset]::UtcNow
        if ($Validated.Kind -cne 'ValidatedRefreshProof' -or [guid]$Validated.BackupId -eq [guid]::Empty -or
            $Validated.ReplacementTables -ne 69 -or $Validated.MeasurementRows -le 0 -or
            $snapshotUtc -le $now.AddMinutes(-15) -or $snapshotUtc -gt $now) {
            throw 'The rehearsal did not produce a current validated snapshot.'
        }
        $id = [guid]$Validated.BackupId
        $receipt = Join-Path $Root ('state/claimed-' + $id.ToString('N'))
        if ([IO.File]::Exists($receipt)) { throw 'This source backup has already been claimed.' }
        $proof = @{
            operationId=$id; source='PMR_SPC_2026'; target='PMR_SPC_TEST'
            snapshotUtc=$snapshotUtc.ToString('O'); replacementTables=69
            measurementRows=[long]$Validated.MeasurementRows; blockers=0
        }
        $public = Join-Path $Root 'proof/preflight.json'
        Write-RefreshJson $public $proof
        $digest = (Get-FileHash -LiteralPath $public -Algorithm SHA256).Hash
        Write-RefreshJson (Join-Path $Root 'state/plan.json') @{
            backupId=$id; sourceBackup=$SourceBackup; targetBackup=$TargetBackup; digest=$digest
        }
        "Preflight published for backup $id."
    }
    finally { $lease.Dispose() }
}

function Claim-RefreshQueueJob([string]$Root, [int]$OperatorId) {
    Assert-RefreshQueuePaths $Root
    $lease = Open-RefreshQueueLease $Root
    try {
        $jobs = @(Get-ChildItem -LiteralPath (Join-Path $Root 'queue') -File -Filter '*.json' |
            ForEach-Object { [pscustomobject]@{ File=$_; Job=(Read-RefreshJson $_.FullName) } } |
            Where-Object { $_.Job.Status -in 'queued','running' })
        if (!$jobs.Count) { return }
        if ($jobs.Count -ne 1 -or $jobs[0].Job.Status -cne 'queued') {
            throw 'A running or inconsistent refresh job requires review.'
        }
        $item = $jobs[0]
        $job = $item.Job
        $jobId = [guid]$job.Id
        if ($jobId -eq [guid]::Empty -or $item.File.Name -cne ($jobId.ToString('N')+'.json') -or
            $job.OperatorId -ne $OperatorId -or $job.TargetDatabase -cne 'PMR_SPC_TEST') {
            throw 'Refresh job identity or target mismatch.'
        }
        $public = Join-Path $Root 'proof/preflight.json'
        $proof = Read-RefreshJson $public
        $plan = Read-RefreshJson (Join-Path $Root 'state/plan.json')
        $id = [guid]$plan.backupId
        $snapshotUtc = [datetimeoffset]$proof.snapshotUtc
        $now = [datetimeoffset]::UtcNow
        if ($id -eq [guid]::Empty -or $id -ne [guid]$proof.operationId -or
            $proof.source -cne 'PMR_SPC_2026' -or $proof.target -cne 'PMR_SPC_TEST' -or
            $proof.replacementTables -ne 69 -or $proof.measurementRows -le 0 -or $proof.blockers -ne 0 -or
            $snapshotUtc -gt $now -or
            $job.PreflightDigest -cne $plan.digest -or $plan.digest -cne (Get-FileHash -LiteralPath $public -Algorithm SHA256).Hash -or
            $plan.sourceBackup -cnotmatch '^PMR_SPC_2026-REFRESH-SOURCE-[0-9]{4}-[a-f0-9]{32}\.bak$' -or
            $plan.targetBackup -cnotmatch '^PMR_SPC_TEST-REFRESH-IDENTITY-[0-9]{4}-[a-f0-9]{32}\.bak$') {
            throw 'Refresh preflight has expired or does not match the approved job.'
        }
        if ($snapshotUtc -le $now.AddMinutes(-15)) {
            $job.Status = 'failed'
            Write-RefreshJson $item.File.FullName $job
            return
        }
        $receipt = [IO.File]::Open((Join-Path $Root ('state/claimed-' + $id.ToString('N'))), 'CreateNew', 'Write', 'None')
        $receipt.Dispose()
        $job.Status = 'running'
        Write-RefreshJson $item.File.FullName $job
        [pscustomobject]@{ JobId=$jobId; BackupId=$id; SourceBackup=$plan.sourceBackup; TargetBackup=$plan.targetBackup }
    }
    finally { $lease.Dispose() }
}

function Complete-RefreshQueueJob([string]$Root, [guid]$JobId, [bool]$Succeeded) {
    Assert-RefreshQueuePaths $Root
    $lease = Open-RefreshQueueLease $Root
    try {
        $path = Join-Path $Root ('queue/' + $JobId.ToString('N') + '.json')
        $job = Read-RefreshJson $path
        if ([guid]$job.Id -ne $JobId -or $job.Status -cne 'running') {
            throw 'Refresh result does not match the running job.'
        }
        $job.Status = if ($Succeeded) { 'succeeded' } else { 'failed' }
        Write-RefreshJson $path $job
    }
    finally { $lease.Dispose() }
}
