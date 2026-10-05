# 小型變更：趨勢圖直方圖與常態檢定
- 功能 ID：SPC-1002-TASK-006
- 版本：1
- 狀態：完成
- 專案／授權依據／需求基準：依 `docs/SPC 修正-1002.xlsx` 第 4 項「趨勢圖一樣增加管制圖的相關功能（ex: 直方圖），只是不要畫管制界線、不套用規則管理」；規格索引 `specs/20261005-spc-1002-workbook-plan/spec.md`。

## 問題、預期行為與範圍
- 問題：管制圖頁已有 raw measurements 直方圖、常態分布曲線與 P-value；趨勢圖頁目前只有時序線圖與統計卡，缺少分布檢視。
- 預期行為：趨勢圖頁在既有線圖下方顯示 raw data 直方圖、常態曲線與常態性檢定結果。
- 範圍：僅前端趨勢圖呈現；使用既有 `/v1/spc/chart` 回傳資料。不新增 API、不修改後端統計、不畫管制界線、不套用 OOC 規則管理。

## 需求與驗收
- R-001：趨勢圖載入資料後顯示量測值分布直方圖。
- R-002：若後端回傳 `normalCurve` 與 `normality`，趨勢圖顯示常態曲線、P-value、偏態、峰度與常態判定。
- R-003：直方圖只標示 LSL/USL/Target/Mean，不顯示 UCL/LCL/CL，不顯示規則管理結果。
- AC-001：前端 testhost build 通過。
- AC-002：測試站前端首頁與新版 JS 資產 HTTP 200。

## 計畫與任務
- 受影響檔案：`frontend/mes-spc-web/src/views/TrendChartView.vue`、`TODO.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`、本規格。
- [x] T-001：實作 R-001/R-003。
- [x] T-002：驗證 AC-001/AC-002。
- [x] T-003：同步變更紀錄並發布測試站前端。

## 驗證
- 方式、環境：`npm run build -- --mode testhost`，發布 SPC 測試站 frontend 後檢查首頁與新版 JS。
- 實際結果及證據：前端 testhost build 通過；`http://172.16.110.27:8083/` HTTP 200；新版 JS `/assets/index-DVVeXuQK.js` HTTP 200，`Content-Type=application/javascript`。
- 發布狀態：已發布 SPC 測試站 frontend；備份 `release/test/frontend.backup-trend-histogram-203651`。正式站未發布。
- 限制或未決問題：需由使用者登入測試站，用有資料的趨勢圖項目人工確認直方圖與 P-value 顯示。
