. (Join-Path $PSScriptRoot 'Invoke-RefreshLifecycle.ps1')
. (Join-Path $PSScriptRoot 'Invoke-IsolatedReplacement.ps1')
. (Join-Path $PSScriptRoot 'Refresh-Lock.ps1')
. (Join-Path $PSScriptRoot 'Operator-Mapping.ps1')

function Invoke-LiveTestRefreshMaintenance {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][guid]$OperationId,
        [Parameter(Mandatory)][guid]$SourceBackupId,
        [Parameter(Mandatory)][hashtable]$ReplacementPlan,
        [switch]$ValidateOnly
    )

    $ErrorActionPreference = 'Stop'
    if ($Connection.State -ne 'Open' -or $Connection.Database -cne 'master' -or
        $Connection.DataSource -cnotmatch '^(tcp:)?172\.16\.110\.16(,1433)?$' -or
        $OperationId -eq [guid]::Empty -or $SourceBackupId -eq [guid]::Empty) {
        throw 'Refresh maintenance requires the fixed SQL endpoint and a master connection.'
    }
    $plan = @{}
    foreach ($key in $ReplacementPlan.Keys) {
        if ($key -notin 'InsertOrder','Columns','IdentityTables','MaintenanceTriggers','RetainedTables') {
            throw 'Unexpected replacement plan argument.'
        }
        $plan[$key] = $ReplacementPlan[$key]
    }
    if ($plan.InsertOrder.Count -ne 69 -or !$plan.Columns -or !$plan.RetainedTables -or !$plan.MaintenanceTriggers) {
        throw 'The full server replacement plan is required.'
    }

    function Invoke-MaintenanceSql([string]$sql, [string]$path) {
        $command = $Connection.CreateCommand()
        try {
            $command.CommandTimeout = 600
            $command.CommandText = $sql
            if ($path) { $null = $command.Parameters.AddWithValue('@path', $path) }
            $command.ExecuteScalar()
        }
        finally { $command.Dispose() }
    }

    function Read-MaintenanceRows([string]$sql) {
        $command = $Connection.CreateCommand()
        try {
            $command.CommandTimeout = 60
            $command.CommandText = $sql
            $rows = [System.Data.DataTable]::new()
            $rows.Load($command.ExecuteReader())
            $rows.Rows
        }
        finally { $command.Dispose() }
    }

    $snapshot = 'SPC_Refresh_Rehearsal_Snapshot_' + $OperationId.ToString('N')
    $null = Invoke-MaintenanceSql @"
IF COALESCE(IS_SRVROLEMEMBER('sysadmin'),0) <> 1
    THROW 51044,'SQL maintenance authority required',1;
DECLARE @r int;
EXEC @r = sys.sp_getapplock @Resource=N'SPC.LiveTestRefresh', @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=0;
IF @r < 0 THROW 51047,'Another live test refresh holds the maintenance lock',1;
"@
    $baselineQuery = @'
SELECT CONVERT(varchar(64),HASHBYTES('SHA2_256',COALESCE(
    (SELECT Id FROM PMR_SPC_TEST.dbo.VariableMeasurements ORDER BY Id FOR JSON PATH),N'[]')),2);
'@
    try {
        $null = Invoke-MaintenanceSql @"
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name=N'PMR_SPC_TEST'
    AND state_desc=N'ONLINE' AND user_access_desc=N'MULTI_USER' AND is_read_only=0)
    THROW 51045,'Test database must be online and in normal access',1;
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name=N'$snapshot'
    AND state_desc=N'ONLINE' AND user_access_desc=N'RESTRICTED_USER' AND is_read_only=1)
   OR NOT EXISTS (
       SELECT 1 FROM msdb.dbo.restorehistory r JOIN msdb.dbo.backupset b ON b.backup_set_id=r.backup_set_id
       WHERE r.destination_database_name=N'$snapshot' AND r.restore_type='D'
         AND b.backup_set_uuid=CONVERT(uniqueidentifier,'$SourceBackupId') AND b.database_name=N'PMR_SPC_2026'
         AND b.type='D' AND b.is_copy_only=1 AND b.has_backup_checksums=1
         AND b.backup_finish_date BETWEEN DATEADD(minute,-60,GETDATE()) AND GETDATE()
         AND r.restore_history_id=(SELECT MAX(x.restore_history_id) FROM msdb.dbo.restorehistory x
                                  WHERE x.destination_database_name=N'$snapshot'))
    THROW 51046,'Prepared read-only production snapshot required',1;
"@
        $null = Invoke-MaintenanceSql @'
DECLARE @root nvarchar(4000)=CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'));
DECLARE @required bigint=(SELECT SUM(CONVERT(bigint,size))*8192 FROM sys.master_files
                           WHERE database_id=DB_ID(N'PMR_SPC_TEST'));
IF @root IS NULL OR @required IS NULL OR NOT EXISTS (
    SELECT 1 FROM sys.dm_os_enumerate_fixed_drives
    WHERE LEFT(@root,LEN(fixed_drive_path))=fixed_drive_path AND free_space_in_bytes>=@required+1073741824)
    THROW 51049,'Insufficient verified capacity for the test recovery backup',1;
'@
        if ($ValidateOnly) {
            'Live test maintenance prerequisites passed without changing database access.'
            return
        }
        Invoke-RefreshLifecycle -EnterMaintenance {
            $null = Invoke-MaintenanceSql 'ALTER DATABASE [PMR_SPC_TEST] SET RESTRICTED_USER WITH ROLLBACK IMMEDIATE;'
        } -CreateRecoveryBackup {
            $stopTime = Invoke-MaintenanceSql 'SELECT SYSDATETIME();'
            $baseline = Invoke-MaintenanceSql $baselineQuery
            $root = Invoke-MaintenanceSql "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'));"
            if (!$root -or !$baseline) { throw 'Recovery baseline or backup directory unavailable.' }
            $file = Join-Path $root ('PMR_SPC_TEST-REFRESH-RECOVERY-' + $OperationId.ToString('N') + '.bak')
            $null = Invoke-MaintenanceSql @'
IF EXISTS (SELECT 1 FROM msdb.dbo.backupmediafamily WHERE physical_device_name=@path)
    THROW 51048,'Recovery backup path already used',1;
BACKUP DATABASE [PMR_SPC_TEST] TO DISK=@path WITH COPY_ONLY,CHECKSUM;
RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM;
'@ $file
            [pscustomobject]@{ Path=$file; Started=$stopTime; Baseline=$baseline }
        } -VerifyReadiness {
            param($maintenance,$backup)
            if (!$backup.Path -or !$backup.Baseline) { throw 'Verified backup context required.' }
            $null = Invoke-MaintenanceSql @'
IF EXISTS (SELECT 1 FROM sys.dm_exec_sessions WHERE is_user_process=1
    AND database_id=DB_ID(N'PMR_SPC_TEST') AND session_id<>@@SPID)
    THROW 51050,'A competing test database session is still active',1;
'@
            $sourceOperators = @(Read-MaintenanceRows "SELECT Id,OperatorCode,Username FROM [$snapshot].dbo.Operators;")
            $targetOperators = @(Read-MaintenanceRows 'SELECT Id,OperatorCode,Username FROM PMR_SPC_TEST.dbo.Operators;')
            $requiredIds = @(Read-MaintenanceRows @'
SELECT Id FROM PMR_SPC_TEST.dbo.Operators WHERE NULLIF(LTRIM(RTRIM(PasswordHash)),N'') IS NOT NULL
UNION SELECT CONVERT(int,j.value) FROM PMR_SPC_TEST.dbo.SpcReportSchedules CROSS APPLY OPENJSON(RecipientOperatorIdsJson) j
UNION SELECT CONVERT(int,j.value) FROM PMR_SPC_TEST.dbo.SpcAlertNotificationSettings CROSS APPLY OPENJSON(RecipientOperatorIdsJson) j
UNION SELECT 18;
'@ | ForEach-Object { [int]$_.Id })
            $plan.OperatorMapping = @(Get-RefreshOperatorMapping $sourceOperators $targetOperators $requiredIds -TestOnlyOperators @{18='P22077'})
        } -ReplaceData {
            param($maintenance,$backup)
            $Connection.ChangeDatabase('PMR_SPC_TEST')
            try {
                Invoke-WithRefreshOperationLock $Connection {
                    Invoke-IsolatedReplacement -Connection $Connection -OperationId $OperationId -SourceBackupId $SourceBackupId -RecoveryBackupPath $backup.Path -MaintenanceStartedAtSql $backup.Started @plan
                }
            }
            finally { $Connection.ChangeDatabase('master') }
        } -VerifyReplacement {
            $eligible = Invoke-MaintenanceSql "SELECT COUNT_BIG(*) FROM PMR_SPC_TEST.dbo.ChemicalStatisticsJobs WHERE Status<>N'Completed' AND (NextAttemptAtUtc IS NULL OR NextAttemptAtUtc<=SYSUTCDATETIME());"
            if ($eligible -ne 0) { throw 'Test statistics jobs are eligible to retry.' }
        } -RestoreAndVerify {
            param($maintenance,$backup)
            $Connection.ChangeDatabase('master')
            $null = Invoke-MaintenanceSql 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM; RESTORE DATABASE [PMR_SPC_TEST] FROM DISK=@path WITH REPLACE,CHECKSUM,RESTRICTED_USER;' $backup.Path
            if ((Invoke-MaintenanceSql $baselineQuery) -cne $backup.Baseline) { throw 'Restored measurement baseline does not match.' }
        } -ExitMaintenance {
            $null = Invoke-MaintenanceSql 'ALTER DATABASE [PMR_SPC_TEST] SET MULTI_USER;'
        }
    }
    finally {
        if ($Connection.State -eq 'Open') {
            $Connection.ChangeDatabase('master')
            $null = Invoke-MaintenanceSql "EXEC sys.sp_releaseapplock @Resource=N'SPC.LiveTestRefresh', @LockOwner='Session';"
        }
    }
}
