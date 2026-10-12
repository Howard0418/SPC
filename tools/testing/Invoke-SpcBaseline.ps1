[CmdletBinding()]
param(
    [string]$TestFilter = "",

    [switch]$SkipTests,

    [switch]$SkipFrontend,

    [string[]]$SmokeUrl = @()
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$backendProject = Join-Path $repoRoot 'backend/MesSpc.Api/MesSpc.Api.csproj'
$testProject = Join-Path $repoRoot 'tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj'
$frontendRoot = Join-Path $repoRoot 'frontend/mes-spc-web'

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-Host "==> $Name"
    & $Action
    Write-Host "OK: $Name"
}

Push-Location $repoRoot
try {
    if (-not $SkipTests) {
        Invoke-Step -Name 'Backend tests' -Action {
            $args = @('test', $testProject, '--no-restore', '-p:UseSharedCompilation=false')
            if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
                $args += @('--filter', $TestFilter)
            }
            & dotnet @args
        }
    }

    Invoke-Step -Name 'Backend build' -Action {
        & dotnet build $backendProject --no-restore -p:UseSharedCompilation=false
    }

    if (-not $SkipFrontend) {
        Invoke-Step -Name 'Frontend build' -Action {
            Push-Location $frontendRoot
            try {
                & npm run build
            }
            finally {
                Pop-Location
            }
        }
    }

    foreach ($url in $SmokeUrl) {
        if ([string]::IsNullOrWhiteSpace($url)) { continue }
        Invoke-Step -Name "Smoke $url" -Action {
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -Method Get -TimeoutSec 15
            $statusCode = [int]$response.StatusCode
            if ($statusCode -lt 200 -or $statusCode -gt 399) {
                throw "Smoke failed: $url returned HTTP $statusCode."
            }
        }
    }
}
finally {
    Pop-Location
}
