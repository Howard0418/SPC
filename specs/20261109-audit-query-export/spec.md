# SPC-AUDIT-1103-TASK-007 稽核查詢與匯出

## 目標與範圍

- 在 SPC 建立 `/audit-events` 頁面與唯讀 API，查閱 append-only `AuditEvents`。
- 支援 UTC 起訖（預設 7 日、最多 31 日）、人員、頁面、事件、目標與結果篩選，以及穩定游標分頁。
- Xlsx 匯出與畫面查閱採獨立明示權限；正式環境不發布、不異動。
- 不回傳 `SessionIdHash`、`AttemptedAccountHash`、`DeduplicationHash` 或 `DetailsJson`，也不修改既有事件。

## BDD 驗收

### 查閱與分頁

- Given 使用者目前仍具 `admin.audit.read` 明示權限
- When 查詢合法的 31 日內區間並套用篩選
- Then 系統依 `OccurredAtUtc, Id` 倒序回傳安全欄位、下一頁不重複，且先保存 `audit.events.query` 才回應資料。

### 最小權限與即時撤銷

- Given Editor、舊 JWT 或僅具匯出權限但資料庫現況未明示 `admin.audit.read`
- When 進入頁面或呼叫 API
- Then 導覽不顯示、前端阻擋且 API 回 403；拒絕事件由既有安全稽核保存。

### 匯出

- Given 使用者同時具 `admin.audit.read` 與 `admin.audit.export`
- When 匯出目前條件
- Then 產生 Xlsx 安全欄位，文字以文字儲存以避免公式注入，並先保存 `audit.events.export` 才回傳檔案。

### 邊界與故障

- Given 起訖超過 31 日、日期反轉、無效游標或匯出超量
- When 呼叫 API
- Then 回 400，不查詢或匯出資料。
- Given 查詢／匯出的稽核事件無法保存
- When 結果原本可產生
- Then 回 503 且不交付資料或檔案。

## 回歸風險

- 既有導覽、角色預設權限與一般查詢不得因此擴權。
- 不建立 schema migration，不改既有 `AuditEvents` 資料。
- rollback 為回復前一版 Web/API；既有稽核資料保留。
