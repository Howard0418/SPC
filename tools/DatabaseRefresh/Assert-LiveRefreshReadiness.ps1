function Assert-LiveRefreshReadiness {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][guid]$OperationId,
        [Parameter(Mandatory)][guid]$SourceBackupId,
        [Parameter(Mandatory)][string]$RecoveryBackupPath,
        [Parameter(Mandatory)][datetime]$MaintenanceStartedAtSql
    )

    if ($Connection.State -ne [System.Data.ConnectionState]::Open -or
        $Connection.DataSource -cnotmatch '^(tcp:)?172\.16\.110\.16(,1433)?$' -or
        $Connection.Database -cne 'PMR_SPC_TEST') {
        throw 'The live refresh guard requires the fixed test database connection.'
    }
    $snapshot = 'SPC_Refresh_Rehearsal_Snapshot_' + $OperationId.ToString('N')
    $expectedFile = 'PMR_SPC_TEST-REFRESH-RECOVERY-' + $OperationId.ToString('N') + '.bak'
    if ([IO.Path]::GetFileName($RecoveryBackupPath) -cne $expectedFile -or
        $RecoveryBackupPath -cnotmatch '^[A-Za-z]:[/\\]' -or $SourceBackupId -eq [guid]::Empty) {
        throw 'Invalid refresh recovery backup or source backup identity.'
    }

    $command = $Connection.CreateCommand()
    try {
        $command.CommandTimeout = 600
        $null = $command.Parameters.AddWithValue('@snapshot', $snapshot)
        $null = $command.Parameters.AddWithValue('@sourceBackup', $SourceBackupId)
        $null = $command.Parameters.AddWithValue('@backupPath', $RecoveryBackupPath)
        $null = $command.Parameters.AddWithValue('@fileName', $expectedFile)
        $started = $command.Parameters.Add('@maintenanceStarted', [System.Data.SqlDbType]::DateTime2)
        $started.Value = $MaintenanceStartedAtSql
        $command.CommandText = @"
IF DB_NAME() <> N'PMR_SPC_TEST' OR
   COALESCE(APPLOCK_MODE('public', N'SPC.DatabaseRefresh','Session'),N'NoLock') <> N'Exclusive'
    THROW 51040,'Fixed test target and exclusive session lock required',1;
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name=DB_NAME()
              AND state_desc=N'ONLINE' AND user_access_desc=N'RESTRICTED_USER' AND is_read_only=0)
    THROW 51041,'Test database must be in restricted writable maintenance mode',1;
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name=@snapshot
              AND state_desc=N'ONLINE' AND user_access_desc=N'RESTRICTED_USER' AND is_read_only=1)
   OR NOT EXISTS (
       SELECT 1 FROM msdb.dbo.restorehistory r
       JOIN msdb.dbo.backupset b ON b.backup_set_id=r.backup_set_id
       WHERE r.destination_database_name=@snapshot AND r.restore_type='D'
         AND b.backup_set_uuid=@sourceBackup AND b.database_name=N'PMR_SPC_2026'
         AND b.type='D' AND b.is_copy_only=1 AND b.has_backup_checksums=1
         AND r.restore_history_id=(SELECT MAX(x.restore_history_id) FROM msdb.dbo.restorehistory x
                                  WHERE x.destination_database_name=@snapshot)
   )
    THROW 51042,'Verified read-only production snapshot required',1;
DECLARE @backupRoot nvarchar(4000)=CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'));
IF @backupRoot IS NULL OR REPLACE(@backupPath,N'/',N'\') <>
       REPLACE(@backupRoot,N'/',N'\') + CASE WHEN RIGHT(@backupRoot,1) IN (N'\',N'/') THEN N'' ELSE N'\' END + @fileName
   OR NOT EXISTS (
       SELECT 1 FROM msdb.dbo.backupset b
       JOIN msdb.dbo.backupmediafamily f ON f.media_set_id=b.media_set_id
       WHERE b.database_name=N'PMR_SPC_TEST' AND b.type='D' AND b.is_copy_only=1
         AND b.has_backup_checksums=1 AND b.backup_start_date>=DATEADD(second,-2,@maintenanceStarted)
         AND f.physical_device_name=@backupPath
   )
    THROW 51043,'Current-operation test recovery backup required',1;
RESTORE VERIFYONLY FROM DISK=@backupPath WITH CHECKSUM;
"@
        $null = $command.ExecuteNonQuery()
        $snapshot
    }
    finally { $command.Dispose() }
}
