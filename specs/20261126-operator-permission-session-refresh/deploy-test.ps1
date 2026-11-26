$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$testRoot = 'D:\SPC\release\test'
$apiTarget = Join-Path $testRoot 'backend'
$webTarget = Join-Path $testRoot 'frontend'
$webProject = Join-Path $root 'frontend\mes-spc-web'
$webDist = Join-Path $webProject 'dist'
$appcmd = Join-Path $env:windir 'system32\inetsrv\appcmd.exe'
$pool = 'SpcApiTest'
$origin = 'https://spctest.pmr.com.tw'
$direct = "$($origin):8901"
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backup = Join-Path $root "release\backups\spc-permission-session-1126-$stamp"
$stage = Join-Path $root ".release-staging\spc-permission-session-1126-$stamp"
$payload = @('MesSpc.Api.dll', 'MesSpc.Api.deps.json', 'MesSpc.Api.pdb')

if ($root -ne 'D:\SPC' -or
    (& $appcmd list vdir 'SpcApiTest/' /text:physicalPath).Trim() -ne $apiTarget -or
    (& $appcmd list vdir 'SpcSingleTest/' /text:physicalPath).Trim() -ne $webTarget) {
    throw 'Unexpected SPC test deployment target.'
}
$config = Get-Content (Join-Path $apiTarget 'appsettings.json') -Raw | ConvertFrom-Json
if ($config.AppEnvironment -ne 'test' -or $config.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'SPC test environment or database does not match.'
}

function Test-Api([string]$version) {
    foreach ($base in @($origin, $direct)) {
        $health = Invoke-RestMethod "$base/api/health" -TimeoutSec 15
        $info = Invoke-RestMethod "$base/api/version" -TimeoutSec 15
        if ($health.status -ne 'ok' -or $health.environment -ne 'test' -or $health.version -ne $version -or
            $info.version -ne $version -or $info.environment -ne 'test') { throw 'SPC test API health, version or environment mismatch.' }
    }
}

function Test-Web([string]$version) {
    $landing = Invoke-WebRequest "$origin/" -TimeoutSec 15
    if ($landing.StatusCode -ne 200 -or ($landing.Headers.'Content-Type' -join '') -notmatch 'text/html') { throw 'Invalid test home response.' }
    $paths = [regex]::Matches($landing.Content, '(?:src|href)="(/assets/[^"]+\.(?:js|css))"') | ForEach-Object { $_.Groups[1].Value }
    $kinds = @()
    foreach ($path in $paths) {
        $asset = Invoke-WebRequest "$origin$path" -TimeoutSec 15
        $kind = if ($path.EndsWith('.js')) { 'javascript' } else { 'css' }
        if ($asset.StatusCode -ne 200 -or ($asset.Headers.'Content-Type' -join '') -notmatch $kind) { throw "Invalid test asset response: $path" }
        if ($kind -eq 'javascript' -and !$asset.Content.Contains($version)) { throw 'Test Web version marker mismatch.' }
        if ($kind -eq 'javascript' -and $version -eq '0.1.201' -and !$asset.Content.Contains('/v1/auth/session')) { throw 'Test Web session sync marker missing.' }
        $kinds += $kind
    }
    if ('javascript' -notin $kinds -or 'css' -notin $kinds) { throw 'Test home misses primary JS or CSS.' }
    $paths
}

Test-Api '0.1.178'
Test-Web '0.1.200' | Out-Null
dotnet publish (Join-Path $root 'backend\MesSpc.Api\MesSpc.Api.csproj') -c Release --no-restore -o $stage
if ($LASTEXITCODE -ne 0 -or (Get-Item (Join-Path $stage 'MesSpc.Api.dll')).VersionInfo.ProductVersion -notlike '0.1.179*') {
    throw 'Invalid staged SPC API build.'
}
$index = Get-Content (Join-Path $webDist 'index.html') -Raw
$main = [regex]::Match($index, 'src="(/assets/[^"]+\.js)"').Groups[1].Value.TrimStart('/')
if (!$main -or !(Select-String -LiteralPath (Join-Path $webDist $main) -SimpleMatch '0.1.201' -Quiet)) { throw 'Invalid staged SPC Web build.' }

New-Item -ItemType Directory -Path $backup | Out-Null
foreach ($file in $payload) {
    if (!(Test-Path (Join-Path $apiTarget $file)) -or !(Test-Path (Join-Path $stage $file))) { throw "Missing API payload file: $file" }
    Copy-Item -LiteralPath (Join-Path $apiTarget $file) -Destination $backup
}
Copy-Item -LiteralPath (Join-Path $webTarget 'index.html') -Destination $backup
$locked = @((Join-Path $apiTarget 'appsettings.json'), (Join-Path $apiTarget 'web.config'), (Join-Path $webTarget 'web.config'))
$beforeHashes = (Get-FileHash -LiteralPath $locked).Hash

function Set-Pool([string]$state) {
    & $appcmd $state apppool "/apppool.name:$pool" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Could not $state the SPC test API pool." }
}

function Wait-TestApi([string]$version) {
    for ($attempt = 0; $attempt -lt 8; $attempt++) {
        try { Test-Api $version; return } catch { if ($attempt -eq 7) { throw } }
        Start-Sleep -Seconds 2
    }
}

try {
    Set-Pool 'stop'
    foreach ($file in $payload) { Copy-Item -LiteralPath (Join-Path $stage $file) -Destination (Join-Path $apiTarget $file) -Force }
    Set-Pool 'start'
    Wait-TestApi '0.1.179'
    Copy-Item -Path (Join-Path $webDist 'assets\*') -Destination (Join-Path $webTarget 'assets') -Force
    Copy-Item -LiteralPath (Join-Path $webDist 'index.html') -Destination (Join-Path $webTarget 'index.html') -Force
    $assets = Test-Web '0.1.201'
    if (Compare-Object $beforeHashes (Get-FileHash -LiteralPath $locked).Hash) { throw 'Protected test configuration changed unexpectedly.' }

    Push-Location $webProject
    try {
        @'
import assert from 'node:assert/strict';
import { chromium } from '@playwright/test';
const browser = await chromium.launch();
try {
  const page = await browser.newPage();
  const origin = 'https://spctest.pmr.com.tw';
  const response = await page.goto(origin + '/api/health', { timeout: 20000 });
  assert.equal(response.status(), 200);
  assert.equal((await response.json()).environment, 'test');
  const results = await page.evaluate(async () => {
    const version = await fetch('/api/version', { cache: 'no-store' });
    const session = await fetch('/api/v1/auth/session', { cache: 'no-store' });
    return { ok: version.ok, data: await version.json(), unauthenticated: session.status };
  });
  assert.deepEqual(results, { ok: true, data: { version: '0.1.179', environment: 'test' }, unauthenticated: 401 });
  console.log('Chromium same-origin health/version and anonymous session rejection passed.');
} finally { await browser.close(); }
'@ | node --input-type=module
        if ($LASTEXITCODE -ne 0) { throw 'SPC test Chromium API verification failed.' }
    } finally { Pop-Location }
}
catch {
    $failure = $_
    if ((& $appcmd list apppool $pool /text:state).Trim() -eq 'Started') { Set-Pool 'stop' }
    foreach ($file in $payload) { Copy-Item -LiteralPath (Join-Path $backup $file) -Destination (Join-Path $apiTarget $file) -Force }
    Copy-Item -LiteralPath (Join-Path $backup 'index.html') -Destination (Join-Path $webTarget 'index.html') -Force
    Set-Pool 'start'
    Wait-TestApi '0.1.178'
    Test-Web '0.1.200' | Out-Null
    throw "SPC test release restored to Web 0.1.200 / API 0.1.178. $($failure.Exception.Message)"
}

[pscustomobject]@{
    BeforeWeb = '0.1.200'; AfterWeb = '0.1.201'; BeforeApi = '0.1.178'; AfterApi = '0.1.179'
    Environment = 'test'; Assets = $assets; Backup = $backup
} | ConvertTo-Json -Depth 3
