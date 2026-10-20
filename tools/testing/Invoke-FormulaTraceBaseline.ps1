param(
    [string]$PortalRoot = (Join-Path $PSScriptRoot '..\..\..\PmrPortal'),
    [Parameter(Mandatory=$true)][string]$TransFilesRoot
)
$ErrorActionPreference = 'Stop'
$taskRepo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$taskPortalSource = Join-Path $PortalRoot 'src\PmrPortal.Api\Services\ExcelFormulaEvaluator.cs'
if (-not (Test-Path -LiteralPath $taskPortalSource -PathType Leaf)) { throw '缺少真實 Portal evaluator 來源，不執行不完整基準。' }
foreach ($taskName in @('batch_convert_gui.py','direct_import.py')) {
    if (-not (Test-Path -LiteralPath (Join-Path $TransFilesRoot $taskName) -PathType Leaf)) { throw "缺少 TransFiles 來源：$taskName" }
}
# Hashes describe inputs only; never read credentials or execute source GUI/network/import functions.
Write-Output ('PortalEvaluatorSHA256=' + (Get-FileHash -LiteralPath $taskPortalSource).Hash)
Write-Output ('TransFilesGuiSHA256=' + (Get-FileHash -LiteralPath (Join-Path $TransFilesRoot 'batch_convert_gui.py')).Hash)
& python -B (Join-Path $PSScriptRoot 'test_formula_trace_sources.py') --transfiles-root $TransFilesRoot
if ($LASTEXITCODE -ne 0) { throw 'TransFiles characterization failed.' }
& dotnet test (Join-Path $taskRepo 'tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj') --no-restore --filter FormulaTraceCompatibilityTests "-p:FormulaCompatibilityPortalSource=$([IO.Path]::GetFullPath($taskPortalSource))" -p:UseSharedCompilation=false --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Formula characterization failed.' }
Write-Output 'BASELINE_COMPLETE: observations only; no feature activation, DB writes, or deployment.'
