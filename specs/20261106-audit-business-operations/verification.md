# 驗證紀錄

日期：2026-11-06；功能 `SPC-AUDIT-1103-TASK-005`。

## 已完成

- `BusinessDataAuditPolicyTests`：10 項通過，涵蓋量測、匯入、公式、F 表、主檔子資源固定事件碼，以及 preview 不被誤記。
- 既有 `AuditEventWriterTests`／`SecurityAuditOperationTests`：42 項通過；1 項隔離 SQL 測試因未提供專用管理設定略過。
- 測試 API 已發布到既有 `release/test/backend`，發布前備份為 `backend.backup-audit-business-20261106`；已核對設定僅為 `test`／`PMR_SPC_TEST`，並啟用 `Audit:BusinessDataEnabled=true`。
- 實際連線：`https://spctest.pmr.com.tw`、目前 JS／CSS 資產均 HTTP 200 且 MIME 分別為 `application/javascript`／`text/css`；`/api/health`、`/api/version` 均 HTTP 200、環境 `test`、版本 `0.1.109`。測試前端非 production 模式使用同源 `/api`。

## 待驗證

- 需由已登入的測試站使用者執行一筆既有的低風險測試操作，確認 `AuditEvents` 寫入 actor、固定事件碼、目標與結果，且不含原始量測／公式／F 值。為避免自行更動測試資料，本次未代操作。
- 尚未做正式發布、正式資料庫連線或正式資料寫入。

## 回復

將測試站 `Audit:BusinessDataEnabled` 設回 `false`，或以 `release/test/backend.backup-audit-business-20261106` 回復 API；既有 AuditEvents 保留、不刪除。
