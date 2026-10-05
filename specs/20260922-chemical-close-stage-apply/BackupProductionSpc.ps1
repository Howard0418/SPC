param(
    [switch]$Execute,
    [string]$ConfirmSpc,
    [string]$OutputDirectory = 'D:\SPC\release-staging\chemical-close-stage-apply-20260922'
)

$ErrorActionPreference = 'Stop'
$expectedServer = '172.16.110.16'
$expectedSpc = 'PMR_SPC_2026'

function Read-ConnectionString([string]$path) {
    $config = Get-Content -LiteralPath $path -Raw -Encoding UTF8 | ConvertFrom-Json
    return [string]$config.ConnectionStrings.SqlServer
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

$connectionString = Read-ConnectionString 'D:\SPC\release\production\backend\appsettings.json'
$builder = [System.Data.SqlClient.SqlConnectionStringBuilder]::new($connectionString)
if ((Normalize-Server $builder.DataSource) -ne $expectedServer -or $builder.InitialCatalog -ne $expectedSpc) {
    throw "Unexpected database target '$($builder.DataSource)/$($builder.InitialCatalog)'"
}

if (-not $Execute) {
    Write-Output "PREVIEW only: server=$expectedServer SPC=$expectedSpc"
    Write-Output 'No backup was created. Add -Execute -ConfirmSpc PMR_SPC_2026'
    return
}

if ($ConfirmSpc -cne $expectedSpc) {
    throw 'Backup execution requires exact -ConfirmSpc PMR_SPC_2026'
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$stamp = [DateTime]::UtcNow.ToString('yyyyMMdd_HHmmss')
$connection = [System.Data.SqlClient.SqlConnection]::new($connectionString)
$connection.Open()
try {
    $stateCmd = $connection.CreateCommand()
    $stateCmd.CommandText = 'SELECT state_desc FROM sys.databases WHERE name=@database'
    [void]$stateCmd.Parameters.AddWithValue('@database', $expectedSpc)
    $state = [string]$stateCmd.ExecuteScalar()
    if ($state -ne 'ONLINE') { throw "$expectedSpc is not ONLINE: $state" }

    $pathCmd = $connection.CreateCommand()
    $pathCmd.CommandText = "SELECT CONVERT(nvarchar(4000), SERVERPROPERTY('InstanceDefaultBackupPath'))"
    $backupDirectory = [string]$pathCmd.ExecuteScalar()
    if ([string]::IsNullOrWhiteSpace($backupDirectory)) {
        throw "SQL Server did not return a backup path for $expectedSpc"
    }

    $backupPath = $backupDirectory.TrimEnd('\') + '\' +
        "${expectedSpc}_pre_chemical_close_stage_${stamp}.bak"

    $backup = $connection.CreateCommand()
    $backup.CommandTimeout = 3600
    $backup.CommandText = @"
BACKUP DATABASE [$expectedSpc]
TO DISK=@path
WITH COPY_ONLY, CHECKSUM, COMPRESSION, STATS=10
"@
    [void]$backup.Parameters.AddWithValue('@path', $backupPath)
    [void]$backup.ExecuteNonQuery()

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
    [void]$metadata.Parameters.AddWithValue('@database', $expectedSpc)
    [void]$metadata.Parameters.AddWithValue('@path', $backupPath)
    $reader = $metadata.ExecuteReader()
    try {
        if (-not $reader.Read()) { throw "Backup metadata not found for $expectedSpc" }
        if (-not $reader.GetBoolean(5) -or -not $reader.GetBoolean(6)) {
            throw "Backup metadata does not confirm COPY_ONLY and CHECKSUM for $expectedSpc"
        }
        $proof = [ordered]@{
            Version = 1
            Server = $expectedServer
            CreatedAtUtc = [DateTimeOffset]::UtcNow.ToString('o')
            Spc = [ordered]@{
                Database = $expectedSpc
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
    }
    finally { $reader.Dispose() }
}
finally { $connection.Dispose() }

$proofPath = Join-Path $OutputDirectory "backup-proof-$stamp.json"
[IO.File]::WriteAllText($proofPath, ($proof | ConvertTo-Json -Depth 6), [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText(
    (Join-Path $OutputDirectory 'backup-proof-latest.txt'),
    $proofPath,
    [Text.UTF8Encoding]::new($false))
Copy-Item -LiteralPath $proofPath -Destination (Join-Path $PSScriptRoot 'backup-proof.json') -Force

Write-Output "BACKUP_PROOF=$proofPath"
Write-Output ($proof | ConvertTo-Json -Depth 6)
