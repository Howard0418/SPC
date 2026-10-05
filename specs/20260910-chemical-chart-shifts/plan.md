# 技術計畫
- 功能 ID：20260910-chemical-chart-shifts
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- SPC Web：`frontend/mes-spc-web/src/views/SpcChartView.vue`。

## 實作方式
- 以 `OPEN`、`MIDDLE`、`CLOSE` 建立日期鍵值對照，固定產生早班、中班、晚班三個獨立 series。
- 共用資料點建立函式依班別指定顏色與 symbol；Tooltip 以班別對照表呈現中文。
- `GENERAL` 舊資料維持原有單序列流程。

## 相容性與風險
- 不變更 API、資料表或運算結果。
- 資料只有早班或只有中班時，也以已存在班別序列呈現；缺少班別不補值。

## 驗證與發布
- `npm run build:test` 與前端診斷。
- 測試站查詢含早中晚班的藥液項目，確認標記與 Tooltip；若無可用三班資料，記錄為待 UAT。
- 備份並只發布 `SpcWeb` 測試站，正式環境不發布。
