# SPC 頁面與讀取操作稽核

- 功能 ID：`SPC-AUDIT-1103-TASK-006`
- 日期：2026-11-09
- 狀態：進行中（僅測試站）
- 主系統：SPC API／Web；不修改 Portal、TransFiles、正式站、正式資料或既有業務量測。
- 前置：TASK-001～005 已完成測試階段；共用事件契約見 [event-contract-v1](../20261103-spc-user-activity-audit/event-contract-v1.md)。

## 目的

在不阻擋正常頁面、查詢或匯出的前提下，記錄已登入使用者成功進入的 canonical 頁面，以及實際完成的讀取、篩選與匯出操作。事件僅保存白名單摘要，不保存完整 URL、query、搜尋詞、lot、工單、下載內容或任何權杖。

## 範圍

- Router 成功導覽後，以 allowlist `PageKey` 回報 `navigation.page.enter`；redirect、alias、未登入重導與未知路由不記成功進頁。
- 經既有 axios wrapper 成功完成的受控 GET／匯出請求，記錄 `read.query.execute`、`read.filter.apply` 或 `read.export.generate`；僅傳送欄位名稱、可選 UTC 日期範圍、結果筆數與匯出格式。
- API 只接受固定 activity 類型與 allowlist PageKey，伺服器重新解析 JWT actor、session、route template、時間及環境；前端不得指定 actor、環境、EventId、HTTP／trace 或任意 DetailsJson。
- 一般活動採 best-effort：一次記錄失敗不影響既有頁面／查詢／下載；只在記憶體內有限重試，不持久化 payload。

非範圍：稽核查詢／匯出頁面與其最小權限（TASK-007）、保存／封存（TASK-008）、全站 E2E／效能與遮罩截圖（TASK-009）、Portal 帳密登入及正式發布。

## 需求與驗收

| 需求 | 驗收 | 可觀察條件 |
|---|---|---|
| R-001 | AC-001 | 成功導覽 `/spc`、動態趨勢與 alias 最終頁時，只保存 canonical PageKey／route template；登入頁、未登入重導與未知路由不生成成功頁面事件。 |
| R-002 | AC-002 | 成功讀取、帶篩選讀取與檔案匯出各記固定 read event；事件只含欄位名稱、日期範圍、筆數／格式，沒有篩選值、完整 URL 或內容。 |
| R-003 | AC-003 | API 拒絕未知 PageKey、未知 activity、偽造 actor／環境／原始 details；有效事件 actor、時間、環境與 route 皆由伺服器決定。 |
| R-004 | AC-004 | 同一頁面 `ClientEventId` 重送回同一事件；讀取事件不接受 client retry key。 |
| R-005 | AC-005 | 稽核 endpoint 暫時失敗時，既有 router／GET／下載仍完成；重試最多一次且不在 localStorage／sessionStorage 保存 payload。 |

```gherkin
Scenario: 成功頁面導覽留存 canonical 事件
  Given 已登入且具備頁面權限的使用者
  When Router 成功由 alias 導至 /spc
  Then 只保存 navigation.page.enter 與 analysis.spc.overall
  And 不保存 alias、完整 URL 或 query

Scenario: 篩選查詢不保存敏感值
  Given 使用者以 lot、工單與日期執行查詢
  When 既有 GET 請求成功回應
  Then 稽核事件僅含 FilterFields、可選日期範圍與 ResultCount
  And 不含 lot、工單或完整 query string

Scenario: 一般活動稽核不可用
  Given 活動稽核 endpoint 暫時失敗
  When 使用者完成頁面導覽或查詢
  Then 原頁與查詢結果仍可使用
  And 前端只有限重試且不持久化活動 payload
```

## 格式不符處理

- 前端不可傳任意 JSON、完整 URL 或未列入 allowlist 的 page／activity；API 回 `400` 並丟棄 payload。
- 篩選值改傳欄位名稱；日期只接受可解析的 UTC 區間，無法安全轉換時省略日期，不猜測本機時區。
- 成功結果筆數無法從回應安全取得時記為 `0`，不序列化整個 response。
