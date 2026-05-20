$ProjectPath = "C:\Users\ihao_ting.PMR.000\Desktop\MES"
Set-Location $ProjectPath
[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " 🚀 PMR MES/SPC - AI Smart DevOps Watcher (Gemini CLI)" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "正在監控專案檔案異動... 預設每 10 分鐘進行一次自動檢查與智能 Commit。" -ForegroundColor DarkGray

while ($true) {
    Start-Sleep -Seconds 600 # 10 minutes interval

    # 檢查是否有未提交的變更
    $status = git status --porcelain
    if ($status) {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
        Write-Host "`n[$timestamp] 偵測到檔案變更，啟動 Gemini CLI 智能分析..." -ForegroundColor Yellow
        
        # 將所有變更暫存
        git add .

        # 呼叫 Gemini CLI 自動分析與提交
        $prompt = "你是一位精準的資深工程師。請先執行 git status 查看暫存區檔案，接著檢查 git diff --cached 的變更內容。請根據 Conventional Commits 規範 (例如 feat, fix, refactor, style, docs) 撰寫一句精確、專業且切合修改內容的英文單行 commit message，並直接執行 git commit -m 'your message' 完成提交。請確保只執行 commit 動作並回報 commit hash。"
        
        Write-Host "⚡ 正在請求 Gemini 模型進行代碼分析與 Commit..." -ForegroundColor DarkGray
        npx -y @google/gemini-cli "$prompt"

        Write-Host "✅ 智能提交迴圈執行完畢！" -ForegroundColor Green
    }
}
