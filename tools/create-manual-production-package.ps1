param(
    [string]$OutputRoot = "D:\SPC\release-packages",
    [string]$PackageName = "Portal-SPC-20260910"
)

$ErrorActionPreference = "Stop"
$spcRoot = "D:\SPC"
$portalRoot = "D:\PmrPortal"
$packageRoot = Join-Path $OutputRoot $PackageName
$components = @(
    @{ Name = "SPC_API"; Project = Join-Path $spcRoot "backend\MesSpc.Api\MesSpc.Api.csproj"; Type = "dotnet" },
    @{ Name = "Portal_API"; Project = Join-Path $portalRoot "src\PmrPortal.Api\PmrPortal.Api.csproj"; Type = "dotnet" },
    @{ Name = "Portal_Web"; Project = Join-Path $portalRoot "src\PmrPortal.Web\PmrPortal.Web.csproj"; Type = "dotnet" },
    @{ Name = "SPC_Web"; Project = Join-Path $spcRoot "frontend\mes-spc-web"; Type = "vite" }
)

if (Test-Path -LiteralPath $packageRoot) {
    throw "發布包目錄已存在，拒絕覆蓋：$packageRoot"
}
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null

foreach ($component in $components) {
    $destination = Join-Path $packageRoot $component.Name
    if ($component.Type -eq "dotnet") {
        dotnet publish $component.Project -c Release -o $destination --no-restore
        if ($LASTEXITCODE -ne 0) { throw "$($component.Name) publish failed." }
    }
    else {
        Push-Location $component.Project
        try {
            npm run build:production
            if ($LASTEXITCODE -ne 0) { throw "SPC_Web production build failed." }
            New-Item -ItemType Directory -Path $destination -Force | Out-Null
            Copy-Item ".\dist\*" $destination -Recurse -Force
        }
        finally { Pop-Location }
    }

    Get-ChildItem -LiteralPath $destination -Recurse -Force -File |
        Where-Object { $_.Name -like "appsettings*.json" -or $_.Extension -in ".pfx", ".p12" -or $_.FullName -match "[\\/]uploads[\\/]" } |
        Remove-Item -Force
    Get-ChildItem -LiteralPath $destination -Recurse -Force -Directory |
        Where-Object { $_.Name -ieq "uploads" -and (Get-ChildItem -LiteralPath $_.FullName -Force | Measure-Object).Count -eq 0 } |
        Remove-Item -Force

    $files = Get-ChildItem -LiteralPath $destination -Recurse -File | ForEach-Object {
        [PSCustomObject]@{
            Path = $_.FullName.Substring($destination.Length).TrimStart('\\')
            Size = $_.Length
            Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    }
    [PSCustomObject]@{
        Component = $component.Name
        CreatedAtUtc = [DateTime]::UtcNow.ToString("o")
        Build = if ($component.Type -eq "dotnet") { "dotnet publish -c Release --no-restore" } else { "npm run build:production" }
        Excluded = @("appsettings*.json", "uploads", "*.pfx", "*.p12", "IIS environment variables", "server certificates")
        Files = $files
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $destination "manifest.json") -Encoding utf8
}

 $createdAtUtc = [DateTime]::UtcNow.ToString("o")
@'
# Portal + SPC 手動正式部署包

建立時間（UTC）：__CREATED_AT_UTC__

本包只含程式與前端靜態檔，不含 `appsettings*.json`、`uploads`、憑證或 IIS 環境變數。請勿使用 `/MIR`、`robocopy /MIR` 或刪除式同步覆蓋正式目錄。

## 包內容

| 資料夾 | 部署主機 | 部署目標 |
|---|---|---|
| `SPC_API` | SPC 正式伺服器 | SPC API 發布目錄 |
| `SPC_Web` | SPC 正式伺服器 | SPC Web 發布目錄 |
| `Portal_API` | Portal 正式伺服器 | Portal API 發布目錄 |
| `Portal_Web` | Portal 正式伺服器 | Portal Web 發布目錄 |

每個資料夾的 `manifest.json` 含 SHA-256 雜湊，可在複製後驗證。

## 部署順序

1. 於各正式伺服器確認實體目錄、App Pool、IIS 環境變數及正式資料庫/正式 SPC API 指向；不執行 migration。
2. 在正式目錄外完整備份目前 API/Web；Portal API 的 `uploads` 需獨立備份。
3. 在目標 API/Web 目錄放置 `app_offline.htm`，停止或等待 App Pool 釋放檔案。
4. 依序覆蓋：SPC API、SPC Web、Portal API、Portal Web。只複製本包內檔案，不覆蓋或刪除 `appsettings*.json`、`uploads`、憑證與 IIS 環境變數。
5. 移除 `app_offline.htm`，回收對應 App Pool。
6. 驗證正式 API health、SPC Web、Portal Web、AD/工號登入，以及 Portal 藥液輸入至 SPC 管制圖流程。

## 回復

若 health、登入、附件或 Portal→SPC 流程失敗：重新放置 `app_offline.htm`，停止 App Pool，將部署前備份覆蓋回原目錄，保留設定與 uploads，移除離線頁後回收 App Pool，再執行 health 與登入驗證。
'@.Replace("__CREATED_AT_UTC__", $createdAtUtc) | Set-Content -LiteralPath (Join-Path $packageRoot "README.md") -Encoding utf8

Write-Host "Package created: $packageRoot" -ForegroundColor Green