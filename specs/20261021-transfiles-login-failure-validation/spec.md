# TRANSFILES-API-CONNECTION-TASK-005 登入失敗GUI驗證
2026-10-22；狀態：自動化驗證通過，EXE 人工故障驗收依使用者指示暫緩。沿用TransFiles既有TASK-005，不新增登入或公式系統。
- 主系統TransFiles；驗證分類到GUI延後回呼，不連Portal/SPC、不寫DB、不改IIS或EXE。
- Given憑證/逾時/連線重設 When登入、取token或讀主檔任一步失敗 ThenGUI顯示正確階段及安全類別、按鈕恢復、不顯示合成秘密、不執行後續登入/匯入。
- 測試使用假root事件佇列及thread/messagebox/dialog mock，執行真實ConversionGUI.start_read_only_login_test與run_read_only_login_test；讓錯誤回呼在worker返回後才執行。測試不是實體EXE畫面驗收。
- Happy成功路徑沿用TASK-003使用者成功畫面；Boundary三個階段共9個情境；Invalid合成秘密不能流入通知/日誌；Regression按鈕復原及首個失敗停止後續呼叫。
- [x] 新GUI回呼3測試/9情境與既有56回歸，共59測試通過。
- [ ] 交付EXE故障類別人工驗收：暫緩（使用者確認無隔離驗收環境，2026-10-22）。
- 回復僅移除此新增驗證工具與同批文件，應用/資料不受影響。必要記錄見verification.md；不將mock結果當真實EXE驗收。

## 2026-10-22 狀態調整
- 使用者明確指定「自動化驗證通過，EXE 人工故障驗收暫緩」。保留59項自動化通過證據；不標完整驗收DONE，不列為程式待修或立即執行工作。
- 不為此建立隔離環境或製造故障；日後自然故障可依既有案例補記安全畫面/時間/日誌，或待使用者明確恢復驗收。
- 本次僅狀態文件更新，無產品/EXE/DB/站台變更；不重跑測試、build、打包或發布。
