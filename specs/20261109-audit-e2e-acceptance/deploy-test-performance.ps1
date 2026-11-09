$ErrorActionPreference = 'Stop'
$repo = 'D:\SPC'
$stage = Join-Path $repo 'release-staging\audit-performance-20261110\backend'
$target = Join-Path $repo 'release\test\backend'
$backup = Join-Path $repo 'release\test\backend.backup-audit-performance-20261110'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$pool = 'SpcSingleTest'
$expectedVersion = '0.1.116'
$verifyOnly = $false

$actual = (& $appcmd list vdir 'SpcApiTest/' /text:physicalPath).Trim()
if ($LASTEXITCODE -ne 0 -or $actual -ne $target -or (& $appcmd list app 'SpcApiTest/' /text:apppool.name).Trim() -ne $pool) {
    throw 'Unexpected IIS test target.'
}
$settingsPath = Join-Path $target 'appsettings.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or !$settings.Auth.Enabled -or
    $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'Unexpected test configuration.'
}
if (!(Test-Path -LiteralPath (Join-Path $stage 'MesSpc.Api.dll'))) { throw 'Missing deployment candidate.' }
$candidateVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $stage 'MesSpc.Api.dll')).ProductVersion.Split('+')[0]
if ($candidateVersion -ne $expectedVersion) { throw "Unexpected candidate version $candidateVersion." }
if (Test-Path -LiteralPath $backup) {
    $installedVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $target 'MesSpc.Api.dll')).ProductVersion.Split('+')[0]
    if ($installedVersion -ne $expectedVersion) { throw 'Deployment backup already exists but the candidate is not installed.' }
    $verifyOnly = $true
}
$settingsHash = (Get-FileHash -LiteralPath $settingsPath -Algorithm SHA256).Hash
if (!$verifyOnly) { Copy-Item -LiteralPath $target -Destination $backup -Recurse }
$offline = Join-Path $target 'app_offline.htm'
try {
    if (!$verifyOnly) {
        Set-Content -LiteralPath $offline -Value '<html><body>SPC test deployment in progress.</body></html>' -Encoding utf8
        & $appcmd stop apppool /apppool.name:$pool | Out-Null
        Start-Sleep -Seconds 3
        foreach ($file in Get-ChildItem -LiteralPath $stage -Recurse -File) {
            $relative = [IO.Path]::GetRelativePath($stage, $file.FullName)
            if ($file.Name -like 'appsettings*.json' -or $file.Name -eq 'web.config' -or
                $relative -match '^(uploads|logs|private-data|certificates|App_Data)[\\/]') { continue }
            $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
            if (!$destination.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe payload path.' }
            New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
            if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) {
                throw "Hash mismatch: $relative"
            }
        }
        Remove-Item -LiteralPath $offline -Force
        & $appcmd start apppool /apppool.name:$pool | Out-Null
    }
    if ((Get-FileHash -LiteralPath $settingsPath -Algorithm SHA256).Hash -ne $settingsHash) { throw 'Private settings changed.' }
    $ready = $false
    for ($i = 0; $i -lt 20; $i++) {
        try {
            $version = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 10
            if ($version.environment -eq 'test' -and $version.version -eq $expectedVersion) { $ready = $true; break }
        } catch {}
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'Test API did not return the expected version.' }
    $health = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/health' -TimeoutSec 10
    if ($health.status -ne 'ok' -or $health.environment -ne 'test' -or $health.version -ne $expectedVersion) { throw 'Health response mismatch.' }
    $webResponse = Invoke-WebRequest 'https://spctest.pmr.com.tw/' -UseBasicParsing -TimeoutSec 10
    if ($webResponse.StatusCode -ne 200 -or $webResponse.Headers.'Content-Type' -notmatch 'text/html') { throw 'Web homepage verification failed.' }
    $assets = [regex]::Matches($webResponse.Content, '(?:src|href)="(?<path>/assets/[^"?]+\.(?:js|css))') | ForEach-Object { $_.Groups['path'].Value } | Select-Object -Unique
    $js = $assets | Where-Object { $_ -like '*.js' } | Select-Object -First 1
    $css = $assets | Where-Object { $_ -like '*.css' } | Select-Object -First 1
    if (!$js -or !$css) { throw 'Current Web assets were not found.' }
    $jsResponse = Invoke-WebRequest ("https://spctest.pmr.com.tw" + $js) -UseBasicParsing -TimeoutSec 10
    $cssResponse = Invoke-WebRequest ("https://spctest.pmr.com.tw" + $css) -UseBasicParsing -TimeoutSec 10
    if ($jsResponse.StatusCode -ne 200 -or $jsResponse.Headers.'Content-Type' -notmatch 'javascript' -or
        $cssResponse.StatusCode -ne 200 -or $cssResponse.Headers.'Content-Type' -notmatch 'text/css') { throw 'Web asset MIME verification failed.' }
    if ($jsResponse.Content -notmatch '(/api|baseURL)') { throw 'Browser API base evidence missing from current JavaScript.' }
    try {
        Invoke-WebRequest 'https://spctest.pmr.com.tw/api/v1/audit/events' -UseBasicParsing -TimeoutSec 10 | Out-Null
        throw 'Anonymous audit endpoint was not protected.'
    } catch {
        if ($_.Exception.Response.StatusCode.value__ -ne 401) { throw }
    }
    [pscustomobject]@{
        Status = 'Deployed'; Environment = $version.environment; ApiVersion = $version.version; Health = $health.status
        WebStatus = $webResponse.StatusCode; JavaScript = $js; Css = $css; AnonymousAuditStatus = 401
        SettingsHashPreserved = $true; BackendBackup = $backup
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment-performance-result.json') -Encoding utf8
} catch {
    $failure = $_
    try {
        if (!$verifyOnly) {
            & $appcmd stop apppool /apppool.name:$pool | Out-Null
            Start-Sleep -Seconds 2
            Get-ChildItem -LiteralPath $backup -Force | Copy-Item -Destination $target -Recurse -Force
        }
    }
    finally {
        if (Test-Path -LiteralPath $offline) { Remove-Item -LiteralPath $offline -Force }
        & $appcmd start apppool /apppool.name:$pool | Out-Null
    }
    throw $failure
}
