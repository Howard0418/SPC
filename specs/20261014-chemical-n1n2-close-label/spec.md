# 小型變更：N1/N2 藥液 CLOSE 顯示為早班收線
- 功能 ID：SPC-CHEM-N1N2-CLOSE-LABEL-20261014
- 版本：1
- 狀態：已完成
- 專案／授權依據／需求基準：SPC；使用者 2026-10-14 指示「OPEN表示早班開線，CLOSE是早班收線，為何會被歸成晚班？調整」；對應 `docs/requirements.md` 中 N1/N2 藥液開收線單線圖與舊班別相容需求。

## 問題、預期行為與範圍
N1/N2 藥液資料中的 `OPEN` / `CLOSE` 在開收線情境代表早班開線 / 早班收線，但 SPC 管制圖部分 tooltip 與點位明細仍使用舊班別顯示函式，導致 `CLOSE` 被顯示為晚班。

本次只修正 SPC 前端顯示與回歸測試，不修改後端資料、匯入資料、資料庫 schema、TransFiles 輸出格式或既有歷史資料。

## 需求與驗收
- R-001：N1/N2 藥液單線圖 tooltip 與點位明細遇到 `SamplingPhase=CLOSE` 時，需顯示「早班收線」，不得顯示「晚班」。
- R-002：一般班別顯示仍須保留舊相容規則，`OPEN=早班`、`MIDDLE=中班`、`CLOSE=晚班`。
- AC-001：前端單元測試需覆蓋 `shiftLabel` 與 `chartShiftLabel` 的差異。
- AC-002：管制圖渲染測試需覆蓋 N1/N2 tooltip 中 `CLOSE` 顯示為「早班收線」。

## 計畫與任務
- 受影響檔案：
  - `frontend/mes-spc-web/src/utils/chemicalChart.js`
  - `frontend/mes-spc-web/src/views/SpcChartView.vue`
  - `frontend/mes-spc-web/tests/chemical-single-line.test.mjs`
  - `docs/requirements.md`
  - `CHANGELOG_CUSTOM.md`
  - `ai_docs/10_change_log.md`
- [x] T-001：恢復一般班別 `shiftLabel` 語意，N1/N2 顯示改用 `chartShiftLabel`。
- [x] T-002：補前端回歸測試並執行相關測試與 build。
- [x] T-003：同步有效需求與變更紀錄，通過後發布 SPC 測試站 frontend。

## 驗證
- 方式、環境：
  - `node --test frontend/mes-spc-web/tests/chemical-single-line.test.mjs`
  - `npm run build`，工作目錄 `frontend/mes-spc-web`
- 實際結果及證據：
  - chemical single line test：7 passed。
  - frontend build：通過；保留既有 Vite chunk size warning。
- 發布狀態：已刷新 SPC 測試站 frontend，備份 `release/test/frontend.backup-20261014-chemical-n1n2-close-label`；PowerShell 對 `https://SPCTEST.pmr.com.tw/` 發生 SSL 連線失敗，需以瀏覽器或受信任憑證環境補做外部 smoke。正式站未發布。
- 限制或未決問題：本次不改 TransFiles 輸出欄位；若要從資料契約根治，後續可另開小工作將班別 `SamplingPhase` 與開收線 `SamplingStage` 完全分欄。

## v2 補修：N1/N2 mapping 結構判斷
- 問題：部分管制圖 mapping 直接提供 `machineCode` / `machineName`，不是巢狀 `machine.machineCode`，導致 N2 藥液圖仍走一般班別標籤並顯示「晚班」。
- 修改：`useSingleChemicalLine` 支援 `machine.machineCode`、`machine.machineName`、`machineCode`、`machineName`，並可辨識 `CHEMICAL` / `藥液` 範圍名稱。
- 驗證：`node --test frontend/mes-spc-web/tests/chemical-single-line.test.mjs` 7 passed；`npm run build` 通過，僅既有 Vite chunk size warning。
