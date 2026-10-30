# 驗證紀錄

- T-001：已確認 `SpcChartView.vue` 原查詢區固定顯示 Anderson-Darling，但結果摘要未顯示方法名稱。
- T-002：已在結果摘要新增「常態檢定方法」欄位，優先顯示 API 回傳 `testName`／`method`，缺值顯示「未提供」。
- T-003：以 `data-testid="normality-method-result"` 保留畫面驗收定位；未新增後端或資料庫變更。
- T-004：`npm run build:test` 通過；SPC 測試前端已發布至 `release/test/frontend`，原檔備份為 `frontend.backup-normality-method-20261030`。
- T-005：正式站未發布；本次僅影響 SPC 測試前端。

## 限制

- 尚未執行瀏覽器真人驗收；需在測試站完成一次查詢確認實際 API 方法名稱與畫面位置。
