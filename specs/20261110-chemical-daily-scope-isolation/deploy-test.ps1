$ErrorActionPreference = 'Stop'
$stamp = 'chemical-daily-scope-20261110'
$spcRepo = 'D:\SPC'
$portalRepo = 'D:\PmrPortal'
$spcTarget = Join-Path $spcRepo 'release\test\backend'
$portalTarget = Join-Path $portalRepo 'publish\portal-web'
$stage = Join-Path $spcRepo 'release-staging\chemical-daily-scope-20261110'
$spcStage = Join-Path $stage 'spc-api'
$portalStage = Join-Path $stage 'portal-web'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'

$actualSpc = (& $appcmd list vdir 'SpcApiTest/' /text:physicalPath).Trim()
$actualPortal = (& $appcmd list vdir 'PmrPortalWeb/' /text:physicalPath).Trim()
if ($actualSpc -ne $spcTarget -or $actualPortal -ne $portalTarget) { throw 'IIS target differs from reviewed test directory.' }
$spcSettings = Get-Content (Join-Path $spcTarget 'appsettings.json') -Raw | ConvertFrom-Json
$portalSettings = Get-Content (Join-Path $portalTarget 'appsettings.json') -Raw | ConvertFrom-Json
if ($spcSettings.AppEnvironment -ne 'test' -or $spcSettings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') { throw 'Unexpected SPC test configuration.' }
if ($portalSettings.PortalApi.BaseUrl -ne 'http://172.16.110.27:8091/') { throw 'Unexpected Portal test API target.' }

if (Test-Path $stage) { throw "Stage already exists: $stage" }
New-Item -ItemType Directory -Path $spcStage,$portalStage -Force | Out-Null
dotnet publish (Join-Path $spcRepo 'backend\MesSpc.Api\MesSpc.Api.csproj') -c Release -o $spcStage --no-restore
if ($LASTEXITCODE -ne 0) { throw 'SPC API publish failed.' }
dotnet publish (Join-Path $portalRepo 'src\PmrPortal.Web\PmrPortal.Web.csproj') -c Release -o $portalStage --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Portal Web publish failed.' }

$targets = @(
    @{ Name='spc-api'; Source=$spcStage; Target=$spcTarget },
    @{ Name='portal-web'; Source=$portalStage; Target=$portalTarget }
)
foreach ($item in $targets) {
    $backup = $item.Target + '.backup-' + $stamp
    if (Test-Path $backup) { throw "Backup already exists: $backup" }
    New-Item -ItemType Directory -Path $backup | Out-Null
    Get-ChildItem $item.Target -Force | Copy-Item -Destination $backup -Recurse -Force
}

try {
    foreach ($item in $targets) { Set-Content (Join-Path $item.Target 'app_offline.htm') '<html><body>Test deployment in progress.</body></html>' -Encoding utf8 }
    Start-Sleep -Seconds 2
    foreach ($item in $targets) {
        foreach ($file in Get-ChildItem $item.Source -Recurse -File) {
            $relative = [IO.Path]::GetRelativePath($item.Source,$file.FullName)
            if ($file.Name -like 'appsettings*.json' -or $file.Name -eq 'web.config') { continue }
            $destination = [IO.Path]::GetFullPath((Join-Path $item.Target $relative))
            if (!$destination.StartsWith($item.Target+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe payload path.' }
            New-Item -ItemType Directory -Path (Split-Path $destination) -Force | Out-Null
            Copy-Item $file.FullName $destination -Force
            if ((Get-FileHash $file.FullName).Hash -ne (Get-FileHash $destination).Hash) { throw "Hash mismatch: $relative" }
        }
    }
} catch {
    foreach ($item in $targets) {
        $backup = $item.Target + '.backup-' + $stamp
        Get-ChildItem $backup -Force | Copy-Item -Destination $item.Target -Recurse -Force
    }
    throw
} finally {
    foreach ($item in $targets) { Remove-Item (Join-Path $item.Target 'app_offline.htm') -Force -ErrorAction SilentlyContinue }
}

[pscustomobject]@{
    Status='Deployed'; Environment='test'; SpcApiVersion='0.1.117'; PortalWebVersion='1.0.267';
    SpcBackup=$spcTarget+'.backup-'+$stamp; PortalBackup=$portalTarget+'.backup-'+$stamp
} | ConvertTo-Json | Set-Content (Join-Path $PSScriptRoot 'deployment-result.json') -Encoding utf8
