# 驗證紀錄

日期：2026-11-06；2026-11-09 完成實際操作驗收；功能 `SPC-AUDIT-1103-TASK-005`。

## 已完成

- `BusinessDataAuditPolicyTests`：10 項通過，涵蓋量測、匯入、公式、F 表、主檔子資源固定事件碼，以及 preview 不被誤記。
- 既有 `AuditEventWriterTests`／`SecurityAuditOperationTests`：42 項通過；1 項隔離 SQL 測試因未提供專用管理設定略過。
- 測試 API 已發布到既有 `release/test/backend`，發布前備份為 `backend.backup-audit-business-20261106`；已核對設定僅為 `test`／`PMR_SPC_TEST`，並啟用 `Audit:BusinessDataEnabled=true`。
- 實際連線：`https://spctest.pmr.com.tw`、目前 JS／CSS 資產均 HTTP 200 且 MIME 分別為 `application/javascript`／`text/css`；`/api/health`、`/api/version` 均 HTTP 200、環境 `test`、版本 `0.1.109`。測試前端非 production 模式使用同源 `/api`。
- 2026-11-09 已登入使用者於測試站「使用者管理／產品料號主檔」實際新增後刪除一筆測試料號；新增事件 `AuditEvents.Id=85`（台北時間 08:10:21，`masterdata.record.create`，POST 200），刪除事件 `Id=86`（08:10:29，`masterdata.record.delete`，DELETE 204），兩筆皆為 `Succeeded`。
- 兩筆 actor 均由伺服器解析為 Operator 27、帳號 `ihao_ting`、顯示名稱「丁義豪」，PageKey 均為 `master.parts`；目標只保存 `Parts`、`Create`／`Delete:117` 與 ChangedFields 安全摘要。
- 刪除後查核 `RemainingTestParts=0`，兩筆稽核仍保留（`PreservedAuditEvents=2`）；在事件欄位與 DetailsJson 搜尋原始業務值結果為 `RawBusinessValueLeaks=0`。符合資料刪除、稽核留存與不保存原始料號內容的驗收條件。

## 結論

- TASK-005 測試階段 DONE；實際新增／刪除、actor、固定事件碼、結果、刪除後留存與敏感內容排除均已核對。
- 尚未做正式發布、正式資料庫連線或正式資料寫入。

## 回復

將測試站 `Audit:BusinessDataEnabled` 設回 `false`，或以 `release/test/backend.backup-audit-business-20261106` 回復 API；既有 AuditEvents 保留、不刪除。
