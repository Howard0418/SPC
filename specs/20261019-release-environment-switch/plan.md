# 計畫
1. 測試先行，新增固定環境profile與環境比對。
2. 抽出共用 server engine，原正式入口維持Production；產包支援Environment。
3. 遠端入口固定選host，WinRM目前帳號；本機test以64位元PowerShell執行，遠端以伺服器64位元PowerShell執行。
4. Plan傳輸僅限私有incoming目錄；授權與ShouldProcess在任何Apply之前，遠端重驗SHA256。
5. 自動化測試／文件同步／提交上傳；工具變更不發布產品網站。
