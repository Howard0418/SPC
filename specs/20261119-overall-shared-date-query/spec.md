# Overall 與主查詢共用日期

- 工作代號：`SPC-OVERALL-SHARED-DATE-1119-TASK-001`
- 主系統：SPC Web
- 資料庫／API：無變更
- 發布：測試區自動發布；正式另行授權

## Requirement

即時互動管制圖頁面只保留主查詢區的一組起訖日期。Overall 區塊移除自己的日期輸入及查詢按鈕，初次載入與按下主查詢按鈕時，皆使用主查詢區日期讀取 Overall。

## BDD

- Given 使用者進入 `/spc`，When 頁面初次載入，Then Overall 使用主查詢區預設起訖日。
- Given 使用者修改主查詢區起訖日，When 按下主查詢，Then 明細與 Overall 使用相同日期。
- Given 使用者切換線別或維度，When查詢，Then Overall仍為所有啟用管制項目的全站統計，不套線別／維度條件。
- Given 日期無效或超過允許範圍，When 按下主查詢，Then 不送出 Overall 請求並顯示既有日期錯誤。

## 風險與回復

- 回歸：Overall 初次載入、主查詢、日期驗證、行動版寬度。
- 回復：回復前一版 Web；無資料回復需求。
