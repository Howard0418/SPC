$ErrorActionPreference = 'Stop'
$repo = 'D:\SPC'
$evidence = Join-Path $repo 'specs\20260914-calibration-date-edit\evidence'
$meta = Get-Content -LiteralPath (Join-Path $evidence 'deployment-stage.json') -Raw | ConvertFrom-Json
$stage = [IO.Path]::GetFullPath($meta.Stage)
$testRoot = Join-Path $repo 'release\test'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
if (-not $stage.StartsWith('D:\SPC\release-staging\calibration-date-edit-', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unexpected staging path' }
if ($meta.Stamp -notmatch '^\d{8}-\d{6}$') { throw 'Invalid backup identifier' }
foreach ($pair in @(@('SpcApi','backend'), @('SpcWeb','frontend'))) {
    $actual = (& $appcmd list vdir ($pair[0] + '/') /text:physicalPath).Trim()
    if ($LASTEXITCODE -ne 0 -or $actual -ne (Join-Path $testRoot $pair[1])) { throw 'IIS target differs from reviewed test directory' }
}
$settings = Get-Content -LiteralPath (Join-Path $testRoot 'backend\appsettings.json') -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or !$settings.Auth.Enabled -or $settings.Calibration.DeliveryEnabled -eq $true -or
    $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') { throw 'Unexpected test configuration' }
$connection = [System.Data.SqlClient.SqlConnection]::new($settings.ConnectionStrings.SqlServer)
try {
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = 'SELECT MigrationId FROM __EFMigrationsHistory'
    $reader = $command.ExecuteReader()
    $applied = [Collections.Generic.HashSet[string]]::new()
    while ($reader.Read()) { [void]$applied.Add($reader.GetString(0)) }
    $reader.Close()
    $sourceIds = Get-ChildItem (Join-Path $repo 'backend\MesSpc.Api\Migrations') -Filter '*.cs' | ForEach-Object {
        foreach ($match in [regex]::Matches((Get-Content -LiteralPath $_.FullName -Raw), '\[Migration\("([^"]+)"\)\]')) { $match.Groups[1].Value }
    } | Sort-Object -Unique
    $pending = @($sourceIds | Where-Object { !$applied.Contains($_) })
    if ($pending.Count) { throw ('Unreviewed pending migrations: ' + ($pending -join ', ')) }
} finally { $connection.Dispose() }

$offline = Join-Path $testRoot 'backend\app_offline.htm'
if (Test-Path -LiteralPath $offline) { throw 'Existing maintenance file; deployment aborted' }
$protected = @()
$payload = @()
foreach ($part in @('backend','frontend')) {
    $target = Join-Path $testRoot $part
    $source = Join-Path $stage $part
    $backup = Join-Path $testRoot ($part + '.backup-' + $meta.Stamp)
    if (!(Test-Path -LiteralPath $source) -or (Test-Path -LiteralPath $backup)) { throw 'Missing staging or backup already exists' }
    $protected += Get-ChildItem -LiteralPath $target -File | Where-Object { $_.Name -like 'appsettings*.json' -or $_.Name -eq 'web.config' } | ForEach-Object {
        [pscustomobject]@{Path=$_.FullName; Hash=(Get-FileHash -LiteralPath $_.FullName).Hash}
    }
    foreach ($file in Get-ChildItem -LiteralPath $source -Recurse -File) {
        $relative = [IO.Path]::GetRelativePath($source, $file.FullName)
        if ($file.Name -like 'appsettings*.json' -or $file.Name -eq 'web.config' -or $relative -match '^(uploads|logs|certificates|App_Data)[\\/]') { continue }
        $destination = [IO.Path]::GetFullPath((Join-Path $target $relative))
        if (!$destination.StartsWith($target + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Unsafe payload path' }
        $payload += [pscustomobject]@{Part=$part;Relative=$relative;Source=$file.FullName;Target=$destination;Backup=(Join-Path $backup $relative);Hash=(Get-FileHash -LiteralPath $file.FullName).Hash}
    }
    New-Item -ItemType Directory -Path $backup | Out-Null
    Get-ChildItem -LiteralPath $target -Force | Where-Object { $_.Name -ne 'logs' } | Copy-Item -Destination $backup -Recurse -Force
}
if (!($payload | Where-Object { $_.Relative -eq 'MesSpc.Api.dll' }) -or !($payload | Where-Object { $_.Relative -eq 'index.html' })) { throw 'Incomplete payload' }
$payload | Select-Object Part,Relative,Hash | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidence 'deployment-payload.json') -Encoding utf8
$smoke = @()
try {
    Set-Content -LiteralPath $offline -Value '<html><body>SPC test deployment in progress.</body></html>' -Encoding utf8
    Start-Sleep -Seconds 2
    # Publish assets before index.html; keep old assets for clients already using the previous page.
    foreach ($item in ($payload | Sort-Object @{Expression={if($_.Relative -eq 'index.html'){1}else{0}}})) {
        New-Item -ItemType Directory -Path (Split-Path $item.Target) -Force | Out-Null
        $copied = $false
        for ($attempt=0; $attempt -lt 12; $attempt++) {
            try { Copy-Item -LiteralPath $item.Source -Destination $item.Target -Force; $copied=$true; break }
            catch { if ($attempt -eq 11) { throw }; Start-Sleep -Seconds 1 }
        }
        if (!$copied -or (Get-FileHash -LiteralPath $item.Target).Hash -ne $item.Hash) { throw 'Payload verification failed' }
    }
    foreach ($config in $protected) { if ((Get-FileHash -LiteralPath $config.Path).Hash -ne $config.Hash) { throw 'Environment configuration changed' } }
    Remove-Item -LiteralPath $offline -Force
    $ready = $false
    for ($attempt=0; $attempt -lt 12; $attempt++) {
        try {
            $version = Invoke-RestMethod -Uri 'http://172.16.110.27:8081/api/version' -TimeoutSec 10
            if ($version.environment -eq 'test') { $ready=$true; break }
        } catch { if ($attempt -eq 11) { throw } }
        Start-Sleep -Seconds 1
    }
    if (!$ready) { throw 'API did not return test environment' }
    $smoke += [pscustomobject]@{Url='http://172.16.110.27:8081/api/version';Status=200;Environment=$version.environment}
    foreach ($path in @('access','preview','commit')) {
        $uri = 'http://172.16.110.27:8081/api/v1/instruments/import/' + $path
        $method = if ($path -eq 'access') { 'GET' } else { 'POST' }
        $response = Invoke-WebRequest -Uri $uri -Method $method -SkipHttpErrorCheck -TimeoutSec 10
        if ($response.StatusCode -ne 401) { throw ('Unexpected anonymous import response: ' + $path) }
        $smoke += [pscustomobject]@{Url=$uri;Status=[int]$response.StatusCode}
    }
    $html = Invoke-WebRequest -Uri 'http://172.16.110.27:8083/calibration-instruments' -TimeoutSec 10
    if ($html.StatusCode -ne 200) { throw 'Web route unavailable' }
    $smoke += [pscustomobject]@{Url='http://172.16.110.27:8083/calibration-instruments';Status=200}
    foreach ($asset in [regex]::Matches($html.Content, '(?:src|href)="(/assets/[^"]+)"')) {
        $uri = 'http://172.16.110.27:8083' + $asset.Groups[1].Value
        $response = Invoke-WebRequest -Uri $uri -TimeoutSec 10
        if ($response.StatusCode -ne 200) { throw 'Web asset unavailable' }
        $smoke += [pscustomobject]@{Url=$uri;Status=200}
    }
    [pscustomobject]@{Status='Deployed';Timestamp=(Get-Date -Format o);BackupId=$meta.Stamp;Environment='test';Database='PMR_SPC_TEST';PendingMigrations=0;PayloadFiles=$payload.Count;ProtectedConfigurationFiles=$protected.Count;ConfigurationHashesPreserved=$true;Smoke=$smoke} |
        ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidence 'deployment-result.json') -Encoding utf8
    'Deployed to SPC test. Backup: ' + $meta.Stamp
    $smoke | Format-Table -AutoSize
} catch {
    $failure = $_
    Set-Content -LiteralPath $offline -Value '<html><body>Restoring SPC test deployment.</body></html>' -Encoding utf8
    Start-Sleep -Seconds 2
    foreach ($item in $payload) {
        # Targets were verified above to remain inside release/test. Only payload files are restored/removed.
        if (Test-Path -LiteralPath $item.Backup) { Copy-Item -LiteralPath $item.Backup -Destination $item.Target -Force }
        elseif (Test-Path -LiteralPath $item.Target) { Remove-Item -LiteralPath $item.Target -Force }
    }
    Remove-Item -LiteralPath $offline -Force
    [pscustomobject]@{Status='RolledBack';BackupId=$meta.Stamp;Reason=$failure.Exception.Message} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidence 'deployment-result.json')
    throw $failure
}

