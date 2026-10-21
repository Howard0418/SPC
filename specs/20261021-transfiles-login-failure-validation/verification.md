# TASK-005失敗分類驗證
2026-10-21；自動驗證通過，EXE人工故障驗收待辦，不標DONE。
- 新增tools/transfiles-login-validation/test_login_gui_failures.py，TRANSFILES_SOURCE指向既有TransFiles來源。三個測試各3個subtest，覆蓋登入/取token/讀主檔的憑證、逾時、連線重設共9情境。
- 使用真實GUI處理方法與run_read_only_login_test，mock網路/dialog/thread/root/messagebox；排隊回呼於worker結束後執行，安全訊息/階段/不含合成secret/token/按鈕復原/失敗停止後續/未preview或confirm均通過。
- 相關test_etch_upload、test_connection_settings、test_tls_context、test_batch_convert_gui共56 passed。與新增3項合計59 passed、0 failed。未修改產品程式，未連線真實帳密/網路、未寫入DB或打包。
- EXE實際故障通知目視未做。使用者上一張成功畫面只完成TASK-003，不能替代三種故障類別驗收。不得要求亂輸密碼、停TLS或中斷正式/測試網站製造錯誤。
- 文件路徑/內容與diff檢查通過；部署/打包不適用，仍待可控EXE故障驗收。主要4件、TransFiles明列1件不變。
