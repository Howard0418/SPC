[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$EnvironmentName,

    [Parameter(Mandatory = $true)]
    [string]$SitePath,

    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [string]$IisAppPoolName,

    [string[]]$PreserveRelativePath = @('appsettings.json', 'appsettings.Production.json', 'web.config'),

    [string[]]$SmokeUrl = @(),

    [switch]$CreateAppOffline,

    [switch]$ControlAppPool
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

function Save-Json {
    param(
        [object]$Value,
        [string]$Path
    )

    $Value | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Copy-PreservedFiles {
    param(
        [string]$SiteRoot,
        [string]$StageRoot,
        [string[]]$RelativePaths
    )

    $copied = @()
    foreach ($relative in $RelativePaths) {
        if ([string]::IsNullOrWhiteSpace($relative)) { continue }
        $source = Join-Path $SiteRoot $relative
        if (-not (Test-Path -LiteralPath $source)) { continue }
        $target = Join-Path $StageRoot $relative
        $targetParent = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $targetParent)) {
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        }
        Copy-Item -LiteralPath $source -Destination $target -Recurse -Force
        $copied += $relative
    }
    return $copied
}

function Invoke-SmokeTests {
    param([string[]]$Urls)

    $results = @()
    foreach ($url in $Urls) {
        try {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get -TimeoutSec 15
            $results += [pscustomobject]@{
                Url = $url
                StatusCode = [int]$response.StatusCode
                Ok = $response.StatusCode -ge 200 -and $response.StatusCode -lt 500
                Error = $null
            }
        }
        catch {
            $results += [pscustomobject]@{
                Url = $url
                StatusCode = $null
                Ok = $false
                Error = $_.Exception.Message
            }
        }
    }
    return $results
}

Assert-NonProductionTarget -Name $EnvironmentName -Path $SitePath

$resolvedSitePath = [System.IO.Path]::GetFullPath($SitePath)
$resolvedPackagePath = [System.IO.Path]::GetFullPath($PackagePath)

if (-not (Test-Path -LiteralPath $resolvedSitePath -PathType Container)) {
    throw "SitePath does not exist or is not a directory: $resolvedSitePath"
}
if (-not (Test-Path -LiteralPath $resolvedPackagePath -PathType Container)) {
    throw "PackagePath does not exist or is not a directory: $resolvedPackagePath"
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$stagePath = Join-Path ([System.IO.Path]::GetTempPath()) "spc-release-apply-$EnvironmentName-$stamp"
$evidencePath = Join-Path $resolvedSitePath "_release-evidence-$stamp.json"
$events = @()

if ($PSCmdlet.ShouldProcess($stagePath, 'Stage package and preserved files')) {
    New-Item -ItemType Directory -Path $stagePath -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $resolvedPackagePath '*') -Destination $stagePath -Recurse -Force
    $preserved = Copy-PreservedFiles -SiteRoot $resolvedSitePath -StageRoot $stagePath -RelativePaths $PreserveRelativePath
    $events += "Staged package and preserved files: $($preserved -join ', ')"

    if ($CreateAppOffline) {
        $appOfflinePath = Join-Path $resolvedSitePath 'app_offline.htm'
        Set-Content -LiteralPath $appOfflinePath -Encoding UTF8 -Value '<html><body><h1>Maintenance</h1></body></html>'
        $events += 'Created app_offline.htm. Removal is intentionally not automated in this prototype.'
    }

    if ($ControlAppPool) {
        if ([string]::IsNullOrWhiteSpace($IisAppPoolName)) {
            throw 'ControlAppPool requires -IisAppPoolName.'
        }
        if (Get-Module -ListAvailable -Name WebAdministration) {
            Import-Module WebAdministration
            Stop-WebAppPool -Name $IisAppPoolName
            $events += "Stopped app pool: $IisAppPoolName"
        }
        else {
            $events += 'WebAdministration module unavailable; app pool stop skipped.'
        }
    }

    Copy-Item -LiteralPath (Join-Path $stagePath '*') -Destination $resolvedSitePath -Recurse -Force
    $events += 'Copied staged package into site path without deleting extra target files.'

    if ($ControlAppPool -and (Get-Module -ListAvailable -Name WebAdministration)) {
        Start-WebAppPool -Name $IisAppPoolName
        $events += "Started app pool: $IisAppPoolName"
    }

    $smoke = Invoke-SmokeTests -Urls $SmokeUrl
    $evidence = [pscustomobject]@{
        tool = 'ReleaseApplyPackage'
        environmentName = $EnvironmentName
        sitePath = $resolvedSitePath
        packagePath = $resolvedPackagePath
        iisAppPoolName = $IisAppPoolName
        createdAt = (Get-Date).ToUniversalTime().ToString('o')
        events = $events
        smoke = $smoke
        note = 'This prototype does not delete files and does not remove app_offline.htm automatically.'
    }
    Save-Json -Value $evidence -Path $evidencePath
    Write-Output "Apply completed: $evidencePath"
}
