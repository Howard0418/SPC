-- Execute in master on the reviewed SQL Server instance.
-- Only a qualified DBA should enable verification. No restore or cleanup is performed.
SET NOCOUNT ON;
DECLARE @VerifyBackups bit = 0;
DECLARE @ExpectedInstance sysname = NULL;
DECLARE @Instance sysname = CONVERT(sysname, SERVERPROPERTY('ServerName'));
IF DB_NAME() <> N'master'
    THROW 51000, N'Run this check in master.', 1;
IF @VerifyBackups = 1 AND (@ExpectedInstance IS NULL OR @Instance <> @ExpectedInstance)
    THROW 51001, N'Confirm the exact SQL Server instance before verifying backups.', 1;

DECLARE @Candidate table (Name sysname PRIMARY KEY);
INSERT @Candidate (Name) VALUES
 (N'SPC_FORMULA_SCHEMA_VERIFY_20261020_192deaf0'),
 (N'SPC_FORMULA_SCHEMA_VERIFY_20261020_31e46ee2'),
 (N'SPC_FORMULA_SCHEMA_VERIFY_20261020_729dd93a'),
 (N'SPC_FORMULA_SCHEMA_VERIFY_20261020_741a26bb'),
 (N'SPC_FORMULA_SCHEMA_VERIFY_20261020_7b1399d8'),
 (N'SPC_FORMULA_SCHEMA_VERIFY_20261020_9023536f'),
 (N'SPC_FORMULA_SCHEMA_VERIFY_20261020_a25c8e6f');

SELECT 'INSTANCE' AS Result, @Instance AS SqlInstance, SYSDATETIMEOFFSET() AS CheckedAt;

DECLARE @Backup table (
    Name sysname PRIMARY KEY, LastSource sysname NULL, RestoredAt datetime NULL,
    BackupAt datetime NULL, BackupPosition int NULL, DiskPath nvarchar(260) NULL,
    HasChecksum bit NULL, Damaged bit NULL, MediaExists bit NULL
);
INSERT @Backup
SELECT c.Name, r.source_db, r.restore_date, b.backup_finish_date, b.position, media.physical_device_name,
    b.has_backup_checksums, b.is_damaged, f.file_exists
FROM @Candidate c
OUTER APPLY (
    SELECT TOP (1) b.database_name AS source_db, r.restore_date
    FROM msdb.dbo.restorehistory r JOIN msdb.dbo.backupset b ON b.backup_set_id = r.backup_set_id
    WHERE r.destination_database_name COLLATE DATABASE_DEFAULT = c.Name
    ORDER BY r.restore_history_id DESC
) r
OUTER APPLY (
    SELECT TOP (1) backup_finish_date, position, media_set_id, has_backup_checksums, is_damaged
    FROM msdb.dbo.backupset
    WHERE database_name COLLATE DATABASE_DEFAULT = c.Name AND type = 'D' AND backup_finish_date >= r.restore_date
    ORDER BY backup_finish_date DESC, backup_set_id DESC
) b
OUTER APPLY (
    SELECT m.physical_device_name
    FROM msdb.dbo.backupmediaset s JOIN msdb.dbo.backupmediafamily m ON m.media_set_id = s.media_set_id
    WHERE s.media_set_id = b.media_set_id AND s.media_family_count = 1 AND m.family_sequence_number = 1
      AND m.mirror = 0 AND m.device_type = 2
) media
OUTER APPLY sys.dm_os_file_exists(media.physical_device_name) f;

SELECT 'CANDIDATE' AS Result, c.Name, COALESCE(d.state_desc,'MISSING') AS DatabaseState,
    b.LastSource, b.RestoredAt, b.BackupAt, b.HasChecksum, b.Damaged, b.MediaExists,
    CASE WHEN d.source_database_id IS NOT NULL OR d.is_published = 1 OR d.is_subscribed = 1
           OR d.is_merge_published = 1 OR d.is_distributor = 1
           OR EXISTS (SELECT 1 FROM sys.databases s WHERE s.source_database_id = d.database_id)
         THEN 'GAP' ELSE 'NONE_FOUND' END AS SnapshotOrReplication
FROM @Candidate c LEFT JOIN sys.databases d ON d.name = c.Name JOIN @Backup b ON b.Name = c.Name
ORDER BY c.Name;

IF IS_SRVROLEMEMBER('sysadmin') = 1 OR HAS_PERMS_BY_NAME(NULL, NULL, 'VIEW SERVER PERFORMANCE STATE') = 1
BEGIN
    SELECT 'REFERENCE' AS Result, 'SESSION' AS Kind, c.Name, COUNT_BIG(*) AS Matches
    FROM sys.dm_exec_sessions s JOIN sys.databases d ON d.database_id = s.database_id
    JOIN @Candidate c ON c.Name = d.name
    WHERE s.is_user_process = 1 AND s.session_id <> @@SPID GROUP BY c.Name;
    SELECT 'REFERENCE' AS Result, 'REQUEST' AS Kind, c.Name, COUNT_BIG(*) AS Matches
    FROM sys.dm_exec_requests r JOIN sys.databases d ON d.database_id = r.database_id
    JOIN @Candidate c ON c.Name = d.name WHERE r.session_id <> @@SPID GROUP BY c.Name;
    SELECT 'REFERENCE' AS Result, 'LOCK' AS Kind, c.Name, COUNT_BIG(*) AS Matches
    FROM sys.dm_tran_locks l JOIN sys.databases d ON d.database_id = l.resource_database_id
    JOIN @Candidate c ON c.Name = d.name WHERE l.request_session_id <> @@SPID GROUP BY c.Name;
    SELECT 'CHECK' AS Result, 'SESSIONS_REQUESTS_LOCKS_INSPECTED' AS Detail;
END
ELSE SELECT 'GAP' AS Result, 'NO_SERVER_PERFORMANCE_STATE' AS Detail;

BEGIN TRY
    DECLARE @CanReadAllJobs bit = 0;
    EXEC sys.sp_executesql N'USE msdb;
        SELECT @CanRead = CASE WHEN IS_ROLEMEMBER(''SQLAgentReaderRole'') = 1
                                 OR IS_ROLEMEMBER(''SQLAgentOperatorRole'') = 1 THEN 1 ELSE 0 END;',
        N'@CanRead bit OUTPUT', @CanRead = @CanReadAllJobs OUTPUT;
    IF COALESCE(IS_SRVROLEMEMBER('sysadmin'),0) <> 1 AND @CanReadAllJobs = 0
       AND COALESCE(HAS_PERMS_BY_NAME(NULL, NULL, 'CONTROL SERVER'),0) <> 1
    BEGIN
        SELECT 'GAP' AS Result, 'SQL_AGENT_REQUIRES_FULL_JOB_VISIBILITY' AS Detail;
    END
    ELSE
    BEGIN
        SELECT 'REFERENCE' AS Result, 'SQL_AGENT' AS Kind, c.Name, j.name AS JobName, j.enabled AS JobEnabled, COUNT_BIG(*) AS Matches
        FROM @Candidate c JOIN msdb.dbo.sysjobsteps s ON s.database_name COLLATE DATABASE_DEFAULT = c.Name
             OR CHARINDEX(c.Name, s.command COLLATE DATABASE_DEFAULT) > 0
        JOIN msdb.dbo.sysjobs j ON j.job_id = s.job_id
        GROUP BY c.Name, j.name, j.enabled;
        SELECT 'CHECK' AS Result, 'SQL_AGENT_INSPECTED' AS Detail;
    END
END TRY
BEGIN CATCH
    SELECT 'GAP' AS Result, 'SQL_AGENT_UNAVAILABLE' AS Detail, ERROR_NUMBER() AS ErrorNumber;
END CATCH;

DECLARE @Names nvarchar(max) = (SELECT STRING_AGG(CONVERT(nvarchar(max), N'(N' + QUOTENAME(Name,'''') + N')'), N',') FROM @Candidate);
DECLARE @Database sysname, @Inspect nvarchar(max);
DECLARE database_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.databases WHERE state_desc = 'ONLINE' AND database_id NOT IN (2,3) ORDER BY name;
OPEN database_cursor;
FETCH NEXT FROM database_cursor INTO @Database;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF HAS_DBACCESS(@Database) = 1
    BEGIN TRY
        SET @Inspect = N'USE ' + QUOTENAME(@Database) + N';
IF HAS_PERMS_BY_NAME(DB_NAME(), ''DATABASE'', ''VIEW DEFINITION'') = 1
BEGIN
    SELECT ''REFERENCE'' AS Result, ''DATABASE_OBJECT'' AS Kind, DB_NAME() AS InDatabase, c.Name, COUNT_BIG(*) AS Matches
    FROM sys.sql_expression_dependencies d JOIN (VALUES ' + @Names + N') c(Name)
      ON d.referenced_database_name COLLATE DATABASE_DEFAULT = c.Name
    GROUP BY c.Name;
    SELECT ''REFERENCE'' AS Result, ''SYNONYM'' AS Kind, DB_NAME() AS InDatabase, c.Name, COUNT_BIG(*) AS Matches
    FROM sys.synonyms s JOIN (VALUES ' + @Names + N') c(Name)
      ON PARSENAME(s.base_object_name,3) COLLATE DATABASE_DEFAULT = c.Name
    GROUP BY c.Name;
END
ELSE SELECT ''GAP'' AS Result, ''NO_DATABASE_VIEW_DEFINITION'' AS Detail, DB_NAME() AS InDatabase;';
        EXEC sys.sp_executesql @Inspect;
    END TRY
    BEGIN CATCH
        SELECT 'GAP' AS Result, 'DATABASE_DEPENDENCY_CHECK_UNAVAILABLE' AS Detail, @Database AS InDatabase, ERROR_NUMBER() AS ErrorNumber;
    END CATCH
    ELSE SELECT 'GAP' AS Result, 'NO_DATABASE_ACCESS' AS Detail, @Database AS InDatabase;
    FETCH NEXT FROM database_cursor INTO @Database;
END;
CLOSE database_cursor;
DEALLOCATE database_cursor;

IF @VerifyBackups = 0
    SELECT 'GAP' AS Result, 'BACKUP_VERIFY_NOT_RUN' AS Detail;
ELSE IF IS_SRVROLEMEMBER('sysadmin') <> 1 AND COALESCE(HAS_PERMS_BY_NAME(NULL, NULL, 'CREATE ANY DATABASE'), 0) <> 1
     AND COALESCE(HAS_PERMS_BY_NAME('master', 'DATABASE', 'CREATE DATABASE'), 0) <> 1
    SELECT 'GAP' AS Result, 'BACKUP_VERIFY_PERMISSION_MISSING' AS Detail;
ELSE
BEGIN
    DECLARE @Path nvarchar(260), @Position int, @Sql nvarchar(max);
    SELECT 'GAP' AS Result, 'BACKUP_INELIGIBLE' AS Detail, Name FROM @Backup
    WHERE LastSource IS NULL OR LastSource <> N'PMR_SPC_TEST' OR DiskPath IS NULL
       OR COALESCE(MediaExists,0) <> 1 OR COALESCE(HasChecksum,0) <> 1 OR COALESCE(Damaged,1) <> 0 OR COALESCE(BackupPosition,0) <= 0;
    DECLARE backup_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT b.Name, b.DiskPath, b.BackupPosition FROM @Backup b
        JOIN sys.databases d ON d.name = b.Name AND d.state_desc = 'ONLINE'
        WHERE b.LastSource = N'PMR_SPC_TEST' AND b.MediaExists = 1 AND b.HasChecksum = 1 AND b.Damaged = 0 AND b.BackupPosition > 0;
    OPEN backup_cursor;
    FETCH NEXT FROM backup_cursor INTO @Database, @Path, @Position;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            SET @Sql = N'RESTORE VERIFYONLY FROM DISK = @Disk WITH CHECKSUM, FILE = ' + CONVERT(nvarchar(12), @Position) + N';';
            EXEC sys.sp_executesql @Sql, N'@Disk nvarchar(260)', @Disk = @Path;
            SELECT 'BACKUP_VERIFY' AS Result, @Database AS Name, 'PASS' AS Status;
        END TRY
        BEGIN CATCH
            SELECT 'BACKUP_VERIFY' AS Result, @Database AS Name, 'FAIL' AS Status, ERROR_NUMBER() AS ErrorNumber;
        END CATCH;
        FETCH NEXT FROM backup_cursor INTO @Database, @Path, @Position;
    END;
    CLOSE backup_cursor;
    DEALLOCATE backup_cursor;
END;
