[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Backup', 'Restore')]
    [string]$Mode,

    [Parameter(Mandatory = $true)]
    [string]$EnvironmentName,

    [Parameter(Mandatory = $true)]
    [string]$SitePath,

    [Parameter(Mandatory = $true)]
    [string]$BackupRoot,

    [string]$BackupId,

    [string]$IisSiteName,

    [switch]$IncludeIisConfig,

    [switch]$AllowNonProductionRestore
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-NonProductionTarget {
    param(
        [string]$Name,
        [string]$Path
    )

    if ($Name -match '^(prod|production|formal|正式)$') {
        throw 'This prototype refuses Production targets. Formal release requires a separate explicit authorization.'
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $blocked = [System.IO.Path]::GetFullPath('D:\Sites\PmrPortal')
    if ($fullPath.StartsWith($blocked, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw 'Blocked target path: D:\Sites\PmrPortal must not be used by this SPC release tool.'
    }
}

function Get-DirectoryHashes {
    param([string]$Root)

    Get-ChildItem -LiteralPath $Root -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
            [pscustomobject]@{
                RelativePath = [System.IO.Path]::GetRelativePath($Root, $_.FullName)
                Length = $_.Length
                Sha256 = $hash.Hash
            }
        }
}

function Save-Json {
    param(
        [object]$Value,
        [string]$Path
    )

    $Value | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $Path -Encoding UTF8
}

Assert-NonProductionTarget -Name $EnvironmentName -Path $SitePath

$resolvedSitePath = [System.IO.Path]::GetFullPath($SitePath)
$resolvedBackupRoot = [System.IO.Path]::GetFullPath($BackupRoot)

if ($Mode -eq 'Backup') {
    if (-not (Test-Path -LiteralPath $resolvedSitePath -PathType Container)) {
        throw "SitePath does not exist or is not a directory: $resolvedSitePath"
    }

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $id = if ([string]::IsNullOrWhiteSpace($BackupId)) { "$EnvironmentName-$stamp" } else { $BackupId }
    $backupPath = Join-Path $resolvedBackupRoot $id
    $siteBackupPath = Join-Path $backupPath 'site'
    $evidencePath = Join-Path $backupPath 'evidence'

    if (Test-Path -LiteralPath $backupPath) {
        throw "BackupId already exists: $backupPath"
    }

    if ($PSCmdlet.ShouldProcess($backupPath, 'Create backup directory and copy site files')) {
        New-Item -ItemType Directory -Path $siteBackupPath -Force | Out-Null
        New-Item -ItemType Directory -Path $evidencePath -Force | Out-Null
        Copy-Item -Path (Join-Path $resolvedSitePath '*') -Destination $siteBackupPath -Recurse -Force

        $hashes = Get-DirectoryHashes -Root $siteBackupPath
        Save-Json -Value $hashes -Path (Join-Path $evidencePath 'site-hashes.json')

        $iisExport = $null
        if ($IncludeIisConfig -and -not [string]::IsNullOrWhiteSpace($IisSiteName)) {
            $iisExport = Join-Path $evidencePath 'iis-site.json'
            if (Get-Module -ListAvailable -Name WebAdministration) {
                Import-Module WebAdministration
                $site = Get-Item "IIS:\Sites\$IisSiteName" -ErrorAction Stop
                Save-Json -Value $site.Attributes -Path $iisExport
            }
            else {
                Save-Json -Value @{ warning = 'WebAdministration module is not available on this machine.' } -Path $iisExport
            }
        }

        $manifest = [pscustomobject]@{
            tool = 'ReleaseBackupRestore'
            mode = 'Backup'
            environmentName = $EnvironmentName
            backupId = $id
            sitePath = $resolvedSitePath
            backupPath = $backupPath
            iisSiteName = $IisSiteName
            iisExport = $iisExport
            createdAt = (Get-Date).ToUniversalTime().ToString('o')
            fileCount = @($hashes).Count
        }
        Save-Json -Value $manifest -Path (Join-Path $backupPath 'backup-manifest.json')
        Write-Output "Backup completed: $backupPath"
    }
}
else {
    if (-not $AllowNonProductionRestore) {
        throw 'Restore requires -AllowNonProductionRestore to make the intent explicit.'
    }
    if ([string]::IsNullOrWhiteSpace($BackupId)) {
        throw 'Restore requires -BackupId.'
    }

    $backupPath = Join-Path $resolvedBackupRoot $BackupId
    $siteBackupPath = Join-Path $backupPath 'site'
    if (-not (Test-Path -LiteralPath $siteBackupPath -PathType Container)) {
        throw "Backup site folder not found: $siteBackupPath"
    }
    if (-not (Test-Path -LiteralPath $resolvedSitePath -PathType Container)) {
        throw "Restore target does not exist or is not a directory: $resolvedSitePath"
    }

    if ($PSCmdlet.ShouldProcess($resolvedSitePath, "Restore files from $siteBackupPath without deleting extra target files")) {
        Copy-Item -Path (Join-Path $siteBackupPath '*') -Destination $resolvedSitePath -Recurse -Force
        $hashes = Get-DirectoryHashes -Root $resolvedSitePath
        $restoreEvidence = [pscustomobject]@{
            tool = 'ReleaseBackupRestore'
            mode = 'Restore'
            environmentName = $EnvironmentName
            backupId = $BackupId
            sitePath = $resolvedSitePath
            backupPath = $backupPath
            restoredAt = (Get-Date).ToUniversalTime().ToString('o')
            fileCount = @($hashes).Count
            note = 'Restore copies backup files over the target and does not delete extra target files.'
        }
        $evidencePath = Join-Path $backupPath 'restore-evidence.json'
        Save-Json -Value $restoreEvidence -Path $evidencePath
        Write-Output "Restore completed: $resolvedSitePath"
    }
}
