SET NOCOUNT ON;

DECLARE @BackupFile nvarchar(4000) =
    N'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_2026_before_full_reset_20260704.bak';

BACKUP DATABASE [PMR_SPC_2026]
TO DISK = @BackupFile
WITH COPY_ONLY, INIT, COMPRESSION, CHECKSUM, STATS = 10;

RESTORE VERIFYONLY
FROM DISK = @BackupFile
WITH CHECKSUM;

SELECT
    @BackupFile AS BackupFile,
    bs.backup_start_date AS BackupStart,
    bs.backup_finish_date AS BackupFinish,
    bs.has_backup_checksums AS HasChecksums,
    CAST(bs.backup_size / 1024.0 / 1024.0 AS decimal(18, 2)) AS BackupSizeMB,
    CAST(bs.compressed_backup_size / 1024.0 / 1024.0 AS decimal(18, 2)) AS CompressedSizeMB
FROM msdb.dbo.backupset bs
JOIN msdb.dbo.backupmediafamily bmf ON bmf.media_set_id = bs.media_set_id
WHERE bmf.physical_device_name = @BackupFile
ORDER BY bs.backup_finish_date DESC;
