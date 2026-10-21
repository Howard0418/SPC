# TransFiles 登入GUI失敗回呼驗證
在PowerShell設定環境變數 `TRANSFILES_SOURCE` 為既有TransFiles來源目錄，再執行 `python tools/transfiles-login-validation/test_login_gui_failures.py`。
使用既有Python相依，不安裝套件。測試只使用合成帳密、mock網路及延後回呼佇列，不開啟實體GUI或EXE、不發送HTTP或匯入資料。
3測試涵蓋9個階段/分類組合；可重現錯誤通知、安全遮蔽與按鈕復原。這不是EXE人工故障驗收，不據此標TASK-005 DONE。
