# N1／N2 藥液管制圖開收線辨識

- 功能 ID：`SPC-CHEM-CHART-STAGE-LABEL-1113-TASK-001`
- 主系統：SPC 前端；API／資料庫／Portal 不變。
- 狀態：測試站已完成；正式站未授權，登入後畫面待人工驗收。

## 需求

- R-001：管制圖點位已保存 `SamplingStage=OPEN/CLOSE` 時，X 軸與 tooltip 必須分辨開線、收線；即使載入的主檔投影缺少機台代碼，也不得把同日兩點都顯示成「早班」。
- R-002：保留 N1／N2 藥液既有單一時間序列、點位順序、MR、管制線、能力指標、規格及原始量測；本件不拆成兩套統計母體。
- R-003：一般線別 `GENERAL` 與沒有開收線階段的圖表維持既有顯示。

## BDD 驗收

- AC-001：Given 同一報表日有早班開線與早班收線兩點，When 顯示管制圖，Then X 軸分別顯示 `MM/DD 早(開)`、`MM/DD 早(收)`，tooltip 顯示對應取樣階段。
- AC-002：Given 主檔投影未提供足以判斷 N1/N2 的機台欄位，但點位明確保存 OPEN/CLOSE，When 顯示圖表，Then 仍依點位階段辨識，不退回兩個相同「早班」。
- AC-003：Given 一般線別只有 GENERAL 階段，When 顯示圖表，Then 原班別與既有統計、點數不變。

## 測試風險

- Happy Path：同日 OPEN／CLOSE 正確標示。
- Boundary：主檔缺機台代碼但點位階段完整。
- Invalid Input：空白／未知階段不冒充開線或收線。
- Regression：不改 API、資料、排序及任何統計公式。

