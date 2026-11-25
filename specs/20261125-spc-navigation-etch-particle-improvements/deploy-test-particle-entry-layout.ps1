$ErrorActionPreference = 'Stop'

$root = 'D:\SPC'
$webTarget = 'D:\SPC\release\test\frontend'
$webSource = 'D:\SPC\frontend\mes-spc-web\dist'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupRoot = Join-Path $root "release\backups\spc-particle-entry-layout-1125-$stamp"
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$webBase = [Uri]'https://spctest.pmr.com.tw/'
$directApiBase = [Uri]'https://spctest.pmr.com.tw:8901/'

if ([IO.Path]::GetFullPath($webTarget) -ne 'D:\SPC\release\test\frontend') {
    throw 'Unexpected SPC test Web target.'
}
if ((& $appcmd list vdir 'SpcSingleTest/' /text:physicalPath).Trim() -ne $webTarget) {
    throw 'IIS target is not the reviewed SPC test Web directory.'
}

$beforeVersion = Invoke-RestMethod ([Uri]::new($webBase, 'api/version')) -TimeoutSec 20
$beforeDirect = Invoke-RestMethod ([Uri]::new($directApiBase, 'api/version')) -TimeoutSec 20
if ($beforeVersion.version -ne '0.1.178' -or $beforeDirect.version -ne '0.1.178' -or
    $beforeVersion.environment -ne 'test' -or $beforeDirect.environment -ne 'test') {
    throw 'SPC test API version or environment changed after review; deployment stopped.'
}

$beforeLanding = Invoke-WebRequest $webBase.AbsoluteUri -UseBasicParsing -TimeoutSec 20
$beforeScriptPath = [regex]::Match($beforeLanding.Content, 'src="([^"]+\.js)"').Groups[1].Value
$beforeScriptUri = [Uri]::new($webBase, $beforeScriptPath)
$beforeScript = (Invoke-WebRequest $beforeScriptUri.AbsoluteUri -UseBasicParsing -TimeoutSec 20).Content
if (!$beforeScript.Contains('0.1.194')) { throw 'Unexpected current SPC test Web version.' }

$stageIndexPath = Join-Path $webSource 'index.html'
$stageFloorPlanPath = Join-Path $webSource 'assets\particle-floor-plan.png'
if (!(Test-Path $stageIndexPath) -or !(Test-Path $stageFloorPlanPath)) {
    throw 'SPC test Web build or floor-plan asset is missing.'
}
$stageIndex = Get-Content $stageIndexPath -Raw
$stageScriptPath = [regex]::Match($stageIndex, 'src="([^"]+\.js)"').Groups[1].Value.TrimStart('/')
$stageScript = Get-Content (Join-Path $webSource $stageScriptPath) -Raw
if (!$stageScript.Contains('0.1.195')) { throw 'Unexpected staged SPC Web version.' }

New-Item -ItemType Directory -Path $backupRoot | Out-Null
Copy-Item -LiteralPath $webTarget -Destination (Join-Path $backupRoot 'frontend') -Recurse
$webConfigHash = (Get-FileHash (Join-Path $webTarget 'web.config') -Algorithm SHA256).Hash

try {
    Get-ChildItem -LiteralPath $webSource -File -Recurse |
        Where-Object { $_.Name -ne 'index.html' -and $_.Name -ne 'web.config' } |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($webSource, $_.FullName)
            $destination = [IO.Path]::GetFullPath((Join-Path $webTarget $relative))
            if (!$destination.StartsWith($webTarget + '\', [StringComparison]::OrdinalIgnoreCase)) {
                throw 'Unsafe SPC Web payload path.'
            }
            New-Item -Path (Split-Path $destination) -ItemType Directory -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }
    Copy-Item -LiteralPath $stageIndexPath -Destination (Join-Path $webTarget 'index.html') -Force
}
catch {
    Copy-Item -Path (Join-Path $backupRoot 'frontend\*') -Destination $webTarget -Recurse -Force
    throw
}

if ((Get-FileHash (Join-Path $webTarget 'web.config') -Algorithm SHA256).Hash -ne $webConfigHash) {
    throw 'Protected SPC test Web configuration changed.'
}

$afterLanding = Invoke-WebRequest $webBase.AbsoluteUri -UseBasicParsing -TimeoutSec 20
$assetPaths = [regex]::Matches($afterLanding.Content, '(?:src|href)="([^"]+\.(?:js|css))"') |
    ForEach-Object { $_.Groups[1].Value }
$assets = foreach ($assetPath in $assetPaths) {
    $assetUri = [Uri]::new($webBase, $assetPath)
    $response = Invoke-WebRequest $assetUri.AbsoluteUri -UseBasicParsing -TimeoutSec 20
    [pscustomobject]@{ Uri = $assetUri.AbsoluteUri; Status = $response.StatusCode; ContentType = $response.Headers.'Content-Type' }
}
$mainScript = $assets | Where-Object Uri -Like '*.js' | Select-Object -First 1
$mainContent = (Invoke-WebRequest $mainScript.Uri -UseBasicParsing -TimeoutSec 20).Content
$floorPlan = Invoke-WebRequest ([Uri]::new($webBase, 'assets/particle-floor-plan.png')).AbsoluteUri -UseBasicParsing -TimeoutSec 20
$afterVersion = Invoke-RestMethod ([Uri]::new($webBase, 'api/version')) -TimeoutSec 20
$afterDirect = Invoke-RestMethod ([Uri]::new($directApiBase, 'api/version')) -TimeoutSec 20
$health = Invoke-RestMethod ([Uri]::new($webBase, 'api/health')) -TimeoutSec 20
$directHealth = Invoke-RestMethod ([Uri]::new($directApiBase, 'api/health')) -TimeoutSec 20

if ($afterLanding.StatusCode -ne 200 -or !$mainContent.Contains('0.1.195') -or
    $floorPlan.StatusCode -ne 200 -or $floorPlan.Headers.'Content-Type' -notmatch '^image/png' -or
    $afterVersion.version -ne '0.1.178' -or $afterVersion.environment -ne 'test' -or
    $afterDirect.version -ne '0.1.178' -or $afterDirect.environment -ne 'test' -or
    $health.status -notin @('ok', 'Healthy') -or $directHealth.status -notin @('ok', 'Healthy') -or
    $assets.Count -lt 2 -or
    $assets.Where({ $_.Status -ne 200 -or ($_.Uri -like '*.js' -and $_.ContentType -notmatch 'javascript') -or ($_.Uri -like '*.css' -and $_.ContentType -notmatch 'css') }).Count) {
    throw 'SPC test Web/API/version/environment verification failed.'
}

[pscustomobject]@{
    BeforeWeb = '0.1.194'
    AfterWeb = '0.1.195'
    BeforeApi = $beforeVersion.version
    AfterApi = $afterVersion.version
    Environment = $afterVersion.environment
    Health = $health.status
    DirectHealth = $directHealth.status
    Home = $afterLanding.StatusCode
    FloorPlan = [pscustomobject]@{ Status = $floorPlan.StatusCode; ContentType = $floorPlan.Headers.'Content-Type'; Bytes = $floorPlan.RawContentLength }
    Assets = $assets
    Backup = $backupRoot
} | ConvertTo-Json -Depth 5
