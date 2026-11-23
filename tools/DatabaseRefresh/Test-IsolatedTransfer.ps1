[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Refresh-Lock.ps1')
$connection = [System.Data.SqlClient.SqlConnection]::new(
    'Server=localhost;Database=master;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=5')
$suffix = [guid]::NewGuid().ToString('N')
$source = "SPC_Refresh_Rehearsal_Source_$suffix"
$target = "SPC_Refresh_Rehearsal_Target_$suffix"
$snapshot = "SPC_Refresh_Rehearsal_Snapshot_$suffix"
$created = [System.Collections.Generic.List[string]]::new()
$backupFile = $null

function Invoke-Sql([string]$Text, [string]$Label) {
    $command = $connection.CreateCommand()
    try {
        $command.CommandTimeout = 60
        $command.CommandText = $Text
        if ($PSBoundParameters.ContainsKey('Label')) {
            $parameter = $command.Parameters.Add('@label', [System.Data.SqlDbType]::NVarChar, 40)
            $parameter.Value = $Label
        }
        $command.ExecuteScalar()
    }
    finally { $command.Dispose() }
}

try {
    $connection.Open()
    $machine = Invoke-Sql "SELECT CONVERT(nvarchar(128), SERVERPROPERTY('MachineName'));"
    if ($machine -ine $env:COMPUTERNAME -or $machine -ine 'PC-PA16560048') {
        throw 'Rehearsal requires the isolated local SQL Server.'
    }
    foreach ($db in @($source, $target)) {
        $null = Invoke-Sql "CREATE DATABASE [$db];"
        $created.Add($db)
        $null = Invoke-Sql @"
USE [$db];
CREATE TABLE dbo.Operations(Id int NOT NULL PRIMARY KEY, Label nvarchar(40) NOT NULL);
CREATE TABLE dbo.EnvironmentSettings(Id int NOT NULL PRIMARY KEY, Mode nvarchar(10) NOT NULL);
INSERT INTO dbo.Operations VALUES(1, 'Baseline');
INSERT INTO dbo.EnvironmentSettings VALUES(1, 'test');
"@
    }

    $connection.ChangeDatabase($target)
    $competitorConfig = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connection.ConnectionString)
    $competitorConfig['Initial Catalog'] = $target
    $competitorConfig['Pooling'] = $false
    $competitor = [System.Data.SqlClient.SqlConnection]::new($competitorConfig.ConnectionString)
    try {
        $competitor.Open()
        Set-RefreshOperationLock -Connection $connection -Action Acquire
        try {
            try {
                Set-RefreshOperationLock -Connection $competitor -Action Acquire
                throw 'A competing connection unexpectedly obtained the operation lock.'
            }
            catch {
                if ($_.Exception.Message -cne 'A refresh operation already holds the test database lock.') { throw }
            }
        }
        finally { Set-RefreshOperationLock -Connection $connection -Action Release }
        Set-RefreshOperationLock -Connection $competitor -Action Acquire
        Set-RefreshOperationLock -Connection $competitor -Action Release
    }
    finally {
        $competitor.Dispose()
        $connection.ChangeDatabase('master')
    }

    $null = Invoke-Sql @"
USE [$source];
UPDATE dbo.Operations SET Label='Source';
CREATE USER RefreshSource WITHOUT LOGIN;
GRANT SELECT ON dbo.Operations TO RefreshSource;
GRANT BACKUP DATABASE TO RefreshSource;
EXECUTE AS USER='RefreshSource';
DECLARE @sourceWrite int = HAS_PERMS_BY_NAME('dbo.Operations','OBJECT','UPDATE');
IF @sourceWrite <> 0
BEGIN
    REVERT;
    THROW 51000, 'Source principal has write permission', 1;
END;
REVERT;
USE [$target];
CREATE USER RefreshTarget WITHOUT LOGIN;
GRANT SELECT, INSERT, DELETE ON dbo.Operations TO RefreshTarget;
GRANT SELECT ON dbo.EnvironmentSettings TO RefreshTarget;
EXECUTE AS USER='RefreshTarget';
DECLARE @allowed int = HAS_PERMS_BY_NAME('dbo.EnvironmentSettings','OBJECT','UPDATE')
    + HAS_PERMS_BY_NAME('dbo.EnvironmentSettings','OBJECT','INSERT')
    + HAS_PERMS_BY_NAME('dbo.EnvironmentSettings','OBJECT','DELETE')
    + HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER');
REVERT;
IF @allowed <> 0 THROW 51004, 'Target principal can modify protected settings or schema', 1;
"@
    $backupRoot = Invoke-Sql "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultBackupPath'));"
    $dataRoot = Invoke-Sql "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultDataPath'));"
    $logRoot = Invoke-Sql "SELECT CONVERT(nvarchar(4000),SERVERPROPERTY('InstanceDefaultLogPath'));"
    $backupFile = Join-Path $backupRoot "$snapshot.bak"
    $dataFile = Join-Path $dataRoot "$snapshot.mdf"
    $logFile = Join-Path $logRoot "$snapshot.ldf"
    $backupSql = $backupFile.Replace("'", "''")
    $dataSql = $dataFile.Replace("'", "''")
    $logSql = $logFile.Replace("'", "''")
    $null = Invoke-Sql @"
USE [$source];
EXECUTE AS USER='RefreshSource';
BEGIN TRY
    BACKUP DATABASE [$source] TO DISK=N'$backupSql' WITH COPY_ONLY, CHECKSUM;
END TRY
BEGIN CATCH
    REVERT;
    THROW;
END CATCH;
REVERT;
USE master;
RESTORE VERIFYONLY FROM DISK=N'$backupSql' WITH CHECKSUM;
"@
    $null = Invoke-Sql "UPDATE [$source].dbo.Operations SET Label='Source after backup';"
    $created.Add($snapshot)
    $null = Invoke-Sql @"
RESTORE DATABASE [$snapshot] FROM DISK=N'$backupSql'
WITH CHECKSUM, MOVE N'$source' TO N'$dataSql', MOVE N'${source}_log' TO N'$logSql';
ALTER DATABASE [$snapshot] SET READ_ONLY;
"@
    $snapshotLabel = Invoke-Sql @"
USE [$snapshot];
EXECUTE AS USER='RefreshSource';
BEGIN TRY
    SELECT Label FROM dbo.Operations WHERE Id=1;
END TRY
BEGIN CATCH
    REVERT;
    THROW;
END CATCH;
REVERT;
"@
    if ($snapshotLabel -cne 'Source') { throw 'Restricted snapshot read returned unexpected data.' }
    $run = @"
USE [$target];
IF DB_NAME() <> '$target' THROW 51001, 'Target guard failed', 1;
SET XACT_ABORT ON;
EXECUTE AS USER='RefreshTarget';
BEGIN TRY
    BEGIN TRANSACTION;
    DELETE FROM dbo.Operations;
    INSERT INTO dbo.Operations(Id,Label) VALUES(1,@label);
    IF NOT EXISTS(SELECT 1 FROM dbo.EnvironmentSettings WHERE Id=1 AND Mode='test')
        THROW 51002, 'Test settings changed', 1;
    {0}
    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    REVERT;
    THROW;
END CATCH;
REVERT;
"@
    try {
        $null = Invoke-Sql ($run -f "THROW 51003, 'Injected failure', 1;") -Label $snapshotLabel
        throw 'Expected rollback did not run.'
    }
    catch {
        if ($_.Exception.Message -notlike '*Injected failure*') { throw }
    }
    $preserved = Invoke-Sql "SELECT Label FROM [$target].dbo.Operations WHERE Id=1;"
    if ($preserved -cne 'Baseline') { throw 'Failed transfer changed the target.' }

    $null = Invoke-Sql ($run -f '') -Label $snapshotLabel
    $after = Invoke-Sql @"
SELECT CASE WHEN (SELECT Label FROM [$source].dbo.Operations WHERE Id=1)='Source after backup'
            AND (SELECT Label FROM [$snapshot].dbo.Operations WHERE Id=1)='Source'
            AND DATABASEPROPERTYEX('$snapshot','Updateability')='READ_ONLY'
            AND (SELECT Label FROM [$target].dbo.Operations WHERE Id=1)='Source'
            AND (SELECT Mode FROM [$target].dbo.EnvironmentSettings WHERE Id=1)='test'
       THEN 1 ELSE 0 END;
"@
    if ($after -ne 1) { throw 'Final source, target, or test-settings verification failed.' }
    Write-Output 'Isolated SQL rehearsal passed: verified copy-only snapshot, point-in-time restore, source grants, one-way transfer, retained test settings, and rollback.'
}
finally {
    try {
        foreach ($db in $created) {
            if ($db -notmatch '^SPC_Refresh_Rehearsal_(Source|Target|Snapshot)_[a-f0-9]{32}$') {
                throw 'Unsafe rehearsal cleanup target.'
            }
            $null = Invoke-Sql "USE master; DROP DATABASE IF EXISTS [$db];"
        }
        if ($backupFile -and (Test-Path -LiteralPath $backupFile)) {
            Remove-Item -LiteralPath $backupFile
        }
    }
    finally { $connection.Dispose() }
}
