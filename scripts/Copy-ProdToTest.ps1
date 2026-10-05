# ============================================================
# Copy-ProdToTest.ps1
# Copy PMR_SPC_2026 (Production) -> PMR_SPC_TEST (Test)
# Run as Administrator in PowerShell:
#   .\Copy-ProdToTest.ps1
# ============================================================

param(
    [string]$SqlServer      = "172.16.110.16",
    [string]$SqlUser        = "sa",
    [string]$SqlPassword    = "a@t123",
    [string]$SourceDb       = "PMR_SPC_2026",
    [string]$TargetDb       = "PMR_SPC_TEST",
    [string]$BackupPath     = "C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_2026.bak",
    [string]$DataFilePath   = "C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PMR_SPC_TEST.mdf",
    [string]$LogFilePath    = "C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\PMR_SPC_TEST_log.ldf",
    [string]$AppOfflineSrc  = "D:\SPC\tools\app_offline.htm",
    [string]$AppOfflineDest = "D:\Sites\SpcTest\app_offline.htm"
)

$ErrorActionPreference = "Stop"
$sqlcmd = "sqlcmd"

$tmpSql = [System.IO.Path]::GetTempFileName() + ".sql"

function Invoke-Sql([string]$query) {
    [System.IO.File]::WriteAllText($tmpSql, $query, [System.Text.Encoding]::UTF8)
    & $sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -i $tmpSql
    $script:lastRc = $LASTEXITCODE
}

function Invoke-SqlOut([string]$query) {
    [System.IO.File]::WriteAllText($tmpSql, $query, [System.Text.Encoding]::UTF8)
    return & $sqlcmd -S $SqlServer -U $SqlUser -P $SqlPassword -i $tmpSql -h-1 -W
}

Write-Host ""
Write-Host "[Step 1] Stop test site (app_offline.htm)" -ForegroundColor Cyan
if (Test-Path $AppOfflineSrc) {
    Copy-Item $AppOfflineSrc $AppOfflineDest -Force
    Write-Host "  -> app_offline.htm placed at $AppOfflineDest" -ForegroundColor Green
} else {
    Write-Host "  -> $AppOfflineSrc not found, skipping" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "[Step 2] Backup production database: $SourceDb" -ForegroundColor Cyan
Invoke-Sql "BACKUP DATABASE [$SourceDb] TO DISK = N'$BackupPath' WITH FORMAT, MEDIANAME = N'SpcBackup', NAME = N'$SourceDb Full Backup', STATS = 10;"
if ($script:lastRc -ne 0) { throw "Backup failed. Check path $BackupPath and permissions." }
Write-Host "  -> Backup complete: $BackupPath" -ForegroundColor Green

Write-Host ""
Write-Host "[Step 3] Read logical file names from backup" -ForegroundColor Cyan
$fileList = Invoke-SqlOut "RESTORE FILELISTONLY FROM DISK = N'$BackupPath';"
$rows = $fileList | Where-Object { $_ -match "^\S" -and $_ -notmatch "^-" -and $_ -notmatch "^LogicalName" } | Select-Object -First 2
$logicalData = ($rows[0] -split "\s+")[0].Trim()
$logicalLog  = ($rows[1] -split "\s+")[0].Trim()
Write-Host "  -> Data file: $logicalData" -ForegroundColor Green
Write-Host "  -> Log  file: $logicalLog" -ForegroundColor Green

Write-Host ""
Write-Host "[Step 4] Disconnect existing connections to $TargetDb" -ForegroundColor Cyan
Invoke-Sql "ALTER DATABASE [$TargetDb] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;" | Out-Null
Write-Host "  -> SINGLE_USER set" -ForegroundColor Green

Write-Host ""
Write-Host "[Step 5] Restore $SourceDb -> $TargetDb" -ForegroundColor Cyan
$restoreQuery = "RESTORE DATABASE [$TargetDb] FROM DISK = N'$BackupPath' WITH MOVE N'$logicalData' TO N'$DataFilePath', MOVE N'$logicalLog' TO N'$LogFilePath', REPLACE, RECOVERY, STATS = 10;"
Invoke-Sql $restoreQuery
if ($script:lastRc -ne 0) { throw "Restore failed. Check MOVE paths and disk space." }
Write-Host "  -> Restore complete. $TargetDb now has production data." -ForegroundColor Green

Write-Host ""
Write-Host "[Step 6] Set $TargetDb back to MULTI_USER" -ForegroundColor Cyan
Invoke-Sql "ALTER DATABASE [$TargetDb] SET MULTI_USER;" | Out-Null
Write-Host "  -> MULTI_USER restored" -ForegroundColor Green

Write-Host ""
Write-Host "[Step 7] Re-enable test site" -ForegroundColor Cyan
if (Test-Path $AppOfflineDest) {
    Remove-Item $AppOfflineDest -Force
    Write-Host "  -> app_offline.htm removed, test site is back online" -ForegroundColor Green
} else {
    Write-Host "  -> app_offline.htm not found, skipping" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Magenta
Write-Host " DONE: $SourceDb -> $TargetDb" -ForegroundColor Magenta
Write-Host " Please verify the test site in browser." -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
