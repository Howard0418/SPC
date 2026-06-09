$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==================================================="
Write-Host "   MES-SPC 系統一鍵發佈腳本 (One-Click Publish)"
Write-Host "==================================================="
Write-Host ""

$RootDir = $PSScriptRoot
$PublishDir = Join-Path $RootDir "publish"
$FrontendPublishDir = Join-Path $PublishDir "frontend"
$BackendPublishDir = Join-Path $PublishDir "backend"

# ── IIS App Pool 名稱（若與此不同請自行修改）──
$AppPoolName = "MesSpc"

function Stop-AppPool {
    try {
        $pool = Get-WebConfigurationProperty -pspath 'MACHINE/WEBROOT/APPHOST' `
            -filter "system.applicationHost/applicationPools/add[@name='$AppPoolName']" -name "state" -ErrorAction SilentlyContinue
        if ($pool -ne $null) {
            Write-Host "  ⏹  停止 IIS 應用程式集區 [$AppPoolName]..."
            Stop-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        }
    } catch {
        Write-Host "  ⚠  無法停止 App Pool（可能尚未設定或已停止），繼續..." -ForegroundColor Yellow
    }
}

function Start-AppPool {
    try {
        Write-Host "  ▶  重新啟動 IIS 應用程式集區 [$AppPoolName]..."
        Start-WebAppPool -Name $AppPoolName -ErrorAction SilentlyContinue
    } catch {
        Write-Host "  ⚠  無法啟動 App Pool，請手動啟動。" -ForegroundColor Yellow
    }
}

Write-Host "[1/3] 準備發佈資料夾..."
if (Test-Path $FrontendPublishDir) { Remove-Item -Path $FrontendPublishDir -Recurse -Force }

# 停止 IIS App Pool 再刪除後端目錄，避免 DLL 被佔用
Stop-AppPool
if (Test-Path $BackendPublishDir) { Remove-Item -Path $BackendPublishDir -Recurse -Force }

New-Item -ItemType Directory -Force -Path $FrontendPublishDir | Out-Null
New-Item -ItemType Directory -Force -Path $BackendPublishDir | Out-Null

Write-Host ""
Write-Host "[2/3] 開始編譯前端 (Vue 3 + Vite)..."
$FrontendSrcDir = Join-Path $RootDir "frontend\mes-spc-web"
Set-Location $FrontendSrcDir

Write-Host "執行 npm install..."
npm install

Write-Host "執行 npm run build..."
npm run build

Write-Host "複製前端檔案到 $FrontendPublishDir ..."
Copy-Item -Path "dist\*" -Destination $FrontendPublishDir -Recurse -Force
Write-Host "✅ 前端編譯與複製完成！" -ForegroundColor Green

Write-Host ""
Write-Host "[3/3] 開始發佈後端 API (.NET 10)..."
$BackendSrcDir = Join-Path $RootDir "backend\MesSpc.Api"
Set-Location $BackendSrcDir

Write-Host "執行 dotnet publish..."
dotnet publish -c Release -o $BackendPublishDir

Write-Host "✅ 後端發佈完成！" -ForegroundColor Green

# 發佈完成後重新啟動 App Pool
Start-AppPool

Write-Host ""
Write-Host "==================================================="
Write-Host " 🎉 發佈大功告成！" -ForegroundColor Cyan
Write-Host ""
Write-Host " IIS 前端站台請指向: $FrontendPublishDir" -ForegroundColor Yellow
Write-Host " IIS 後端應用請指向: $BackendPublishDir" -ForegroundColor Yellow
Write-Host "==================================================="

Set-Location $RootDir
Read-Host -Prompt "按 Enter 鍵結束"
