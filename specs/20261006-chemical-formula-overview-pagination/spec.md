# 小型變更：藥液公式總覽分頁
- 功能 ID：20261006-chemical-formula-overview-pagination
- 版本：1
- 狀態：完成；測試站 frontend 已發布
- 專案／授權依據／需求基準：2026-10-06 使用者回報線別分析項目總覽頁面拉太長，要求加入分頁功能。

## 問題、預期行為與範圍
- 現況：`/chemical-analysis-overview` 公式完整預覽後，列高增加，全部項目一次顯示會讓頁面過長。
- 預期：前端以分頁顯示目前篩選結果，減少單頁長度；切換篩選條件時回到第 1 頁。
- 範圍：只調整 SPC 前端總覽頁分頁顯示；不改 API、不改資料、不改儲存與計算。匯出 Excel 仍匯出篩選後全部資料。

## 需求與驗收
- R-001：總覽表格每頁顯示固定筆數，提供上一頁/下一頁與頁碼資訊。
- R-002：線別、槽體、狀態篩選或點線別摘要卡後，分頁回到第 1 頁。
- R-003：批次儲存仍以全部 dirty rows 為準，不限目前頁。
- AC-001：前端 build 通過。
- AC-002：SPC 測試站 frontend 發布後，新版 JS/CSS 可載入。

## 計畫與任務
- 受影響檔案：
  - `frontend/mes-spc-web/src/views/ChemicalAnalysisOverviewView.vue`
  - `docs/requirements.md`
  - `CHANGELOG_CUSTOM.md`
- [x] T-001：新增分頁狀態、總頁數與目前頁資料。
- [x] T-002：篩選條件變更時回到第 1 頁。
- [x] T-003：新增分頁操作列。
- [x] T-004：前端 build。
- [x] T-005：同步需求索引與變更紀錄。
- [x] T-006：發布測試站 frontend。

## 驗證
- 方式、環境：`npm run build -- --mode testhost`
- 實際結果及證據：
  - 通過；產出 `index-BYjQytI2.js`、`index-B22nJqjf.css`。
  - 測試站首頁 `/` 回 200/text-html。
  - 新版 JS `/assets/index-BYjQytI2.js` 回 200/application-javascript。
  - 新版 CSS `/assets/index-B22nJqjf.css` 回 200/text-css。
  - 後端 `/api/version` 回 200，environment=test。
- 發布狀態：2026-10-06 已發布 SPC 測試站 frontend；備份 `release/test/frontend.backup-chemical-formula-pagination-20261006`；backend 未變更、未發布；正式站未發布。

## Rollback
- 還原本次前端檔案；若需回復測試站，使用 `D:\SPC\release\test\frontend.backup-chemical-formula-pagination-20261006` 還原 `D:\SPC\release\test\frontend`。
