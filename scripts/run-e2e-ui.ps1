# MES SPC UI 自動化測試（Playwright，有界面 / 非無頭模式）
# 前置：後端 http://localhost:5243、前端 http://localhost:5173 已啟動

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

Set-Location $Root

param(
    [string]$Filter = "",
    [int]$SlowMo = 1500,
    [int]$StepDelayMs = 1000
)

$env:E2E_STEP_DELAY_MS = "$StepDelayMs"
Write-Host "執行 Playwright E2E（Headless=false，SlowMo=${SlowMo}ms，步驟暫停=${StepDelayMs}ms）..." -ForegroundColor Cyan
$args = @(
    "test", ".\tests\MES.SPC.E2ETests\MES.SPC.E2ETests.csproj",
    "--settings", ".\tests\MES.SPC.E2ETests\playwright.runsettings",
    "--logger", "console;verbosity=normal"
)
if ($Filter) { $args += @("--filter", $Filter) }

# SlowMo 需與 tests/MES.SPC.E2ETests/playwright.runsettings 內 <SlowMo> 一致（預設 1500）
if ($SlowMo -ne 1000) {
    Write-Host "提示：若要調整 SlowMo，請修改 playwright.runsettings 的 <SlowMo>（目前腳本參數為 $SlowMo ms）。" -ForegroundColor Yellow
}

dotnet @args
