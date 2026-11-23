function Set-RefreshOperationLock {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][ValidateSet('Acquire', 'Release')][string]$Action
    )

    if ($Connection.State -ne [System.Data.ConnectionState]::Open -or
        $Connection.Database -cnotmatch '^(PMR_SPC_TEST|SPC_Refresh_Rehearsal_Target_[a-f0-9]{32})$') {
        throw 'The refresh lock requires an open test-target connection.'
    }

    $command = $Connection.CreateCommand()
    try {
        $command.CommandTimeout = 5
        $command.CommandText = if ($Action -eq 'Acquire') {
            "DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource=N'SPC.DatabaseRefresh', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=0; SELECT @result;"
        } else {
            "DECLARE @result int; EXEC @result = sys.sp_releaseapplock @Resource=N'SPC.DatabaseRefresh', @LockOwner='Session'; SELECT @result;"
        }
        $result = [int]$command.ExecuteScalar()
        if ($result -eq -1 -and $Action -eq 'Acquire') { throw 'A refresh operation already holds the test database lock.' }
        if ($result -lt 0) { throw "Refresh operation lock $Action failed ($result)." }
    }
    finally { $command.Dispose() }
}

function Invoke-WithRefreshOperationLock {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][scriptblock]$Operation
    )

    Set-RefreshOperationLock -Connection $Connection -Action Acquire
    try { & $Operation }
    finally {
        if ($Connection.State -eq [System.Data.ConnectionState]::Open) {
            Set-RefreshOperationLock -Connection $Connection -Action Release
        }
    }
}
