$ErrorActionPreference = 'Stop'
$repo = 'D:\SPC'
$apiSource = Join-Path $repo '.release-builds\audit-business-action-20261120\backend'
$webSource = Join-Path $repo 'frontend\mes-spc-web\dist'
$apiTarget = Join-Path $repo 'release\test\backend'
$webTarget = Join-Path $repo 'release\test\frontend'
$backupRoot = Join-Path $repo ("release\backups\audit-business-action-20261120-{0}" -f (Get-Date -Format 'HHmmss'))
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$apiPool = 'SpcApiTest'
$webPool = 'SpcSingleTest'

$expectedApi = [IO.Path]::GetFullPath('D:\SPC\release\test\backend').TrimEnd('\')
$expectedWeb = [IO.Path]::GetFullPath('D:\SPC\release\test\frontend').TrimEnd('\')
$resolvedApi = [IO.Path]::GetFullPath($apiTarget).TrimEnd('\')
$resolvedWeb = [IO.Path]::GetFullPath($webTarget).TrimEnd('\')
$resolvedBackup = [IO.Path]::GetFullPath($backupRoot).TrimEnd('\')
$allowedBackupRoot = [IO.Path]::GetFullPath((Join-Path $repo 'release\backups')).TrimEnd('\') + '\'
if ($resolvedApi -ne $expectedApi -or $resolvedWeb -ne $expectedWeb -or
    -not ($resolvedBackup + '\').StartsWith($allowedBackupRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Resolved deployment paths are outside the reviewed SPC test locations.'
}
if ((& $appcmd list vdir 'SpcApiTest/' /text:physicalPath).Trim() -ne $resolvedApi -or
    (& $appcmd list vdir 'SpcSingleTest/' /text:physicalPath).Trim() -ne $resolvedWeb -or
    (& $appcmd list app 'SpcApiTest/' /text:apppool.name).Trim() -ne $apiPool -or
    (& $appcmd list app 'SpcSingleTest/' /text:apppool.name).Trim() -ne $webPool) {
    throw 'Unexpected SPC test IIS target or application pool.'
}
if (-not (Test-Path (Join-Path $apiSource 'MesSpc.Api.dll')) -or
    -not (Test-Path (Join-Path $webSource 'index.html')) -or
    -not (Test-Path (Join-Path $apiTarget 'appsettings.json')) -or
    -not (Test-Path (Join-Path $apiTarget 'web.config')) -or
    -not (Test-Path (Join-Path $webTarget 'web.config'))) {
    throw 'Publish payload or preserved test configuration is missing.'
}
$settings = Get-Content (Join-Path $apiTarget 'appsettings.json') -Raw | ConvertFrom-Json
$connection = [string]$settings.ConnectionStrings.SqlServer
if ($settings.AppEnvironment -ne 'test' -or $connection -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'SPC test API environment or database is incorrect.'
}
$parts = @{}
foreach ($part in $connection -split ';') {
    if ($part -match '^\s*([^=]+)=(.*)$') { $parts[$matches[1].Trim().ToLowerInvariant()] = $matches[2].Trim() }
}
$server = if ($parts['server']) { $parts['server'] } else { $parts['data source'] }
$user = if ($parts['user id']) { $parts['user id'] } else { $parts['uid'] }
$password = if ($parts['password']) { $parts['password'] } else { $parts['pwd'] }
$migration = (& sqlcmd -S $server -d PMR_SPC_TEST -U $user -P $password -C -b -h -1 -W -Q "SET NOCOUNT ON; SELECT CASE WHEN EXISTS(SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId='20261120134446_AllowBusinessActionClientEvent') THEN 1 ELSE 0 END;" | Where-Object { $_ -match '^[01]$' } | Select-Object -Last 1).Trim()
if ($migration -ne '1') { throw 'Required audit business action migration is not applied.' }
$mainAsset = Get-ChildItem (Join-Path $webSource 'assets') -Filter 'index-*.js' -File | Sort-Object Length -Descending | Select-Object -First 1
if (-not $mainAsset -or -not (Select-String -LiteralPath $mainAsset.FullName -Pattern '0.1.174' -SimpleMatch -Quiet)) {
    throw 'Built Web payload does not contain version 0.1.174.'
}

New-Item -ItemType Directory -Path (Join-Path $backupRoot 'backend') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $backupRoot 'frontend') -Force | Out-Null
Get-ChildItem -LiteralPath $resolvedApi -Force | Copy-Item -Destination (Join-Path $backupRoot 'backend') -Recurse -Force
Get-ChildItem -LiteralPath $resolvedWeb -Force | Copy-Item -Destination (Join-Path $backupRoot 'frontend') -Recurse -Force

& $appcmd stop apppool /apppool.name:$apiPool | Out-Null
& $appcmd stop apppool /apppool.name:$webPool | Out-Null
try {
    Get-ChildItem -LiteralPath $resolvedApi -Force | Remove-Item -Recurse -Force
    Get-ChildItem -LiteralPath $apiSource -Force | Copy-Item -Destination $resolvedApi -Recurse -Force
    Copy-Item (Join-Path $backupRoot 'backend\appsettings.json') (Join-Path $resolvedApi 'appsettings.json') -Force
    Copy-Item (Join-Path $backupRoot 'backend\web.config') (Join-Path $resolvedApi 'web.config') -Force

    Get-ChildItem -LiteralPath $resolvedWeb -Force | Remove-Item -Recurse -Force
    Get-ChildItem -LiteralPath $webSource -Force | Copy-Item -Destination $resolvedWeb -Recurse -Force
    Copy-Item (Join-Path $backupRoot 'frontend\web.config') (Join-Path $resolvedWeb 'web.config') -Force
} catch {
    $failure = $_
    Get-ChildItem -LiteralPath $resolvedApi -Force | Remove-Item -Recurse -Force
    Get-ChildItem -LiteralPath (Join-Path $backupRoot 'backend') -Force | Copy-Item -Destination $resolvedApi -Recurse -Force
    Get-ChildItem -LiteralPath $resolvedWeb -Force | Remove-Item -Recurse -Force
    Get-ChildItem -LiteralPath (Join-Path $backupRoot 'frontend') -Force | Copy-Item -Destination $resolvedWeb -Recurse -Force
    throw $failure
} finally {
    & $appcmd start apppool /apppool.name:$apiPool | Out-Null
    & $appcmd start apppool /apppool.name:$webPool | Out-Null
}

$siteBase = 'https://spctest.pmr.com.tw'
$ready = $false
for ($i = 0; $i -lt 20; $i++) {
    try {
        $version = Invoke-RestMethod "$siteBase/api/version" -TimeoutSec 10
        if ($version.environment -eq 'test' -and $version.version -eq '0.1.163') { $ready = $true; break }
    } catch {}
    Start-Sleep -Seconds 1
}
if (-not $ready) { throw 'SPC test API did not return API 0.1.163 / test.' }
$health = Invoke-RestMethod "$siteBase/api/health" -TimeoutSec 20
$directHealth = Invoke-RestMethod "$siteBase`:8901/api/health" -TimeoutSec 20
$directVersion = Invoke-RestMethod "$siteBase`:8901/api/version" -TimeoutSec 20
$landing = Invoke-WebRequest "$siteBase/" -UseBasicParsing -TimeoutSec 20
$assetPaths = [regex]::Matches($landing.Content, '(?:src|href)="([^"]+\.(?:js|css))"') | ForEach-Object { $_.Groups[1].Value }
$assets = foreach ($path in $assetPaths) {
    $uri = [Uri]::new([Uri]$siteBase, $path)
    $response = Invoke-WebRequest $uri.AbsoluteUri -UseBasicParsing -TimeoutSec 20
    [pscustomobject]@{ Uri=$uri.AbsoluteUri; Status=$response.StatusCode; ContentType=$response.Headers.'Content-Type' }
}
$deployedMain = $assets | Where-Object Uri -like '*.js' | Select-Object -First 1
$deployedContent = (Invoke-WebRequest $deployedMain.Uri -UseBasicParsing -TimeoutSec 20).Content
if ($landing.StatusCode -ne 200 -or $assets.Count -lt 2 -or
    $assets.Where({$_.Status -ne 200 -or ($_.Uri -like '*.js' -and $_.ContentType -notmatch 'javascript') -or ($_.Uri -like '*.css' -and $_.ContentType -notmatch 'css')}).Count -gt 0 -or
    $health.status -notin @('ok','Healthy') -or $directHealth.status -notin @('ok','Healthy') -or
    $version.environment -ne 'test' -or $directVersion.environment -ne 'test' -or
    $version.version -ne '0.1.163' -or $directVersion.version -ne '0.1.163' -or
    $deployedContent -notmatch '0\.1\.174') {
    throw 'SPC test Web/API/version/environment verification failed.'
}
[pscustomobject]@{
    Status='Deployed'; WebVersion='0.1.174'; ApiVersion=$version.version; Environment=$version.environment;
    Health=$health.status; DirectHealth=$directHealth.status; HomeStatus=$landing.StatusCode; Assets=$assets; Backup=$backupRoot
} | ConvertTo-Json -Depth 5
