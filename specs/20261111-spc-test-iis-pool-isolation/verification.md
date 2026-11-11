# 驗證紀錄

- 功能 ID：`SPC-TEST-IIS-POOL-ISOLATION-1111-TASK-001`
- 環境：PC-PA16560048／Test
- 正式站：未涉及

| 驗收 ID | 驗證方式 | 結果 | 狀態 |
|---|---|---|---|
| AC-001 | IIS站台與集區指派查詢 | `SpcApiTest`→`SpcApiTest` pool；`SpcSingleTest`→`SpcSingleTest` pool，兩站皆 Started。 | 通過 |
| AC-002 | 新舊集區必要設定比對 | 新 API pool 以來源 pool 的受控設定建立：Integrated、64-bit、ApplicationPoolIdentity、OnDemand、20分鐘 idle、單一 worker；未共享。 | 通過 |
| AC-003 | `SPCTEST` Web／API健康檢查 | 2026-11-11：首頁 HTTP 200／`text/html`；`/api/health`、`/api/version` 均 HTTP 200，API 為 `0.1.125`、環境 `test`。 | 通過 |
| AC-004 | IIS備份與回復步驟檢核 | 原 IIS backup `spc-test-pool-isolation-20261111-r2` 已建立；首次錯誤路徑嘗試已自動回復，第二次成功後服務正常。 | 通過 |

## 後續測試發布阻擋

- `spc-chart-auto-refresh-test-20261111-r3` 的唯讀 Plan 已通過；Apply 在 `backup-database` 階段停止，證據狀態為 `failed-before-apply`，尚未套用檔案、migration 或資料庫寫入。
- 受控帳號可連到 `PMR_SPC_TEST`，但 `BACKUP DATABASE` 權限檢查為否，因此無法完成規定的 `COPY_ONLY`／`CHECKSUM`／`RESTORE VERIFYONLY`。不得略過備份繼續發布。
- 發布失敗後兩個測試 IIS pool 已回到 Started；首頁與 API health/version 均再次確認 200。需由 DBA 或既有具備測試庫備份權限的維護執行身分補足此權限後重試 T-004；正式站完全未涉及。
