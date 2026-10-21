# TRANSFILES-API-CONNECTION-TASK-003 交付續驗
2026-10-21；狀態待人工驗收，非DONE。使用者要求下個工作；主系統TransFiles，不改網站、DB、IIS、憑證或登入邏輯。
- Given舊操作指南指向已清理目錄 When核對現存交付 Then更新至發布版_20261020_日期公式DB測試/BatchConvertTool.exe，不重建重複包。
- 先讀TransFiles AGENTS/需求/變更與本規格前案；既有TASK-003規格維持不擴張。
- 現有EXE SHA256=EB4777FF2C2B90755622FA9FAFC8403ADE035D1121393B4C21FA8D860F515139，與TASK-005既有交付紀錄一致；首筆LOG版hash亦匹配。沒有啟動/重打包/刪除程式。
- 相關connection_settings/TLS測試12 passed，使用mock/TemporaryDirectory，無網路帳密或DB匯入。程式核對connection_test.run_read_only_login_test與batch_convert_gui.start_read_only_login_test不呼叫preview/confirm。
- Happy正常登入按鈕仍待使用者；Boundary現包身份/URL保存；Invalid非法URL與受控根憑證由12測試涵蓋；Regression預覽與確認不混同登入。
- 已修正TransFiles docs/api-login-test-guide.md並同步verification/需求/變更。該目錄非Git，本摘要在SPC版控。
- 尚缺GUI按鈕成功訊息及EXE版本，不能以先前130筆預覽代替。TASK-005錯誤分類獨立保留，本次未核銷。
- 文件內容/連結檢查通過，build/部署/打包不適用；回復僅同批文件。主要4件、TransFiles明列2件均不變。
