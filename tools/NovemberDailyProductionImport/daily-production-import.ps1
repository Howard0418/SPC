[CmdletBinding()]
param(
    [datetime]$ForDate = (Get-Date).Date.AddDays(-1),
    [switch]$PlanOnly,
    [string]$RunRoot = 'C:\ProgramData\PMR\TransFilesDaily'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$transFiles = 'C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\TransFiles'
$date = $ForDate.ToString('yyyy-MM-dd')
$runId = '{0}-{1}' -f $date,(Get-Date -Format 'yyyyMMdd-HHmmss')
$runDir = Join-Path $RunRoot $runId
$lockPath = Join-Path $RunRoot 'daily-production-import.lock'
$settingsPath = Join-Path $RunRoot 'settings.json'
$summaryPath = Join-Path $runDir 'summary.json'
$lock = $null
Import-Module (Join-Path $PSScriptRoot 'ImportSwitchSettings.psm1') -Force
$switches = Get-ImportSwitchSettings -Path $settingsPath

function Write-Summary([string]$Status,[string]$Message,[hashtable]$Extra = @{}) {
    $summary = [ordered]@{
        task = 'TRANSFILES-NOV-DAILY-1110-TASK-005'
        reportDate = $date
        status = $Status
        message = $Message
        hostName = $env:COMPUTERNAME
        operator = [Security.Principal.WindowsIdentity]::GetCurrent().Name
        planOnly = [bool]$PlanOnly
        completedAt = (Get-Date).ToString('o')
    }
    foreach ($key in $Extra.Keys) { $summary[$key] = $Extra[$key] }
    New-Item -ItemType Directory -Path $runDir -Force | Out-Null
    $summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $summaryPath -Encoding utf8
}

try {
    New-Item -ItemType Directory -Path $RunRoot -Force | Out-Null
    if(-not $switches.MasterEnabled){Write-Summary 'Disabled' '每日匯入總開關已關閉；未掃描來源、未連線資料庫。' @{enableChemical=$switches.EnableChemical;enableEtching=$switches.EnableEtching};exit 0}
    try { $lock = [IO.File]::Open($lockPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None) }
    catch [IO.IOException] { throw "另一個每日匯入仍在執行：$lockPath" }
    New-Item -ItemType Directory -Path $runDir -Force | Out-Null

    if (-not (Test-Path $transFiles)) { throw "找不到 TransFiles：$transFiles" }
    $sourceDir = Join-Path $runDir 'source'
    New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null
    $configPath = Join-Path $runDir 'scheduled-import-config.json'
    [ordered]@{
        mode = 'dry-run'
        environment = 'production'
        chemical_source = '\\172.16.119.200\LineData\產線-品保部\03_QC\01_IQC.IPQC\IPQC\藥水分析\藥水分析記錄表\2026年'
        etching_source = '\\172.16.119.200\LineData\產線-品保部\03_QC\01_IQC.IPQC\PD-3-581-06B-咬蝕量_2026.xlsx'
        output_dir = $sourceDir
        state_file = (Join-Path $sourceDir 'state.json')
        start_date = $date
        end_date = $date
        requested_by = [Security.Principal.WindowsIdentity]::GetCurrent().Name
        enable_chemical_preview = $false
        enable_etching_preview = $false
        enable_confirm = $false
        max_retries = 2
    } | ConvertTo-Json | Set-Content -LiteralPath $configPath -Encoding utf8

    Push-Location $transFiles
    try { & python .\scheduled_import.py --config $configPath; if ($LASTEXITCODE -ne 0) { throw "來源掃描失敗，exit=$LASTEXITCODE" } }
    finally { Pop-Location }
    $sourceReport = Get-ChildItem -LiteralPath $sourceDir -Filter 'scheduled_import_*.json' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $sourceReport) { throw '來源掃描未產生報告。' }
    $source = Get-Content -LiteralPath $sourceReport.FullName -Raw | ConvertFrom-Json
    $source.items=@(Select-EnabledImportItems -Items $source.items -Settings $switches)
    $source.errors=@()
    $source | ConvertTo-Json -Depth 100 | Set-Content -LiteralPath $sourceReport.FullName -Encoding utf8
    $blockedStatuses = @('error','invalid_format','manual_review','quarantined')
    $blocked = @($source.items | Where-Object { $_.status -in $blockedStatuses })
    if ($source.errors.Count -gt 0 -or $blocked.Count -gt 0) {
        throw "來源驗證未通過：errors=$($source.errors.Count)，blocked=$($blocked.Count)"
    }

    $snapshotDir = Join-Path $runDir 'snapshot-before'
    & (Join-Path $repoRoot 'specs\20261110-transfiles-november-daily-import\snapshot-production-databases.ps1') -OutputDirectory $snapshotDir -ReportDate $date
    $reconciliation = Join-Path $runDir 'reconciliation-before.json'
    & python (Join-Path $repoRoot 'specs\20261110-transfiles-november-daily-import\reconcile-november.py') $sourceReport.FullName (Join-Path $snapshotDir 'spc-production-snapshot.json') (Join-Path $snapshotDir 'portal-production-snapshot.json') $reconciliation --environment production
    if ($LASTEXITCODE -ne 0) { throw "比對失敗，exit=$LASTEXITCODE" }
    $recon = Get-Content -LiteralPath $reconciliation -Raw | ConvertFrom-Json
    $mappingErrors = @($recon.chemical.mappingErrors).Count
    $conflicts = @($recon.chemical.rows | Where-Object classification -eq 'same_key_different_content').Count + @($recon.etching.rows | Where-Object classification -eq 'same_key_different_content').Count
    if ($mappingErrors -gt 0 -or $conflicts -gt 0) { throw "正式匯入已阻擋：mappingErrors=$mappingErrors，conflicts=$conflicts" }
    $chemicalCandidates = @($recon.chemical.rows | Where-Object classification -eq 'insert_candidate').Count
    $etchCandidates = @($recon.etching.rows | Where-Object classification -eq 'insert_candidate').Count
    if (($chemicalCandidates + $etchCandidates) -eq 0) {
        Write-Summary 'NoChange' '啟用類型的來源皆已存在或當日沒有可匯入資料；未備份、未寫入。' @{ sourceReport=$sourceReport.FullName; reconciliation=$reconciliation; chemicalCandidates=0; etchCandidates=0;enableChemical=$switches.EnableChemical;enableEtching=$switches.EnableEtching }
        exit 0
    }

    $evidence = Join-Path $runDir 'import-evidence'
    $project = Join-Path $repoRoot 'tools\NovemberTestImport\NovemberTestImport.csproj'
    & dotnet run --project $project -- --prod-plan $sourceReport.FullName $reconciliation $evidence "--daily-date=$date"
    if ($LASTEXITCODE -ne 0) { throw "正式預檢失敗，exit=$LASTEXITCODE" }
    if ($PlanOnly) {
        Write-Summary 'Planned' '預檢通過；PlanOnly 未寫入正式資料。' @{ sourceReport=$sourceReport.FullName; reconciliation=$reconciliation; chemicalCandidates=$chemicalCandidates; etchCandidates=$etchCandidates }
        exit 0
    }

    $sha = (Get-FileHash -LiteralPath $reconciliation -Algorithm SHA256).Hash
    & dotnet run --project $project -- --prod-apply $sourceReport.FullName $reconciliation $evidence '--confirm-spc=PMR_SPC_2026' '--confirm-portal=PMR_PORTAL_UAT' "--confirm-reconciliation-sha256=$sha" "--daily-date=$date"
    if ($LASTEXITCODE -ne 0) { throw "正式寫入失敗，exit=$LASTEXITCODE" }
    & dotnet run --project $project -- --prod-verify $sourceReport.FullName $reconciliation $evidence "--daily-date=$date"
    if ($LASTEXITCODE -ne 0) { throw "正式寫入後驗證失敗，exit=$LASTEXITCODE" }

    $snapshotAfter = Join-Path $runDir 'snapshot-after'
    & (Join-Path $repoRoot 'specs\20261110-transfiles-november-daily-import\snapshot-production-databases.ps1') -OutputDirectory $snapshotAfter -ReportDate $date
    $reconciliationAfter = Join-Path $runDir 'reconciliation-after.json'
    & python (Join-Path $repoRoot 'specs\20261110-transfiles-november-daily-import\reconcile-november.py') $sourceReport.FullName (Join-Path $snapshotAfter 'spc-production-snapshot.json') (Join-Path $snapshotAfter 'portal-production-snapshot.json') $reconciliationAfter --environment production
    if ($LASTEXITCODE -ne 0) { throw "寫入後比對失敗，exit=$LASTEXITCODE" }
    $after = Get-Content -LiteralPath $reconciliationAfter -Raw | ConvertFrom-Json
    $remaining = @($after.chemical.rows | Where-Object classification -ne 'existing_same').Count + @($after.etching.rows | Where-Object classification -ne 'existing_same').Count
    if ($remaining -gt 0 -or @($after.chemical.mappingErrors).Count -gt 0) { throw "寫入後仍有 $remaining 筆未對齊。" }
    Write-Summary 'Completed' '正式匯入與逐筆比對完成。' @{ sourceReport=$sourceReport.FullName; reconciliationBefore=$reconciliation; reconciliationAfter=$reconciliationAfter; chemicalCandidates=$chemicalCandidates; etchCandidates=$etchCandidates }
}
catch {
    Write-Summary 'Failed' $_.Exception.Message
    Write-Error $_
    exit 1
}
finally {
    if ($lock) { $lock.Dispose() }
    Remove-Item -LiteralPath $lockPath -Force -ErrorAction SilentlyContinue
}
