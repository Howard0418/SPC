# 高風險資料操作稽核

- 功能 ID：`SPC-AUDIT-1103-TASK-005`
- 日期：2026-11-06
- 狀態：進行中
- 主系統：SPC API；不修改 Portal、TransFiles、正式資料或既有業務資料。

## 目的

在既有 append-only `AuditEvents` 基礎上，為量測、受控匯入、藥液公式／F 版本與主檔停用／刪除等高風險寫入建立伺服器端稽核。操作者僅由已驗證 JWT 解析；事件只保存固定代碼、目標與欄位摘要，絕不保存 Excel 內容、公式原文、F 值、量測值、帳密或權杖。

## 需求與驗收

- R-001：受納管寫入成功時，資料與 `Succeeded` 事件同一交易完成。
- R-002：受納管寫入失敗、回傳 4xx／5xx 或稽核寫入失敗時，不得留下 `Succeeded` 事件；若已開始交易，業務修改一併回復。
- R-003：量測／匯入／設定／主檔操作以固定事件碼、路由目標及白名單欄位摘要保存；不可相信 request 的操作者欄位。
- R-004：功能旗標關閉時，既有 API 行為不變；旗標開啟但身分或稽核資料不合格時 fail closed。

```gherkin
Scenario: 已驗證使用者成功建立量測
  Given BusinessData 稽核已啟用且使用者 JWT 有效
  When 使用者呼叫受納管的量測寫入 API 並成功
  Then 業務資料與 measurement.record.create 事件在同一交易提交
  And 事件的 actor 取自 JWT 而非輸入表單

Scenario: 稽核追加失敗
  Given 受納管寫入已開始資料庫交易
  When AuditEvents 追加失敗
  Then API 回覆 503
  And 業務資料與 Succeeded 稽核事件皆不保留
```

## 範圍

納管目前已辨識的 API 寫入：量測批次／手動量測、上傳確認與刪除、咬蝕確認、藥液排程發布，以及主檔 create/update/deactivate/delete。僅在 `Audit:BusinessDataEnabled=true` 作用。

非範圍：前端進頁、查詢／匯出、稽核查閱、保存／封存；分別屬 TASK-006～008。既有正式資料不回填稽核事件。
