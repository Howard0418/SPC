# 技術計畫
- 功能 ID：SPC-CHEM-CHART-STAGE-LABEL-1113-TASK-001-FIX-001
- 規格版本：2
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- SPC 前端：`frontend/mes-spc-web/src/utils/chemicalChart.js`，判斷是否採用藥液階段標籤。
- SPC 前端測試：`frontend/mes-spc-web/tests/chemical-single-line.test.mjs`。
- 不影響 API、資料庫、Portal 或 TransFiles。

## 實作方式與需求對應
- R-001：在既有主檔辨識外，加入「同一圖表資料中同日存在 `OPEN` 與 `CLOSE`」的受限回退條件。
- R-002：只以同一個日曆日的完整成對階段作為回退證據，不接受跨日、單一或 `GENERAL` 值。
- R-003：沿用既有單一序列與資料點，不調整資料排序或計算。
- R-004：`chartStageShiftLabel` 僅在 N1／N2 路徑將舊 `SamplingPhase=CLOSE` 視為早班收線；其他線別保留「收線待校正」。

## API、檔案格式及資料模型
無變更。

## 相容性與風險
- 風險：錯將其他線別誤標開收線。
- 控制：回退須為同日完整 `OPEN/CLOSE`，且既有 N1／N2 主檔路徑保持優先；新增一般線別與不完整階段回歸。

## 驗證安排
- 先新增紅燈前端測試，重現主檔缺失但 11/04 有 `OPEN/CLOSE` 的兩筆資料。
- 執行 `node --test tests/chemical-single-line.test.mjs`、前端測試與 production/test build。
- 發布測試站後驗證首頁、主要 JS/CSS MIME、`/api/health`、`/api/version` 與同源 API；使用者登入畫面確認 AC-001、AC-004。

## 發布、備份及回復
- 測試站：依既有 Web 發布程序部署前端；不改 API 或資料庫。
- 正式站：未授權，不發布。
- 回復：以測試站發布前前端備份還原；程式以本次提交還原。
