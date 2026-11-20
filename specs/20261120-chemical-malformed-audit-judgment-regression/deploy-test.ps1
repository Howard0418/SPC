$ErrorActionPreference = 'Stop'
$root = 'D:\SPC'
$target = 'D:\SPC\release\test\backend'
$stage = 'D:\SPC\release-staging\malformed-audit-judgment-20261120\backend'
$expectedTarget = 'D:\SPC\release\test\backend'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'

if ([IO.Path]::GetFullPath($target) -ne $expectedTarget) { throw 'Unexpected test target.' }
if ((& $appcmd list vdir 'SpcApiTest/' /text:physicalPath).Trim() -ne $expectedTarget) {
    throw 'IIS API target is not the reviewed SPC test directory.'
}
$settings = Get-Content (Join-Path $target 'appsettings.json') -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'SPC test API environment or database is incorrect.'
}
$settingsHash = (Get-FileHash (Join-Path $target 'appsettings.json') -Algorithm SHA256).Hash
$webConfigHash = (Get-FileHash (Join-Path $target 'web.config') -Algorithm SHA256).Hash

dotnet publish (Join-Path $root 'backend\MesSpc.Api\MesSpc.Api.csproj') -c Release -o $stage --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
$stageVersion = (Get-Item (Join-Path $stage 'MesSpc.Api.dll')).VersionInfo.ProductVersion
if ($stageVersion -notlike '0.1.162*') { throw "Unexpected staged API version: $stageVersion" }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = "D:\SPC\release\test\backend.backup-malformed-audit-judgment-$stamp"
Copy-Item -LiteralPath $target -Destination $backup -Recurse
[IO.File]::WriteAllText((Join-Path $target 'app_offline.htm'), '<html><body>Updating SPC test API</body></html>')
Start-Sleep -Seconds 4
try {
    Get-ChildItem -LiteralPath $stage -File -Recurse |
        Where-Object { $_.Name -notlike 'appsettings*.json' -and $_.Name -ne 'web.config' } |
        ForEach-Object {
            $stagePrefix = $stage.TrimEnd([char]'\') + '\'
            if (!$_.FullName.StartsWith($stagePrefix, [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe staged payload path.' }
            $relative = $_.FullName.Substring($stagePrefix.Length)
            $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
            if (!$destination.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe payload path.' }
            New-Item -Path (Split-Path $destination) -ItemType Directory -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }
}
catch {
    Copy-Item -Path "$backup\*" -Destination $target -Recurse -Force
    throw
}
finally {
    Remove-Item -LiteralPath (Join-Path $target 'app_offline.htm') -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Seconds 5

if ((Get-FileHash (Join-Path $target 'appsettings.json') -Algorithm SHA256).Hash -ne $settingsHash -or
    (Get-FileHash (Join-Path $target 'web.config') -Algorithm SHA256).Hash -ne $webConfigHash) {
    throw 'Protected test settings changed.'
}
if ((Get-FileHash (Join-Path $stage 'MesSpc.Api.dll') -Algorithm SHA256).Hash -ne
    (Get-FileHash (Join-Path $target 'MesSpc.Api.dll') -Algorithm SHA256).Hash) {
    throw 'Deployed API DLL differs from staged artifact.'
}

$health = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/health' -TimeoutSec 30
$version = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 30
$directVersion = Invoke-RestMethod 'https://spctest.pmr.com.tw:8901/api/version' -TimeoutSec 30
$webResponse = Invoke-WebRequest 'https://spctest.pmr.com.tw/' -UseBasicParsing -TimeoutSec 30
$assetPaths = [regex]::Matches($webResponse.Content, '(?:src|href)="([^"]+\.(?:js|css))"') | ForEach-Object { $_.Groups[1].Value }
$assets = foreach ($path in $assetPaths) {
    $uri = [Uri]::new([Uri]'https://spctest.pmr.com.tw/', $path)
    $response = Invoke-WebRequest $uri.AbsoluteUri -UseBasicParsing -TimeoutSec 30
    [pscustomobject]@{ Uri=$uri.AbsoluteUri; Status=$response.StatusCode; ContentType=$response.Headers.'Content-Type' }
}
if ($health.status -notin @('ok','Healthy') -or $version.version -ne '0.1.162' -or $version.environment -ne 'test' -or
    $directVersion.version -ne '0.1.162' -or $directVersion.environment -ne 'test' -or $webResponse.StatusCode -ne 200 -or
    $assets.Count -lt 2 -or $assets.Where({$_.Status -ne 200 -or ($_.Uri -like '*.js' -and $_.ContentType -notmatch 'javascript') -or ($_.Uri -like '*.css' -and $_.ContentType -notmatch 'css')}).Count) {
    throw 'SPC test post-deployment verification failed.'
}
[pscustomobject]@{
    Backup=$backup; ApiVersion=$version.version; DirectApiVersion=$directVersion.version;
    Environment=$version.environment; Health=$health.status; Web=$webResponse.StatusCode; Assets=$assets
} | ConvertTo-Json -Depth 5
