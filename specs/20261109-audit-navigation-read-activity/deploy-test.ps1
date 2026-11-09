$ErrorActionPreference = 'Stop'
$repo = 'D:\SPC'
$stamp = 'audit-navigation-read-20261109-r2'
$testRoot = Join-Path $repo 'release\test'
$stage = Join-Path $repo 'release-staging\audit-navigation-read-20261109'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'

foreach ($pair in @(@('SpcApiTest','backend'), @('SpcSingleTest','frontend'))) {
    $actual = (& $appcmd list vdir ($pair[0] + '/') /text:physicalPath).Trim()
    if ($LASTEXITCODE -ne 0 -or $actual -ne (Join-Path $testRoot $pair[1])) { throw 'IIS target differs from reviewed test directory' }
}
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
    Start-Sleep -Seconds 2
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
    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    if ($null -eq $settings.Audit) { $settings | Add-Member -NotePropertyName Audit -NotePropertyValue ([pscustomobject]@{}) }
    $settings.Audit | Add-Member -NotePropertyName NavigationReadEnabled -NotePropertyValue $true -Force
    $settings | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $settingsPath -Encoding utf8
    Remove-Item -LiteralPath $offline -Force
    $ready = $false
    for ($i=0; $i -lt 12; $i++) { try { $version=Invoke-RestMethod -Uri 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 10; if ($version.environment -eq 'test') {$ready=$true;break} } catch {}; Start-Sleep -Seconds 1 }
    if (!$ready) { throw 'Test API did not return test environment' }
    [pscustomobject]@{Status='Deployed';Environment=$version.environment;Version=$version.version;BackendBackup=(Join-Path $testRoot ('backend.backup-'+$stamp));FrontendBackup=(Join-Path $testRoot ('frontend.backup-'+$stamp));NavigationReadEnabled=$true} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'deployment-result.json') -Encoding utf8
} catch {
    foreach ($part in @('backend','frontend')) {
        $backup = Join-Path $testRoot ($part + '.backup-' + $stamp); $target = Join-Path $testRoot $part
        if (Test-Path -LiteralPath $backup) {
            Get-ChildItem -LiteralPath $backup -Force | Copy-Item -Destination $target -Recurse -Force
        }
    }
    if (Test-Path -LiteralPath $offline) { Remove-Item -LiteralPath $offline -Force }
    throw
}
