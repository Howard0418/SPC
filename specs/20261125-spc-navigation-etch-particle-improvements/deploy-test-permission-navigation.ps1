$ErrorActionPreference = 'Stop'
$root = 'D:\SPC'
$testRoot = 'D:\SPC\release\test'
$apiTarget = Join-Path $testRoot 'backend'
$webTarget = Join-Path $testRoot 'frontend'
$webSource = Join-Path $root 'frontend\mes-spc-web\dist'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$stage = Join-Path $root ".release-staging\spc-particle-paste-entry-1125-$stamp\backend"
$backupRoot = Join-Path $root "release\backups\spc-particle-paste-entry-1125-$stamp"
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$apiAppPool = 'SpcApiTest'

if ([IO.Path]::GetFullPath($apiTarget) -ne 'D:\SPC\release\test\backend' -or
    [IO.Path]::GetFullPath($webTarget) -ne 'D:\SPC\release\test\frontend') {
    throw 'Unexpected SPC test target.'
}
if ((& $appcmd list vdir 'SpcApiTest/' /text:physicalPath).Trim() -ne $apiTarget -or
    (& $appcmd list vdir 'SpcSingleTest/' /text:physicalPath).Trim() -ne $webTarget) {
    throw 'IIS target is not the reviewed SPC test directory.'
}
$settings = Get-Content (Join-Path $apiTarget 'appsettings.json') -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'SPC test API environment or database is incorrect.'
}
$beforeVersion = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 20
$beforeDirect = Invoke-RestMethod 'https://spctest.pmr.com.tw:8901/api/version' -TimeoutSec 20
if ($beforeVersion.version -ne '0.1.177' -or $beforeDirect.version -ne '0.1.177' -or
    $beforeVersion.environment -ne 'test' -or $beforeDirect.environment -ne 'test') {
    throw 'SPC test API changed after preflight; deployment stopped.'
}

dotnet publish (Join-Path $root 'backend\MesSpc.Api\MesSpc.Api.csproj') -c Release -o $stage --no-restore
if ($LASTEXITCODE -ne 0) { throw 'API publish failed.' }
$stageVersion = (Get-Item (Join-Path $stage 'MesSpc.Api.dll')).VersionInfo.ProductVersion
if ($stageVersion -notlike '0.1.178*') { throw "Unexpected staged API version: $stageVersion" }
if (!(Test-Path (Join-Path $webSource 'index.html'))) { throw 'Web test build is missing.' }
$stageIndex = Get-Content (Join-Path $webSource 'index.html') -Raw
$stageScriptPath = [regex]::Match($stageIndex, 'src="([^"]+\.js)"').Groups[1].Value.TrimStart('/')
$stageScript = Get-Content (Join-Path $webSource $stageScriptPath) -Raw
if (!$stageScript.Contains('0.1.194')) { throw 'Unexpected staged Web version.' }

New-Item -ItemType Directory -Path $backupRoot | Out-Null
Copy-Item -LiteralPath $apiTarget -Destination (Join-Path $backupRoot 'backend') -Recurse
Copy-Item -LiteralPath $webTarget -Destination (Join-Path $backupRoot 'frontend') -Recurse
$settingsHash = (Get-FileHash (Join-Path $apiTarget 'appsettings.json') -Algorithm SHA256).Hash
$apiConfigHash = (Get-FileHash (Join-Path $apiTarget 'web.config') -Algorithm SHA256).Hash
$webConfigHash = (Get-FileHash (Join-Path $webTarget 'web.config') -Algorithm SHA256).Hash
$offline = Join-Path $apiTarget 'app_offline.htm'

try {
    [IO.File]::WriteAllText($offline, '<html><body>Updating SPC test API</body></html>')
    & $appcmd stop apppool "/apppool.name:$apiAppPool" | Out-Null
    Start-Sleep -Seconds 3
    Get-ChildItem -LiteralPath $stage -File -Recurse |
        Where-Object { $_.Name -notlike 'appsettings*.json' -and $_.Name -ne 'web.config' } |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($stage, $_.FullName)
            $destination = [IO.Path]::GetFullPath((Join-Path $apiTarget $relative))
            if (!$destination.StartsWith($apiTarget + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe API payload path.' }
            New-Item -Path (Split-Path $destination) -ItemType Directory -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }
    Get-ChildItem -LiteralPath $webSource -File -Recurse |
        Where-Object { $_.Name -ne 'index.html' -and $_.Name -ne 'web.config' } |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($webSource, $_.FullName)
            $destination = [IO.Path]::GetFullPath((Join-Path $webTarget $relative))
            if (!$destination.StartsWith($webTarget + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe Web payload path.' }
            New-Item -Path (Split-Path $destination) -ItemType Directory -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }
    Copy-Item -LiteralPath (Join-Path $webSource 'index.html') -Destination (Join-Path $webTarget 'index.html') -Force
}
catch {
    Copy-Item -Path (Join-Path $backupRoot 'backend\*') -Destination $apiTarget -Recurse -Force
    Copy-Item -Path (Join-Path $backupRoot 'frontend\*') -Destination $webTarget -Recurse -Force
    throw
}
finally {
    Remove-Item -LiteralPath $offline -Force -ErrorAction SilentlyContinue
    & $appcmd start apppool "/apppool.name:$apiAppPool" | Out-Null
}

if ((Get-FileHash (Join-Path $apiTarget 'appsettings.json') -Algorithm SHA256).Hash -ne $settingsHash -or
    (Get-FileHash (Join-Path $apiTarget 'web.config') -Algorithm SHA256).Hash -ne $apiConfigHash -or
    (Get-FileHash (Join-Path $webTarget 'web.config') -Algorithm SHA256).Hash -ne $webConfigHash) {
    throw 'Protected test configuration changed.'
}

$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $version = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 10
        if ($version.version -eq '0.1.178' -and $version.environment -eq 'test') { $ready = $true; break }
    } catch {}
    Start-Sleep -Seconds 1
}
if (!$ready) { throw 'SPC test API did not become ready as 0.1.178/test.' }

$directVersion = Invoke-RestMethod 'https://spctest.pmr.com.tw:8901/api/version' -TimeoutSec 20
$health = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/health' -TimeoutSec 20
$directHealth = Invoke-RestMethod 'https://spctest.pmr.com.tw:8901/api/health' -TimeoutSec 20
$webResponse = Invoke-WebRequest 'https://spctest.pmr.com.tw/' -UseBasicParsing -TimeoutSec 20
$assetPaths = [regex]::Matches($webResponse.Content, '(?:src|href)="([^"]+\.(?:js|css))"') | ForEach-Object { $_.Groups[1].Value }
$assets = foreach ($assetPath in $assetPaths) {
    $uri = [Uri]::new([Uri]'https://spctest.pmr.com.tw/', $assetPath)
    $response = Invoke-WebRequest $uri.AbsoluteUri -UseBasicParsing -TimeoutSec 20
    [pscustomobject]@{ Uri = $uri.AbsoluteUri; Status = $response.StatusCode; ContentType = $response.Headers.'Content-Type' }
}
$mainScript = $assets | Where-Object Uri -Like '*.js' | Select-Object -First 1
$mainContent = (Invoke-WebRequest $mainScript.Uri -UseBasicParsing -TimeoutSec 20).Content
if ($webResponse.StatusCode -ne 200 -or $health.status -notin @('ok', 'Healthy') -or $directHealth.status -notin @('ok', 'Healthy') -or
    $version.version -ne '0.1.178' -or $version.environment -ne 'test' -or
    $directVersion.version -ne '0.1.178' -or $directVersion.environment -ne 'test' -or
    !$mainContent.Contains('0.1.194') -or $assets.Count -lt 2 -or
    $assets.Where({ $_.Status -ne 200 -or ($_.Uri -like '*.js' -and $_.ContentType -notmatch 'javascript') -or ($_.Uri -like '*.css' -and $_.ContentType -notmatch 'css') }).Count) {
    throw 'SPC test Web/API/version/environment verification failed.'
}

[pscustomobject]@{
    BeforeWeb = '0.1.193'; AfterWeb = '0.1.194'; BeforeApi = $beforeVersion.version; AfterApi = $version.version;
    DirectApi = $directVersion.version; Environment = $version.environment; Health = $health.status;
    DirectHealth = $directHealth.status; Home = $webResponse.StatusCode; Assets = $assets; Backup = $backupRoot
} | ConvertTo-Json -Depth 5
