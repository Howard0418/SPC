$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==================================================="
Write-Host "   MES-SPC 系統一鍵發佈腳本 (One-Click Publish)"
Write-Host "==================================================="
Write-Host ""

$RootDir            = $PSScriptRoot
$PublishDir         = Join-Path $RootDir "publish"
$FrontendPublishDir = Join-Path $PublishDir "frontend"
$BackendPublishDir  = Join-Path $PublishDir "backend"
$BackendExe         = Join-Path $BackendPublishDir "MesSpc.Api.exe"
$BackendPort        = 8082

# ── 用 netstat + taskkill 停止佔用 Port 的 Process（不需要 IIS / 特殊模組）──
function Stop-BackendProcess {
    Write-Host "  🔍 檢查 Port $BackendPort 是否有 Process 佔用..."
    $netstatOutput = cmd /c "netstat -aon 2>nul | findstr LISTENING | findstr :$BackendPort"
    if ($netstatOutput) {
        foreach ($line in $netstatOutput) {
            $parts = $line.Trim() -split '\s+'
            $procId = $parts[-1]
            if ($procId -match '^\d+$' -and $procId -ne '0') {
                Write-Host "  ⏹  停止 PID $procId（佔用 Port $BackendPort）..."
                cmd /c "taskkill /F /PID $procId 2>nul" | Out-Null
            }
        }
        Start-Sleep -Seconds 2
        Write-Host "  ✅ Process 已停止" -ForegroundColor Green
    } else {
        Write-Host "  ℹ  Port $BackendPort 無佔用，繼續..." -ForegroundColor DarkGray
    }
}

# ── 刪除目錄（優先 PowerShell，失敗則 fallback 至 cmd rmdir）──
function Remove-DirSafe ($Path) {
    if (-not (Test-Path $Path)) { return }
    try {
        Remove-Item -Path $Path -Recurse -Force -ErrorAction Stop
        Write-Host "  ✅ 已清除目錄：$Path" -ForegroundColor DarkGray
    } catch {
        Write-Host "  ⚠  PowerShell 刪除失敗，改用 cmd rmdir..." -ForegroundColor Yellow
        cmd /c "rmdir /s /q `"$Path`""
        if (Test-Path $Path) {
            Write-Host "" 
            Write-Host "  ❌ 無法刪除目錄，可能仍有 Process 鎖定 DLL。" -ForegroundColor Red
            Write-Host "     請手動關閉後端服務後再執行本腳本。" -ForegroundColor Red
            exit 1
        }
        Write-Host "  ✅ 已清除目錄（cmd rmdir）：$Path" -ForegroundColor DarkGray
    }
}

# ── 啟動後端 exe（最小化視窗，背景執行）──
function Start-BackendService {
    if (-not (Test-Path $BackendExe)) {
        Write-Host "  ⚠  找不到後端執行檔：$BackendExe" -ForegroundColor Yellow
        return
    }
    Write-Host "  ▶  啟動後端服務..."
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName        = $BackendExe
    $startInfo.WorkingDirectory = $BackendPublishDir
    $startInfo.UseShellExecute  = $true
    $startInfo.WindowStyle      = [System.Diagnostics.ProcessWindowStyle]::Minimized
    [System.Diagnostics.Process]::Start($startInfo) | Out-Null
    Start-Sleep -Seconds 3
    Write-Host "  ✅ 後端服務已啟動，監聽 http://0.0.0.0:$BackendPort" -ForegroundColor Green
}

# ===========================================
Write-Host "[1/3] 準備發佈資料夾..."
Remove-DirSafe $FrontendPublishDir

Stop-BackendProcess
Remove-DirSafe $BackendPublishDir

New-Item -ItemType Directory -Force -Path $FrontendPublishDir | Out-Null
New-Item -ItemType Directory -Force -Path $BackendPublishDir  | Out-Null

Write-Host ""
Write-Host "[2/3] 開始編譯前端 (Vue 3 + Vite)..."
$FrontendSrcDir = Join-Path $RootDir "frontend\mes-spc-web"
Set-Location $FrontendSrcDir

Write-Host "自動遞增前端版本號 (Patch)..."
try {
    npm.cmd version patch --no-git-tag-version
} catch {
    npm version patch --no-git-tag-version
}

Write-Host "執行 npm install..."
npm install

Write-Host "執行 npm run build..."
npm run build

Write-Host "複製前端檔案到 $FrontendPublishDir ..."
Copy-Item -Path "dist\*" -Destination $FrontendPublishDir -Recurse -Force
Write-Host "✅ 前端編譯完成！" -ForegroundColor Green

Write-Host ""
Write-Host "[3/3] 開始發佈後端 API (.NET 10)..."
$BackendSrcDir = Join-Path $RootDir "backend\MesSpc.Api"
Set-Location $BackendSrcDir

Write-Host "執行 dotnet publish..."
dotnet publish -c Release -o $BackendPublishDir
Write-Host "✅ 後端發佈完成！" -ForegroundColor Green

Start-BackendService

Write-Host ""
Write-Host "==================================================="
Write-Host " 🎉 發佈大功告成！" -ForegroundColor Cyan
Write-Host ""
Write-Host " 前端目錄 : $FrontendPublishDir" -ForegroundColor Yellow
Write-Host " 後端目錄 : $BackendPublishDir"  -ForegroundColor Yellow
Write-Host " 後端 API : http://$(hostname):${BackendPort}/api" -ForegroundColor Cyan
Write-Host "==================================================="

Set-Location $RootDir
Read-Host -Prompt "按 Enter 鍵結束"
