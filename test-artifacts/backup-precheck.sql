SET NOCOUNT ON;

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS nvarchar(4000)) AS DefaultBackupPath,
    CAST(DATABASEPROPERTYEX(DB_NAME(), 'Status') AS nvarchar(60)) AS DatabaseStatus,
    CAST(DATABASEPROPERTYEX(DB_NAME(), 'Recovery') AS nvarchar(60)) AS RecoveryModel;

SELECT
    CAST(SUM(size) * 8.0 / 1024 AS decimal(18, 2)) AS DatabaseAllocatedMB
FROM sys.database_files;

SELECT TOP (5)
    bs.database_name AS DatabaseName,
    bs.backup_start_date AS BackupStart,
    bs.backup_finish_date AS BackupFinish,
    CAST(bs.backup_size / 1024.0 / 1024.0 AS decimal(18, 2)) AS BackupSizeMB,
    CAST(bs.compressed_backup_size / 1024.0 / 1024.0 AS decimal(18, 2)) AS CompressedSizeMB,
    bmf.physical_device_name AS BackupFile
FROM msdb.dbo.backupset bs
JOIN msdb.dbo.backupmediafamily bmf ON bmf.media_set_id = bs.media_set_id
WHERE bs.database_name = DB_NAME() AND bs.type = 'D'
ORDER BY bs.backup_finish_date DESC;
