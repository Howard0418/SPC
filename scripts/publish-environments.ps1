param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$OutputRoot = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repoRoot "release"
}

$apiProject = Join-Path $repoRoot "backend\MesSpc.Api\MesSpc.Api.csproj"
$webRoot = Join-Path $repoRoot "frontend\mes-spc-web"
$commit = (git -C $repoRoot rev-parse --short HEAD).Trim()
$publishedAt = Get-Date -Format "yyyy-MM-dd HH:mm:ss"

function Reset-FrontendOutput {
    param([Parameter(Mandatory = $true)][string]$Path)

    $resolvedRoot = [IO.Path]::GetFullPath($OutputRoot).TrimEnd('\') + '\'
    $resolvedPath = [IO.Path]::GetFullPath($Path).TrimEnd('\') + '\'
    if (-not $resolvedPath.StartsWith($resolvedRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "拒絕清除 release 以外的目錄：$Path"
    }

    New-Item -ItemType Directory -Path $Path -Force | Out-Null
    Get-ChildItem -LiteralPath $Path -Force | Remove-Item -Recurse -Force
}

foreach ($environment in @("test", "production")) {
    $environmentRoot = Join-Path $OutputRoot $environment
    $backendOutput = Join-Path $environmentRoot "backend"
    $frontendOutput = Join-Path $environmentRoot "frontend"
    $settingsPath = Join-Path $backendOutput "appsettings.json"

    if (-not (Test-Path -LiteralPath $settingsPath)) {
        throw "缺少 $environment 後端固定設定：$settingsPath"
    }

    $settingsBackup = Get-Content -LiteralPath $settingsPath -Raw
    New-Item -ItemType Directory -Path $backendOutput -Force | Out-Null
    Write-Host "[Publish] $environment backend -> $backendOutput" -ForegroundColor Cyan
    dotnet publish $apiProject -c $Configuration -o $backendOutput --no-restore
    if ($LASTEXITCODE -ne 0) { throw "$environment backend publish failed." }
    Set-Content -LiteralPath $settingsPath -Value $settingsBackup -Encoding utf8

    $viteMode = if ($environment -eq "production") { "production" } else { "testhost" }
    Write-Host "[Build] $environment frontend (mode=$viteMode)" -ForegroundColor Cyan
    Push-Location $webRoot
    try {
        npm run build -- --mode $viteMode
        if ($LASTEXITCODE -ne 0) { throw "$environment frontend build failed." }
    }
    finally {
        Pop-Location
    }

    Reset-FrontendOutput -Path $frontendOutput
    Copy-Item -Path (Join-Path $webRoot "dist\*") -Destination $frontendOutput -Recurse -Force

    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $connection = [string]$settings.ConnectionStrings.SqlServer
    $database = if ($connection -match '(?i)(Initial Catalog|Database)\s*=\s*([^;]+)') { $Matches[2] } else { "unknown" }
    $apiVersion = (Get-Item (Join-Path $backendOutput "MesSpc.Api.dll")).VersionInfo.ProductVersion
    $webVersion = (Get-Content (Join-Path $webRoot "package.json") -Raw | ConvertFrom-Json).version

    @"
SPC $environment release
PublishedAt: $publishedAt
GitCommit: $commit
ApiVersion: $apiVersion
WebVersion: $webVersion
AppEnvironment: $($settings.AppEnvironment)
Database: $database
"@ | Set-Content -LiteralPath (Join-Path $environmentRoot "release-manifest.txt") -Encoding utf8
}

Write-Host "[DONE] SPC test and production packages are ready under $OutputRoot" -ForegroundColor Green
