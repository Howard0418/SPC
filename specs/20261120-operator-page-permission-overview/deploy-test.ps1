$ErrorActionPreference = 'Stop'
$root = 'D:\SPC'
$testRoot = 'D:\SPC\release\test'
$apiSource = 'D:\SPC\.release-builds\operator-page-permissions-20261120\backend'
$webSource = 'D:\SPC\frontend\mes-spc-web\dist'
$stamp = 'operator-page-permissions-20261120'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'

$apiTarget = Join-Path $testRoot 'backend'
$webTarget = Join-Path $testRoot 'frontend'
if ((& $appcmd list vdir 'SpcApiTest/' /text:physicalPath).Trim() -ne $apiTarget -or
    (& $appcmd list vdir 'SpcSingleTest/' /text:physicalPath).Trim() -ne $webTarget) {
    throw 'IIS target is not the reviewed SPC test directory.'
}
$settings = Get-Content (Join-Path $apiTarget 'appsettings.json') -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'SPC test API environment or database is incorrect.'
}
foreach ($part in @('backend','frontend')) {
    $source = Join-Path $testRoot $part
    $backup = Join-Path $testRoot ($part + '.backup-' + $stamp)
    if (Test-Path $backup) { throw "Backup already exists: $backup" }
    New-Item -ItemType Directory -Path $backup | Out-Null
    Get-ChildItem $source -Force | Copy-Item -Destination $backup -Recurse -Force
}

$offline = Join-Path $apiTarget 'app_offline.htm'
try {
    Set-Content $offline '<html><body>SPC test deployment in progress.</body></html>' -Encoding utf8
    Start-Sleep -Seconds 2
    Get-ChildItem $apiSource -Recurse -File | Where-Object { $_.Name -notlike 'appsettings*.json' -and $_.Name -ne 'web.config' } | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($apiSource, $_.FullName)
        $destination = [IO.Path]::GetFullPath((Join-Path $apiTarget $relative))
        if (!$destination.StartsWith($apiTarget + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe API payload path.' }
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Copy-Item $_.FullName $destination -Force
    }
    Get-ChildItem $webSource -Recurse -File | Where-Object { $_.Name -ne 'index.html' -and $_.Name -ne 'web.config' } | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($webSource, $_.FullName)
        $destination = [IO.Path]::GetFullPath((Join-Path $webTarget $relative))
        if (!$destination.StartsWith($webTarget + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe Web payload path.' }
        New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
        Copy-Item $_.FullName $destination -Force
    }
    Copy-Item (Join-Path $webSource 'index.html') (Join-Path $webTarget 'index.html') -Force
} finally {
    if (Test-Path $offline) { Remove-Item $offline -Force }
}

$ready = $false
for ($i=0; $i -lt 20; $i++) {
    try {
        $version = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 10
        if ($version.environment -eq 'test' -and $version.version -eq '0.1.156') { $ready = $true; break }
    } catch {}
    Start-Sleep -Seconds 1
}
if (!$ready) { throw 'SPC test API did not return API 0.1.156 / test.' }
$health = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/health' -TimeoutSec 10
$webResponse = Invoke-WebRequest 'https://spctest.pmr.com.tw/' -UseBasicParsing -TimeoutSec 10
$assetPaths = [regex]::Matches($webResponse.Content, '(?:src|href)="([^"]+\.(?:js|css))"') | ForEach-Object { $_.Groups[1].Value }
$assets = foreach ($path in $assetPaths) {
    $uri = [Uri]::new([Uri]'https://spctest.pmr.com.tw/', $path)
    $response = Invoke-WebRequest $uri.AbsoluteUri -UseBasicParsing -TimeoutSec 10
    [pscustomobject]@{ Uri=$uri.AbsoluteUri; Status=$response.StatusCode; ContentType=$response.Headers.'Content-Type' }
}
if ($webResponse.StatusCode -ne 200 -or $health.environment -ne 'test' -or $assets.Count -lt 2 -or
    $assets.Where({$_.Status -ne 200 -or ($_.Uri -like '*.js' -and $_.ContentType -notmatch 'javascript') -or ($_.Uri -like '*.css' -and $_.ContentType -notmatch 'css')}).Count) {
    throw 'SPC test Web/API/asset verification failed.'
}
[pscustomobject]@{WebVersion='0.1.165';ApiVersion=$version.version;Environment=$version.environment;Health=$health.status;Home=$webResponse.StatusCode;Assets=$assets} | ConvertTo-Json -Depth 5
