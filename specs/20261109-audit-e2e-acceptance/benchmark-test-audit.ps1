[CmdletBinding()]
param(
    [int]$ReadSamples = 100,
    [int]$WriteSamples = 30,
    [int]$DisabledPort = 18911,
    [int]$EnabledPort = 18912,
    [string]$ReleasePath = '',
    [string]$OutputPath = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$release = if ([string]::IsNullOrWhiteSpace($ReleasePath)) { Join-Path $repoRoot 'release/test/backend' } else { (Resolve-Path $ReleasePath).Path }
$settingsPath = Join-Path $release 'appsettings.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
if ($settings.AppEnvironment -ne 'test' -or $settings.ConnectionStrings.SqlServer -notmatch '(?i)(Initial Catalog|Database)\s*=\s*PMR_SPC_TEST\s*(;|$)') {
    throw 'Benchmark only permits PMR_SPC_TEST.'
}
if ($ReadSamples -lt 20 -or $WriteSamples -lt 10) { throw 'Insufficient samples.' }

function ConvertTo-Base64Url([byte[]]$Bytes) {
    return [Convert]::ToBase64String($Bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}
function New-TestJwt {
    $now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    $header = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
    $payloadObject = [ordered]@{
        'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier' = '27'
        'http://schemas.microsoft.com/ws/2008/06/identity/claims/role' = 'Editor'
        jti = "audit-benchmark-$([Guid]::NewGuid().ToString('N'))"
        nbf = $now - 5
        exp = $now + 1800
    }
    $payload = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes(($payloadObject | ConvertTo-Json -Compress)))
    $unsigned = "$header.$payload"
    $hmac = [Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes([string]$settings.Auth.JwtKey))
    try { $signature = ConvertTo-Base64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned))) }
    finally { $hmac.Dispose() }
    return "$unsigned.$signature"
}
function Start-BenchmarkApi([int]$Port, [bool]$Enabled) {
    $start = [Diagnostics.ProcessStartInfo]::new('dotnet', 'MesSpc.Api.dll')
    $start.WorkingDirectory = $release
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    # appsettings.json contains Urls, so use the matching configuration key
    # instead of the lower-priority ASPNETCORE_URLS host default.
    $start.Environment['Urls'] = "http://127.0.0.1:$Port"
    $start.Environment['ASPNETCORE_ENVIRONMENT'] = 'Production'
    $start.Environment['Audit__NavigationReadEnabled'] = $Enabled.ToString().ToLowerInvariant()
    $start.Environment['Audit__BusinessDataEnabled'] = $Enabled.ToString().ToLowerInvariant()
    $start.Environment['Audit__Retention__Enabled'] = 'false'
    $start.Environment['Calibration__DeliveryEnabled'] = 'false'
    $start.Environment['SmtpSettings__Host'] = '127.0.0.1'
    $process = [Diagnostics.Process]::Start($start)
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        if ($process.HasExited) { throw "Benchmark API exited on port $Port." }
        try {
            $health = Invoke-RestMethod -Uri "http://127.0.0.1:$Port/api/health" -TimeoutSec 2
            if ($health.status -eq 'ok') { return $process }
        } catch { Start-Sleep -Milliseconds 200 }
    } while ([DateTime]::UtcNow -lt $deadline)
    $process.Kill($true)
    throw "Benchmark API did not become ready on port $Port."
}
function Invoke-Timed([scriptblock]$Action) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    & $Action | Out-Null
    $watch.Stop()
    return $watch.Elapsed.TotalMilliseconds
}
function Get-P95([double[]]$Values) {
    $sorted = @($Values | Sort-Object)
    $index = [Math]::Max(0, [Math]::Ceiling(0.95 * $sorted.Count) - 1)
    return [Math]::Round($sorted[$index], 3)
}
function Invoke-Json([string]$Method, [string]$Uri, $Body = $null) {
    $parameters = @{ Method = $Method; Uri = $Uri; Headers = $headers; TimeoutSec = 20 }
    if ($null -ne $Body) { $parameters.ContentType = 'application/json'; $parameters.Body = ($Body | ConvertTo-Json -Compress) }
    return Invoke-RestMethod @parameters
}

$token = New-TestJwt
$headers = @{ Authorization = "Bearer $token" }
$disabled = $null
$enabled = $null
$prefix = "AUDIT-PERF-$([Guid]::NewGuid().ToString('N').Substring(0, 12))"
try {
    $disabled = Start-BenchmarkApi $DisabledPort $false
    $enabled = Start-BenchmarkApi $EnabledPort $true
    $disabledBase = "http://127.0.0.1:$DisabledPort"
    $enabledBase = "http://127.0.0.1:$EnabledPort"

    1..10 | ForEach-Object {
        Invoke-Json GET "$disabledBase/api/v1/parts" | Out-Null
        Invoke-Json GET "$enabledBase/api/v1/parts" | Out-Null
    }
    [Collections.Generic.List[double]]$readDisabled = @()
    [Collections.Generic.List[double]]$readEnabled = @()
    1..$ReadSamples | ForEach-Object {
        if ($_ % 2) {
            $readDisabled.Add((Invoke-Timed { Invoke-Json GET "$disabledBase/api/v1/parts" }))
            $readEnabled.Add((Invoke-Timed { Invoke-Json GET "$enabledBase/api/v1/parts" }))
        } else {
            $readEnabled.Add((Invoke-Timed { Invoke-Json GET "$enabledBase/api/v1/parts" }))
            $readDisabled.Add((Invoke-Timed { Invoke-Json GET "$disabledBase/api/v1/parts" }))
        }
    }

    [Collections.Generic.List[double]]$writeDisabled = @()
    [Collections.Generic.List[double]]$writeEnabled = @()
    1..$WriteSamples | ForEach-Object {
        foreach ($case in @(
            @{ Name = 'disabled'; Base = $disabledBase; Values = $writeDisabled },
            @{ Name = 'enabled'; Base = $enabledBase; Values = $writeEnabled }
        )) {
            $partNo = "$prefix-$($case.Name)-$_"
            $body = @{ partNo = $partNo; partName = 'Audit performance temporary'; isEnabled = $true }
            $created = $null
            $watch = [Diagnostics.Stopwatch]::StartNew()
            $created = Invoke-Json POST "$($case.Base)/api/v1/parts" $body
            $watch.Stop()
            $elapsed = $watch.Elapsed.TotalMilliseconds
            try { $case.Values.Add($elapsed) }
            finally { if ($created.id) { Invoke-Json DELETE "$($case.Base)/api/v1/parts/$($created.id)" | Out-Null } }
        }
    }

    $remaining = @((Invoke-Json GET "$disabledBase/api/v1/parts") | Where-Object { $_.partNo -like "$prefix*" }).Count
    if ($remaining -ne 0) { throw "Temporary benchmark parts remain: $remaining" }
    $readOff = Get-P95 $readDisabled.ToArray(); $readOn = Get-P95 $readEnabled.ToArray()
    $writeOff = Get-P95 $writeDisabled.ToArray(); $writeOn = Get-P95 $writeEnabled.ToArray()
    $readIncrease = [Math]::Round((($readOn / $readOff) - 1) * 100, 2)
    $writeIncrease = [Math]::Round((($writeOn / $writeOff) - 1) * 100, 2)
    $readAdded = [Math]::Round($readOn - $readOff, 3)
    $writeAdded = [Math]::Round($writeOn - $writeOff, 3)
    $readPassed = if ($readOff -ge 50) { $readIncrease -le 5 } else { $readAdded -le 5 -and $readOn -le 20 }
    $writePassed = if ($writeOff -ge 50) { $writeIncrease -le 10 } else { $writeAdded -le 25 -and $writeOn -le 50 }
    $result = [ordered]@{
        environment = 'test'; database = 'PMR_SPC_TEST'; release = (Split-Path -Leaf $release)
        readSamples = $ReadSamples; writeSamples = $WriteSamples; temporaryRowsRemaining = $remaining
        read = @{ disabledP95Ms = $readOff; enabledP95Ms = $readOn; addedP95Ms = $readAdded; increasePercent = $readIncrease;
            relativeLimitPercent = 5; lowLatencyAddedLimitMs = 5; lowLatencyTotalLimitMs = 20; passed = $readPassed }
        criticalWrite = @{ disabledP95Ms = $writeOff; enabledP95Ms = $writeOn; addedP95Ms = $writeAdded; increasePercent = $writeIncrease;
            relativeLimitPercent = 10; lowLatencyAddedLimitMs = 25; lowLatencyTotalLimitMs = 50; passed = $writePassed }
    }
    $json = $result | ConvertTo-Json -Depth 5
    if ($OutputPath) { [IO.File]::WriteAllText($OutputPath, $json, [Text.UTF8Encoding]::new($false)) }
    $json
    if (-not $result.read.passed -or -not $result.criticalWrite.passed) { exit 2 }
}
finally {
    foreach ($process in @($disabled, $enabled)) {
        if ($null -ne $process -and -not $process.HasExited) { $process.Kill($true); $process.WaitForExit() }
    }
}
