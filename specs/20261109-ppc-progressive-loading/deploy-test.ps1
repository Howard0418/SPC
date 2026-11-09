$ErrorActionPreference = 'Stop'

$repo = 'D:\SPC'
$target = 'D:\SPC\release\test\frontend'
$source = 'D:\SPC\frontend\mes-spc-web\dist'
$backup = 'D:\SPC\release\test\frontend.backup-ppc-progressive-20261109'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$pool = 'SpcSingleTest'

$expectedRoot = [IO.Path]::GetFullPath('D:\SPC\release\test').TrimEnd('\') + '\'
$resolvedTarget = [IO.Path]::GetFullPath($target).TrimEnd('\') + '\'
$resolvedBackup = [IO.Path]::GetFullPath($backup).TrimEnd('\') + '\'
if (-not $resolvedTarget.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase) -or
    -not $resolvedBackup.StartsWith($expectedRoot, [StringComparison]::OrdinalIgnoreCase)) {
  throw 'Refusing to deploy outside the SPC test release root.'
}

$actualTarget = (& $appcmd list vdir 'SpcSingleTest/' /text:physicalPath).Trim()
$actualPool = (& $appcmd list app 'SpcSingleTest/' /text:apppool.name).Trim()
if ($LASTEXITCODE -ne 0 -or $actualTarget -ne $target -or $actualPool -ne $pool) {
  throw "Unexpected IIS test target: target=$actualTarget pool=$actualPool"
}
if (-not (Test-Path $source) -or -not (Test-Path (Join-Path $source 'index.html'))) {
  throw 'Test frontend build output is missing.'
}
$alreadyDeployed = Test-Path $backup

$webConfig = Join-Path $target 'web.config'
if (-not (Test-Path $webConfig)) { throw 'Existing test web.config is missing.' }

if (-not $alreadyDeployed) {
  & $appcmd stop apppool /apppool.name:$pool | Out-Null
  try {
    Move-Item -LiteralPath $target -Destination $backup
    New-Item -ItemType Directory -Path $target | Out-Null
    Copy-Item -Path (Join-Path $source '*') -Destination $target -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $backup 'web.config') -Destination $webConfig -Force
  } catch {
    $failure = $_
    if (Test-Path $target) { Remove-Item -LiteralPath $target -Recurse -Force }
    if (Test-Path $backup) { Move-Item -LiteralPath $backup -Destination $target }
    throw $failure
  } finally {
    & $appcmd start apppool /apppool.name:$pool | Out-Null
  }
}

$sourceFiles = Get-ChildItem $source -Recurse -File
foreach ($file in $sourceFiles) {
  $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
  $destination = Join-Path $target $relative
  if (-not (Test-Path $destination) -or (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $destination).Hash) {
    throw "Published file mismatch: $relative"
    }
}

$webResponse = Invoke-WebRequest 'https://spctest.pmr.com.tw/' -TimeoutSec 15
$index = Get-Content (Join-Path $target 'index.html') -Raw
$assetMatches = [regex]::Matches($index, '(?:src|href)="([^"]+\.(?:js|css))"')
if ($webResponse.StatusCode -ne 200 -or $assetMatches.Count -lt 2) { throw 'Test web home or asset discovery failed.' }
$assets = foreach ($match in $assetMatches) {
  $uri = [Uri]::new([Uri]'https://spctest.pmr.com.tw/', $match.Groups[1].Value)
  $response = Invoke-WebRequest $uri.AbsoluteUri -TimeoutSec 15
  [pscustomobject]@{ Url = $uri.AbsoluteUri; Status = $response.StatusCode; ContentType = $response.Headers.'Content-Type' }
}
if ($assets.Where({ $_.Status -ne 200 -or ($_.Url -like '*.js' -and $_.ContentType -notmatch 'javascript') -or ($_.Url -like '*.css' -and $_.ContentType -notmatch 'css') }).Count -gt 0) {
  throw 'Test web asset status or MIME validation failed.'
}
$health = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/health' -TimeoutSec 15
$version = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 15
if ($health.status -notin @('ok', 'Healthy') -or $version.environment -ne 'test') { throw 'Test API health or environment validation failed.' }

[pscustomobject]@{
  Status = 'Deployed'
  WebVersion = '0.1.124'
  ApiVersion = $version.version
  Environment = $version.environment
  Health = $health.status
  HomeStatus = $webResponse.StatusCode
  Assets = $assets
  Backup = $backup
} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $PSScriptRoot 'deployment-result.json') -Encoding utf8
