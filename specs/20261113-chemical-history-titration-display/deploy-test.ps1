$ErrorActionPreference = 'Stop'
$target = 'D:\SPC\release\test\backend'
$stage = 'D:\SPC\release-staging\chemical-history-titration-20261113-final'
$expectedTarget = 'D:\SPC\release\test\backend'
if ([IO.Path]::GetFullPath($target) -ne $expectedTarget) { throw 'Unexpected test target' }
$config = Get-Content "$target\appsettings.json" -Raw | ConvertFrom-Json
if ($config.AppEnvironment -ne 'test' -or $config.ConnectionStrings.SqlServer -notmatch 'PMR_SPC_TEST') {
    throw 'Unexpected test environment'
}

dotnet publish backend/MesSpc.Api/MesSpc.Api.csproj -c Release -o $stage --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Publish failed' }

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = "$target.backup-history-titration-$stamp"
Copy-Item -LiteralPath $target -Destination $backup -Recurse
Set-Content -LiteralPath "$target\app_offline.htm" -Value '<html><body>Updating test API</body></html>'
Start-Sleep -Seconds 5
try {
    Get-ChildItem -LiteralPath $stage -File -Recurse |
        Where-Object { $_.Name -notlike 'appsettings*.json' -and $_.Name -ne 'web.config' } |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($stage, $_.FullName)
            $destination = Join-Path $target $relative
            New-Item -Path (Split-Path $destination) -ItemType Directory -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $destination -Force
        }
}
catch {
    Copy-Item -Path "$backup\*" -Destination $target -Recurse -Force
    throw
}
finally {
    Remove-Item -LiteralPath "$target\app_offline.htm" -Force -ErrorAction SilentlyContinue
}

Start-Sleep -Seconds 4
$health = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/health' -TimeoutSec 30
$version = Invoke-RestMethod 'https://spctest.pmr.com.tw/api/version' -TimeoutSec 30
[pscustomobject]@{
    Backup = $backup
    DllTime = (Get-Item "$target\MesSpc.Api.dll").LastWriteTime
    DllVersion = (Get-Item "$target\MesSpc.Api.dll").VersionInfo.ProductVersion
    Health = $health | ConvertTo-Json -Compress
    Version = $version | ConvertTo-Json -Compress
} | ConvertTo-Json
