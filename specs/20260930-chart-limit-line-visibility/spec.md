# 小型變更：管制圖規格線與管制線顯示穩定化
- 功能 ID：20260930-chart-limit-line-visibility
- 版本：1
- 狀態：完成
- 專案／授權依據／需求基準：SPC；使用者回報規格線與管制線有時顯示、有時不顯示，並同意修正；需求索引見 `docs/requirements.md`。

## 問題、預期行為與範圍
- 問題：分段規格線以 markLine segment 陣列表示，但 y 軸範圍計算只讀單一 markLine 的 `yAxis`，導致分段線可能未納入座標範圍。另管制線只要偵測到動態欄位即改畫動態 series，若資料不足可能看不到線。
- 預期：勾選規格界限或管制界限時，所有可用線值都穩定顯示；動態管制線資料不足時回退固定 UCL/CL/LCL。
- 範圍：僅 SPC 前端 `SpcChartView.vue` 圖表顯示；不改後端、不改資料庫。

## 需求與驗收
- R-001：y 軸範圍需納入分段規格線。
- AC-001：分段 USL/LSL/Target 超出資料點範圍時仍可顯示。
- R-002：動態管制線資料不足時需回退固定管制線。
- AC-002：勾選管制界限時，若有固定 UCL/CL/LCL，至少顯示固定線。

## 計畫與任務
- 受影響檔案：`frontend/mes-spc-web/src/views/SpcChartView.vue`
- [x] T-001：實作 R-001/R-002。
- [x] T-002：驗證 AC-001/AC-002。
- [x] T-003：同步有效需求與變更紀錄。

## 驗證
- 方式、環境：`npm run build`；使用者於測試站以分段規格與管制界限勾選驗收。
- 實際結果及證據：`npm run build` 通過。
- 發布狀態：已發布 SPC 測試站前端；備份 `frontend.backup-limit-line-visibility-20260930-141614`；首頁載入新資產 `index-BlUMHcJE.js`。
- 限制或未決問題：本機未直接連測試帳號重現使用者畫面。
