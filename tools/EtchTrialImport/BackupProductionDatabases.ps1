param(
    [switch]$Execute,
    [string]$ConfirmPortal,
    [string]$ConfirmSpc,
    [switch]$ReuseRecentPortalBackup,
    [switch]$ReuseRecentSpcBackup,
    [string]$OutputDirectory = 'D:\SPC\release-staging\etch-production-20260922'
)

$ErrorActionPreference = 'Stop'
$expectedServer = '172.16.110.16'
$expectedPortal = 'PMR_PORTAL_UAT'
$expectedSpc = 'PMR_SPC_2026'

function Read-ConnectionString([string]$path, [string]$name) {
    $config = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    $value = $config.ConnectionStrings.$name
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Missing connection string '$name' in $path"
    }
    return [string]$value
}

function Normalize-Server([string]$value) {
    $server = $value.Trim()
    if ($server.StartsWith('tcp:', [StringComparison]::OrdinalIgnoreCase)) {
        $server = $server.Substring(4)
    }
    $comma = $server.IndexOf(',')
    if ($comma -ge 0) { $server = $server.Substring(0, $comma) }
    return $server
}

function Guard-Target([string]$connectionString, [string]$database) {
    $builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
    if ((Normalize-Server $builder.DataSource) -ne $expectedServer -or
        $builder.InitialCatalog -ne $database) {
        throw "Unexpected database target '$($builder.DataSource)/$($builder.InitialCatalog)'"
    }
    return $builder
}

function Query-Scalar(
    [System.Data.SqlClient.SqlConnection]$connection,
    [string]$sql,
    [hashtable]$parameters = @{}
) {
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 120
    foreach ($entry in $parameters.GetEnumerator()) {
        [void]$command.Parameters.AddWithValue($entry.Key, $entry.Value)
    }
    return $command.ExecuteScalar()
}

function Backup-And-Verify(
    [string]$connectionString,
    [string]$database,
    [string]$stamp,
    [bool]$reuseRecent
) {
    $connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
    $connection.Open()
    try {
        $state = [string](Query-Scalar $connection `
            'SELECT state_desc FROM sys.databases WHERE name=@database' `
            @{ '@database' = $database })
        if ($state -ne 'ONLINE') { throw "$database is not ONLINE: $state" }

        $backupDirectory = [string](Query-Scalar $connection `
            "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))")
        if ([string]::IsNullOrWhiteSpace($backupDirectory)) {
            throw "SQL Server did not return a backup path for $database"
        }

        if ($reuseRecent) {
            $recent = $connection.CreateCommand()
            $recent.CommandTimeout = 120
            $recent.CommandText = @"
SELECT TOP (1) f.physical_device_name
FROM msdb.dbo.backupset b
JOIN msdb.dbo.backupmediafamily f ON f.media_set_id=b.media_set_id
WHERE b.database_name=@database
  AND b.is_copy_only=1
  AND b.has_backup_checksums=1
  AND b.backup_finish_date >= DATEADD(hour,-24,GETDATE())
  AND f.physical_device_name LIKE '%pre_etch_import%'
ORDER BY b.backup_finish_date DESC
"@
            [void]$recent.Parameters.AddWithValue('@database', $database)
            $backupPath = [string]$recent.ExecuteScalar()
            if ([string]::IsNullOrWhiteSpace($backupPath)) {
                throw "No reusable recent COPY_ONLY/CHECKSUM backup found for $database"
            }
            Write-Host "Reusing recent backup: $backupPath"
        }
        else {
            $backupPath = $backupDirectory.TrimEnd('\') + '\' +
                "${database}_pre_etch_import_${stamp}.bak"
            $safeDatabase = '[' + $database.Replace(']', ']]') + ']'

            $backup = $connection.CreateCommand()
            $backup.CommandTimeout = 3600
            $backup.CommandText = @"
BACKUP DATABASE $safeDatabase
TO DISK=@path
WITH COPY_ONLY, CHECKSUM, COMPRESSION, STATS=10
"@
            [void]$backup.Parameters.AddWithValue('@path', $backupPath)
            [void]$backup.ExecuteNonQuery()
        }

        $verify = $connection.CreateCommand()
        $verify.CommandTimeout = 3600
        $verify.CommandText = 'RESTORE VERIFYONLY FROM DISK=@path WITH CHECKSUM'
        [void]$verify.Parameters.AddWithValue('@path', $backupPath)
        [void]$verify.ExecuteNonQuery()

        $metadata = $connection.CreateCommand()
        $metadata.CommandTimeout = 120
        $metadata.CommandText = @"
SELECT TOP (1)
       b.backup_set_id,
       b.backup_start_date,
       b.backup_finish_date,
       b.backup_size,
       b.compressed_backup_size,
       b.is_copy_only,
       b.has_backup_checksums
FROM msdb.dbo.backupset b
JOIN msdb.dbo.backupmediafamily f ON f.media_set_id=b.media_set_id
WHERE b.database_name=@database
  AND f.physical_device_name=@path
ORDER BY b.backup_finish_date DESC
"@
        [void]$metadata.Parameters.AddWithValue('@database', $database)
        [void]$metadata.Parameters.AddWithValue('@path', $backupPath)
        $reader = $metadata.ExecuteReader()
        try {
            if (-not $reader.Read()) { throw "Backup metadata not found for $database" }
            if (-not $reader.GetBoolean(5) -or -not $reader.GetBoolean(6)) {
                throw "Backup metadata does not confirm COPY_ONLY and CHECKSUM for $database"
            }
            return [ordered]@{
                Database = $database
                BackupPath = $backupPath
                CopyOnly = $true
                Checksum = $true
                VerifyOnly = $true
                BackupSetId = $reader.GetInt32(0)
                BackupStartAt = $reader.GetDateTime(1).ToString('o')
                BackupFinishedAt = $reader.GetDateTime(2).ToString('o')
                BackupSize = [Convert]::ToInt64($reader.GetValue(3))
                CompressedBackupSize = [Convert]::ToInt64($reader.GetValue(4))
            }
        }
        finally { $reader.Dispose() }
    }
    finally { $connection.Dispose() }
}

$spcConnection = Read-ConnectionString `
    'D:\SPC\release\production\backend\appsettings.json' 'SqlServer'
$portalConnection = Read-ConnectionString `
    'D:\PmrPortal\release\production\portal-api\appsettings.json' 'Production'
[void](Guard-Target $spcConnection $expectedSpc)
[void](Guard-Target $portalConnection $expectedPortal)

if (-not $Execute) {
    Write-Output "PREVIEW only: server=$expectedServer Portal=$expectedPortal SPC=$expectedSpc"
    Write-Output 'No backup was created. Add -Execute with both exact confirmations.'
    return
}

if ($ConfirmPortal -cne $expectedPortal -or $ConfirmSpc -cne $expectedSpc) {
    throw 'Backup execution requires exact -ConfirmPortal and -ConfirmSpc values.'
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$stamp = [DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss')

Write-Output "Backing up $expectedPortal with COPY_ONLY and CHECKSUM..."
$portalProof = Backup-And-Verify $portalConnection $expectedPortal $stamp $ReuseRecentPortalBackup.IsPresent
Write-Output "Backing up $expectedSpc with COPY_ONLY and CHECKSUM..."
$spcProof = Backup-And-Verify $spcConnection $expectedSpc $stamp $ReuseRecentSpcBackup.IsPresent

$proof = [ordered]@{
    Version = 1
    Server = $expectedServer
    CreatedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
    Portal = $portalProof
    Spc = $spcProof
}
$proofPath = Join-Path $OutputDirectory "backup-proof-$stamp.json"
[IO.File]::WriteAllText(
    $proofPath,
    ($proof | ConvertTo-Json -Depth 6),
    [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText(
    (Join-Path $OutputDirectory 'backup-proof-latest.txt'),
    $proofPath,
    [Text.UTF8Encoding]::new($false))

Write-Output "BACKUP_PROOF=$proofPath"
Write-Output ($proof | ConvertTo-Json -Depth 6)
