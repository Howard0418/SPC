[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$connection = [System.Data.SqlClient.SqlConnection]::new(
    'Server=localhost;Database=master;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=5')
$suffix = [guid]::NewGuid().ToString('N')
$source = "SPC_Refresh_Rehearsal_Source_$suffix"
$target = "SPC_Refresh_Rehearsal_Target_$suffix"
$created = [System.Collections.Generic.List[string]]::new()

function Invoke-Sql([string]$Text) {
    $command = $connection.CreateCommand()
    try {
        $command.CommandTimeout = 20
        $command.CommandText = $Text
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
CREATE ROLE RefreshRehearsalRole;
CREATE USER RefreshRehearsalUser WITHOUT LOGIN;
ALTER ROLE RefreshRehearsalRole ADD MEMBER RefreshRehearsalUser;
GRANT SELECT ON dbo.Operations TO RefreshRehearsalRole;
INSERT INTO dbo.Operations VALUES(1, 'Baseline');
INSERT INTO dbo.EnvironmentSettings VALUES(1, 'test');
"@
    }

    $null = Invoke-Sql @"
USE [$source];
UPDATE dbo.Operations SET Label='Source';
EXECUTE AS USER='RefreshRehearsalUser';
IF HAS_PERMS_BY_NAME('dbo.Operations','OBJECT','UPDATE')<>0
BEGIN
    REVERT;
    THROW 51000, 'Source principal has write permission', 1;
END;
REVERT;
USE [$target];
GRANT INSERT, DELETE ON dbo.Operations TO RefreshRehearsalRole;
"@
    $run = @"
USE [$target];
IF DB_NAME() <> '$target' THROW 51001, 'Target guard failed', 1;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    DELETE FROM dbo.Operations;
    INSERT INTO dbo.Operations(Id,Label) SELECT Id,Label FROM [$source].dbo.Operations;
    IF NOT EXISTS(SELECT 1 FROM dbo.EnvironmentSettings WHERE Id=1 AND Mode='test')
        THROW 51002, 'Test settings changed', 1;
    {0}
    COMMIT;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    THROW;
END CATCH;
"@
    try {
        $null = Invoke-Sql ($run -f "THROW 51003, 'Injected failure', 1;")
        throw 'Expected rollback did not run.'
    }
    catch {
        if ($_.Exception.Message -notlike '*Injected failure*') { throw }
    }
    $preserved = Invoke-Sql "SELECT Label FROM [$target].dbo.Operations WHERE Id=1;"
    if ($preserved -cne 'Baseline') { throw 'Failed transfer changed the target.' }

    $null = Invoke-Sql ($run -f '')
    $after = Invoke-Sql @"
SELECT CASE WHEN (SELECT Label FROM [$source].dbo.Operations WHERE Id=1)='Source'
            AND (SELECT Label FROM [$target].dbo.Operations WHERE Id=1)='Source'
            AND (SELECT Mode FROM [$target].dbo.EnvironmentSettings WHERE Id=1)='test'
       THEN 1 ELSE 0 END;
"@
    if ($after -ne 1) { throw 'Final source, target, or test-settings verification failed.' }
    Write-Output 'Isolated SQL rehearsal passed: source grants, one-way transfer, retained test settings, and rollback.'
}
finally {
    try {
        foreach ($db in $created) {
            if ($db -notmatch '^SPC_Refresh_Rehearsal_(Source|Target)_[a-f0-9]{32}$') {
                throw 'Unsafe rehearsal cleanup target.'
            }
            $null = Invoke-Sql "USE master; DROP DATABASE [$db];"
        }
    }
    finally { $connection.Dispose() }
}
