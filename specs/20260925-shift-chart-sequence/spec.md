# 小型變更：個別管制圖班別時間序列
- 功能 ID：20260925-shift-chart-sequence
- 版本：1
- 狀態：實作中
- 專案／授權依據／需求基準：使用者確認班別顯示與連線規則

## 問題、預期行為與範圍
個別管制圖目前依班別拆線，且將 N1/N2 的 CLOSE 顯示為晚班。改為依量測時間將同一管制項目的班別點連成單一序列；N1/N2 顯示早班開線、早班收線、中班，其他線別顯示早班、中班。

## 需求與驗收
- R-001：依量測時間排序並以單一線連接各班別點。
- R-002：N1/N2 的 OPEN/MIDDLE/CLOSE 分別顯示早班開線／中班／早班收線；其他線別 OPEN/MIDDLE 顯示早班／中班。
- AC-001：圖表不再將班別拆成多條線，且缺點不造成跨不存在資料點的連線。

## 計畫與任務
- 受影響檔案：`frontend/mes-spc-web/src/views/SpcChartView.vue`、`frontend/mes-spc-web/src/utils/chemicalChart.js`
- [x] T-001：調整班別標籤與時間序列繪圖。
- [x] T-002：執行前端建置與既有班別測試。
- [x] T-003：同步變更紀錄。

## 驗證
- 方式、環境：前端測試建置。
- 實際結果及證據：`node --test tests/chemical-single-line.test.mjs` 6/6 通過；`npm run build:test` 通過。
- 發布狀態：尚未發布測試站
- 限制或未決問題：無
