$ErrorActionPreference = 'Stop'
$repo = 'D:\SPC'
$stamp = 'audit-query-export-20261109'
$testRoot = Join-Path $repo 'release\test'
$stage = Join-Path $repo 'release-staging\audit-query-export-20261109'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$pool = 'SpcSingleTest'

foreach ($pair in @(@('SpcApiTest','backend'), @('SpcSingleTest','frontend'))) {
    $actual = (& $appcmd list vdir ($pair[0] + '/') /text:physicalPath).Trim()
    if ($LASTEXITCODE -ne 0 -or $actual -ne (Join-Path $testRoot $pair[1])) { throw 'IIS target differs from reviewed test directory' }
}
if ((& $appcmd list app 'SpcApiTest/' /text:apppool.name).Trim() -ne $pool -or
    (& $appcmd list app 'SpcSingleTest/' /text:apppool.name).Trim() -ne $pool) { throw 'Unexpected test application pool' }
$settingsPath = Join-Path $testRoot 'backend\appsettings.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or !$settings.Auth.Enabled -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') { throw 'Unexpected test configuration' }
foreach ($part in @('backend','frontend')) {
    if (!(Test-Path -LiteralPath (Join-Path $stage $part))) { throw "Missing stage $part" }
    $backup = Join-Path $testRoot ($part + '.backup-' + $stamp)
    if (Test-Path -LiteralPath $backup) { throw "Backup already exists: $backup" }
    New-Item -ItemType Directory -Path $backup | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $testRoot $part) -Force | Copy-Item -Destination $backup -Recurse -Force
}
$offline = Join-Path $testRoot 'backend\app_offline.htm'
if (Test-Path -LiteralPath $offline) { throw 'Existing maintenance file; deployment aborted' }
try {
    Set-Content -LiteralPath $offline -Value '<html><body>SPC test deployment in progress.</body></html>' -Encoding utf8
    & $appcmd stop apppool /apppool.name:$pool | Out-Null
    Start-Sleep -Seconds 3
    foreach ($part in @('backend','frontend')) {
        $source = Join-Path $stage $part; $target = Join-Path $testRoot $part
        foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
            $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
            if ($file.Name -like 'appsettings*.json' -or $file.Name -eq 'web.config' -or $relative -match '^(uploads|logs|private-data|certificates|App_Data)[\\/]') { continue }
            $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
            if (!$destination.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe payload path' }
            New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
            Copy-Item -LiteralPath $file.FullName -Destination $destination -Force
            if ((Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $destination).Hash) { throw "Hash mismatch $relative" }
        }
    }
    Remove-Item -LiteralPath $offline -Force
    & $appcmd start apppool /apppool.name:$pool | Out-Null
    $ready = $false
    for ($i=0; $i -lt 12; $i++) { try { $version=Invoke-RestMethod -Uri 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 10; if ($version.environment -eq 'test' -and $version.version -eq '0.1.110') {$ready=$true;break} } catch {}; Start-Sleep -Seconds 1 }
    if (!$ready) { throw 'Test API did not return expected test version' }
    [pscustomobject]@{Status='Deployed';Environment=$version.environment;ApiVersion=$version.version;WebVersion='0.1.121';BackendBackup=(Join-Path $testRoot ('backend.backup-'+$stamp));FrontendBackup=(Join-Path $testRoot ('frontend.backup-'+$stamp))} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment-result.json') -Encoding utf8
} catch {
    $failure = $_
    try {
        foreach ($part in @('backend','frontend')) {
            $backup = Join-Path $testRoot ($part + '.backup-' + $stamp); $target = Join-Path $testRoot $part
            if (Test-Path -LiteralPath $backup) { Get-ChildItem -LiteralPath $backup -Force | Copy-Item -Destination $target -Recurse -Force }
        }
    } finally {
        if (Test-Path -LiteralPath $offline) { Remove-Item -LiteralPath $offline -Force }
        & $appcmd start apppool /apppool.name:$pool | Out-Null
    }
    throw $failure
}
