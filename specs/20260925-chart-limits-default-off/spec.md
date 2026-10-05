# 小型變更：管制圖界限預設關閉
- 功能 ID：20260925-chart-limits-default-off
- 版本：1
- 狀態：實作中
- 專案／授權依據／需求基準：使用者確認

## 問題、預期行為與範圍
管制圖頁面首次載入時不應預設顯示規格界限或管制界限；使用者仍可手動勾選顯示。

## 需求與驗收
- R-001：`showSpecLimits` 與 `showControlLimits` 初始值均為 false。
- AC-001：頁面初始不繪製兩類界限，勾選後仍可繪製。

## 計畫與任務
- 受影響檔案：`frontend/mes-spc-web/src/views/SpcChartView.vue`
- [x] T-001：調整初始狀態。
- [x] T-002：執行測試與建置。

## 驗證
- 實際結果及證據：班別測試 6/6 通過；`npm run build:test` 通過。
- 發布狀態：已發布 SPC 測試站
