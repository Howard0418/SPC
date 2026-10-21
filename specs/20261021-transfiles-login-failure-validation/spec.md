# TRANSFILES-API-CONNECTION-TASK-005 登入失敗GUI驗證
2026-10-21；狀態執行中。沿用TransFiles既有TASK-005，不新增登入或公式系統。
- 主系統TransFiles；驗證分類到GUI延後回呼，不連Portal/SPC、不寫DB、不改IIS或EXE。
- Given憑證/逾時/連線重設 When登入、取token或讀主檔任一步失敗 ThenGUI顯示正確階段及安全類別、按鈕恢復、不顯示合成秘密、不執行後續登入/匯入。
- 測試使用假root事件佇列及thread/messagebox/dialog mock，執行真實ConversionGUI.start_read_only_login_test與run_read_only_login_test；讓錯誤回呼在worker返回後才執行。測試不是實體EXE畫面驗收。
- Happy成功路徑沿用TASK-003使用者成功畫面；Boundary三個階段共9個情境；Invalid合成秘密不能流入通知/日誌；Regression按鈕復原及首個失敗停止後續呼叫。
- [x] 新GUI回呼3測試/9情境與既有56回歸，共59測試通過。
- [ ] 交付EXE故障類別人工驗收（無故障環境，不改伺服器、不亂輸密碼）。
- 回復僅移除此新增驗證工具與同批文件，應用/資料不受影響。必要記錄見verification.md；不將mock結果當真實EXE驗收。
