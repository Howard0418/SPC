# SPC 管制分析 API 回應格式防護

## 目的

修正正式 SPC 管制分析頁在主檔 API 回傳非陣列資料時，因直接呼叫 `.filter()` 而造成整頁空白的問題。

## 範圍

- `SpcChartView.vue` 的三個初始化主檔回應：管制項目、管制圖類型、管制圖群組。
- 支援陣列及常見 `items`／`data`／`results` 包裝；無法辨識時顯示可理解的載入錯誤。
- 不修改後端、資料庫或既有資料。

## 驗收（BDD）

### Scenario: API 回傳陣列

Given 三個主檔 API 回傳陣列
When 管制分析頁初始化
Then 頁面可正常建立篩選選項，且不發生 `filter is not a function`。

### Scenario: API 回傳包裝陣列

Given 主檔 API 回傳 `{ items: [...] }`、`{ data: [...] }` 或 `{ results: [...] }`
When 管制分析頁初始化
Then 前端取出內含陣列並正常建立篩選選項。

### Scenario: API 回傳無法辨識格式

Given 主檔 API 回傳非陣列且沒有支援的包裝欄位
When 管制分析頁初始化
Then 頁面保留可操作外框並顯示載入錯誤，不因 `.filter()` 例外而白屏。
