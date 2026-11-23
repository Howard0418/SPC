function Get-RefreshOperatorMapping {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][array]$SourceOperators,
        [Parameter(Mandatory)][array]$TargetOperators,
        [Parameter(Mandatory)][AllowEmptyCollection()][int[]]$RequiredTargetIds,
        [hashtable]$TestOnlyOperators = @{}
    )

    $targetsById = @{}
    foreach ($operator in $TargetOperators) {
        if ($targetsById.ContainsKey([int]$operator.Id)) { throw 'Duplicate test operator ID.' }
        $targetsById.Add([int]$operator.Id, $operator)
    }
    $mappedIds = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($id in ($RequiredTargetIds | Sort-Object -Unique)) {
        $target = $targetsById[$id]
        if (!$target) { throw "Test operator reference $id does not exist." }
        $code = ([string]$target.OperatorCode).Trim()
        $username = ([string]$target.Username).Trim()
        $matches = @($SourceOperators | Where-Object {
            ($code -and ([string]$_.OperatorCode).Trim() -eq $code) -or
            ($username -and ([string]$_.Username).Trim() -eq $username)
        })
        if ($TestOnlyOperators.ContainsKey($id)) {
            if ($username -ine $TestOnlyOperators[$id] -or $matches.Count -gt 0 -or
                @($SourceOperators | Where-Object { [int]$_.Id -eq $id }).Count -gt 0) {
                throw "Test-only operator $id has a changed identity or source collision."
            }
            [pscustomobject]@{ TargetId = $id; SourceId = $null; RetainTestOnly = $true }
            continue
        }
        if ($matches.Count -eq 0) { throw "Test operator reference $id has no source match." }
        if ($matches.Count -gt 1) { throw "Test operator reference $id matches multiple source operators." }
        if (!$mappedIds.Add([int]$matches[0].Id)) { throw "Test operator reference $id shares a source match." }
        [pscustomobject]@{ TargetId = $id; SourceId = [int]$matches[0].Id; RetainTestOnly = $false }
    }
}
