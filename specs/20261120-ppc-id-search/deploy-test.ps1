$ErrorActionPreference = 'Stop'

$repo = 'D:\SPC'
$source = Join-Path $repo 'frontend\mes-spc-web\dist'
$target = Join-Path $repo 'release\test\frontend'
$backupRoot = Join-Path $repo 'release\backups'
$backup = Join-Path $backupRoot ("web-0.1.172-before-ppc-id-search-20261120-{0}" -f (Get-Date -Format 'HHmmss'))
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$pool = 'SpcSingleTest'

$expectedTarget = [IO.Path]::GetFullPath('D:\SPC\release\test\frontend').TrimEnd('\')
$resolvedTarget = [IO.Path]::GetFullPath($target).TrimEnd('\')
$resolvedSource = [IO.Path]::GetFullPath($source).TrimEnd('\')
$resolvedBackupRoot = [IO.Path]::GetFullPath($backupRoot).TrimEnd('\') + '\'
$resolvedBackup = [IO.Path]::GetFullPath($backup).TrimEnd('\')
if ($resolvedTarget -ne $expectedTarget -or
    $resolvedSource -ne [IO.Path]::GetFullPath('D:\SPC\frontend\mes-spc-web\dist').TrimEnd('\') -or
    -not ($resolvedBackup + '\').StartsWith($resolvedBackupRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Resolved deployment paths are outside the reviewed SPC locations.'
}

$actualTarget = (& $appcmd list vdir 'SpcSingleTest/' /text:physicalPath).Trim()
$actualPool = (& $appcmd list app 'SpcSingleTest/' /text:apppool.name).Trim()
if ($LASTEXITCODE -ne 0 -or $actualTarget -ne $target -or $actualPool -ne $pool) {
    throw "Unexpected SPC test IIS target: target=$actualTarget pool=$actualPool"
}
if (-not (Test-Path (Join-Path $source 'index.html')) -or -not (Test-Path (Join-Path $target 'web.config'))) {
    throw 'Build output or existing test web.config is missing.'
}
$settings = Get-Content (Join-Path $repo 'release\test\backend\appsettings.json') -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'SPC test API environment or database is incorrect.'
}
$mainAsset = Get-ChildItem (Join-Path $source 'assets') -Filter 'index-*.js' -File | Sort-Object Length -Descending | Select-Object -First 1
if (-not $mainAsset -or -not (Select-String -LiteralPath $mainAsset.FullName -Pattern '0.1.173' -SimpleMatch -Quiet)) {
    throw 'Built Web payload does not contain version 0.1.173.'
}

New-Item -ItemType Directory -Path $backup | Out-Null
Get-ChildItem $target -Force | Copy-Item -Destination $backup -Recurse -Force

& $appcmd stop apppool /apppool.name:$pool | Out-Null
try {
    # The exact target was resolved and validated above; clear only this test Web directory.
    Get-ChildItem -LiteralPath $resolvedTarget -Force | Remove-Item -Recurse -Force
    Copy-Item -Path (Join-Path $resolvedSource '*') -Destination $resolvedTarget -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $resolvedBackup 'web.config') -Destination (Join-Path $resolvedTarget 'web.config') -Force
} catch {
    $failure = $_
    Get-ChildItem -LiteralPath $resolvedTarget -Force | Remove-Item -Recurse -Force
    Copy-Item -Path (Join-Path $resolvedBackup '*') -Destination $resolvedTarget -Recurse -Force
    throw $failure
} finally {
    & $appcmd start apppool /apppool.name:$pool | Out-Null
}

foreach ($file in Get-ChildItem $source -Recurse -File) {
    $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
    $published = Join-Path $target $relative
    if (-not (Test-Path $published) -or (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $published).Hash) {
        throw "Published file mismatch: $relative"
    }
}

$siteBase = 'https://spctest.pmr.com.tw'
$landing = Invoke-WebRequest "$siteBase/" -UseBasicParsing -TimeoutSec 20
$assetPaths = [regex]::Matches($landing.Content, '(?:src|href)="([^"]+\.(?:js|css))"') | ForEach-Object { $_.Groups[1].Value }
$assets = foreach ($path in $assetPaths) {
    $uri = [Uri]::new([Uri]$siteBase, $path)
    $response = Invoke-WebRequest $uri.AbsoluteUri -UseBasicParsing -TimeoutSec 20
    [pscustomobject]@{ Uri=$uri.AbsoluteUri; Status=$response.StatusCode; ContentType=$response.Headers.'Content-Type' }
}
$health = Invoke-RestMethod "$siteBase/api/health" -TimeoutSec 20
$version = Invoke-RestMethod "$siteBase/api/version" -TimeoutSec 20
$directVersion = Invoke-RestMethod "$siteBase`:8901/api/version" -TimeoutSec 20
$deployedMain = $assets | Where-Object Uri -like '*.js' | Select-Object -First 1
$deployedContent = (Invoke-WebRequest $deployedMain.Uri -UseBasicParsing -TimeoutSec 20).Content
if ($landing.StatusCode -ne 200 -or $assets.Count -lt 2 -or
    $assets.Where({$_.Status -ne 200 -or ($_.Uri -like '*.js' -and $_.ContentType -notmatch 'javascript') -or ($_.Uri -like '*.css' -and $_.ContentType -notmatch 'css')}).Count -gt 0 -or
    $health.status -notin @('ok','Healthy') -or $version.environment -ne 'test' -or $directVersion.environment -ne 'test' -or
    $version.version -ne '0.1.161' -or $directVersion.version -ne '0.1.161' -or $deployedContent -notmatch '0\.1\.173') {
    throw 'SPC test Web/API/version/environment verification failed.'
}

[pscustomobject]@{
    Status='Deployed'; WebVersion='0.1.173'; ApiVersion=$version.version; Environment=$version.environment;
    Health=$health.status; HomeStatus=$landing.StatusCode; Assets=$assets; Backup=$backup
} | ConvertTo-Json -Depth 5
