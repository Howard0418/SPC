$ErrorActionPreference = 'Stop'
$root = 'D:\SPC'
$testRoot = Join-Path $root 'release\test'
$webSource = Join-Path $root 'frontend\mes-spc-web\dist'
$webTarget = Join-Path $testRoot 'frontend'
$backup = Join-Path $testRoot 'frontend.backup-operators-auth-loading-20261120'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'

if ((& $appcmd list vdir 'SpcSingleTest/' /text:physicalPath).Trim() -ne $webTarget) {
    throw 'IIS Web target is not the reviewed SPC test directory.'
}
$settings = Get-Content (Join-Path $testRoot 'backend\appsettings.json') -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'SPC test API environment or database is incorrect.'
}
if (!(Test-Path $backup)) {
    New-Item -ItemType Directory -Path $backup | Out-Null
    Get-ChildItem $webTarget -Force | Copy-Item -Destination $backup -Recurse -Force

    Get-ChildItem $webSource -Recurse -File | Where-Object { $_.Name -ne 'index.html' -and $_.Name -ne 'web.config' } | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($webSource, $_.FullName)
        $destination = [IO.Path]::GetFullPath((Join-Path $webTarget $relative))
        if (!$destination.StartsWith($webTarget + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe Web payload path.' }
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Copy-Item $_.FullName $destination -Force
    }
    Copy-Item (Join-Path $webSource 'index.html') (Join-Path $webTarget 'index.html') -Force
}

$version = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 10
$directVersion = Invoke-RestMethod 'https://spctest.pmr.com.tw:8901/api/version' -TimeoutSec 10
$health = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/health' -TimeoutSec 10
$homeResponse = Invoke-WebRequest 'https://spctest.pmr.com.tw/' -UseBasicParsing -TimeoutSec 10
$assetPaths = [regex]::Matches($homeResponse.Content, '(?:src|href)="([^"]+\.(?:js|css))"') | ForEach-Object { $_.Groups[1].Value }
$assets = foreach ($path in $assetPaths) {
    $uri = [Uri]::new([Uri]'https://spctest.pmr.com.tw/', $path)
    $response = Invoke-WebRequest $uri.AbsoluteUri -UseBasicParsing -TimeoutSec 10
    [pscustomobject]@{ Uri=$uri.AbsoluteUri; Status=$response.StatusCode; ContentType=$response.Headers.'Content-Type' }
}
$mainScript = $assets | Where-Object Uri -like '*.js' | Select-Object -First 1
$mainContent = (Invoke-WebRequest $mainScript.Uri -UseBasicParsing -TimeoutSec 10).Content
if ($homeResponse.StatusCode -ne 200 -or $health.status -notin @('ok','Healthy') -or $version.environment -ne 'test' -or
    $directVersion.environment -ne 'test' -or $version.version -ne '0.1.156' -or $directVersion.version -ne '0.1.156' -or
    $assets.Count -lt 2 -or $assets.Where({$_.Status -ne 200 -or ($_.Uri -like '*.js' -and $_.ContentType -notmatch 'javascript') -or ($_.Uri -like '*.css' -and $_.ContentType -notmatch 'css')}).Count -or
    $mainContent -notmatch '0\.1\.166') {
    throw 'SPC test Web/API/version/environment verification failed.'
}
[pscustomobject]@{
    WebVersion='0.1.166'; ApiVersion=$version.version; DirectApiVersion=$directVersion.version;
    Environment=$version.environment; Health=$health.status; Home=$homeResponse.StatusCode; Assets=$assets; Backup=$backup
} | ConvertTo-Json -Depth 5
