# PORTAL-CHEM-ADJUST-PARAMETERS-1118-TASK-001 驗證

- SPC 核心測試：`UpdateChemicalAdjustmentParameters` 2/2 通過，覆蓋保留公式／未知鍵／規格及非正值拒絕。
- SPC API Release 前建置：0 warning / 0 error。
- Portal API 測試：288/288 通過；Portal Web 契約測試：47/47 通過。
- Portal API／Web 建置：均 0 warning / 0 error。
- 發布：使用者明確授權夾帶工作區現有修改後，已發布 Portal 測試站 API `1.0.261`／Web `1.0.299` 與 SPC 測試站 API `0.1.135`／Web `0.1.141`。
- 連線：Portal Login、SPC 首頁及實際 JS／CSS 均 HTTP 200 且 MIME 正確；Portal API health 為 `database=ok`、`dbEnvironment=test`，SPC health/version 為 `0.1.135`、`test`；Portal 同源匿名 API 回 401，符合授權保護。
- 回復：SPC 測試站備份 `D:\SPC\release\backup-adjust-parameters-test-20261118-195656`；Portal 由標準發布腳本保留發布前檔案策略。
- 人工驗收：待使用者於測試區確認新增／修改及重載結果。
- 正式：未授權、未發布。
