# 小型變更：藥液公式總覽可讀性改善
- 功能 ID：20261006-chemical-formula-overview-readability
- 版本：1
- 狀態：完成；測試站 frontend 已發布
- 專案／授權依據／需求基準：2026-10-06 使用者確認藥液公式總覽頁公式欄位被摺疊不易閱讀，先做最小 UI 調整。

## 問題、預期行為與範圍
- 現況：`/chemical-analysis-overview` 的濃度公式、調整公式、調整量公式 textarea 預設 `rows=2`，長公式需要在欄位內捲動，不容易核對。
- 預期：公式欄位預設更高、使用等寬字體、可手動拉高，方便核對完整公式。
- 範圍：只調整 SPC 前端總覽頁 UI；不改 API、不改公式內容、不改儲存與計算邏輯。

## 需求與驗收
- R-001：公式欄位預設高度需增加，減少長公式被摺疊。
- R-002：公式欄位使用等寬字體並保留可垂直調整。
- AC-001：前端 build 通過。
- AC-002：SPC 測試站前端發布後，`/chemical-analysis-overview` 可載入新版 JS/CSS。

## 計畫與任務
- 受影響檔案：
  - `frontend/mes-spc-web/src/views/ChemicalAnalysisOverviewView.vue`
  - `docs/requirements.md`
  - `CHANGELOG_CUSTOM.md`
- [x] T-001：將三個公式 textarea 預設 rows 由 2 調整為 5。
- [x] T-002：調整 `.formula-field` 樣式為等寬字體、較高最小高度、保留垂直 resize。
- [x] T-003：前端 build。
- [x] T-004：同步需求索引與變更紀錄。
- [x] T-005：發布 SPC 測試站 frontend。

## 驗證
- 方式、環境：`npm run build -- --mode testhost`
- 實際結果及證據：
  - 通過；產出 `index-tRINLkRd.js`、`index-BXboKn_D.css`。
  - 測試站首頁 `/` 回 200/text-html。
  - 新版 JS `/assets/index-tRINLkRd.js` 回 200/application-javascript。
  - 新版 CSS `/assets/index-BXboKn_D.css` 回 200/text-css。
  - 後端 `/api/version` 回 200，environment=test。
- 發布狀態：2026-10-06 已發布 SPC 測試站 frontend；備份 `release/test/frontend.backup-chemical-formula-readability-20261006`；backend 未變更、未發布；正式站未發布。

## Rollback
- 還原本次前端檔案；若需回復測試站，使用 `D:\SPC\release\test\frontend.backup-chemical-formula-readability-20261006` 還原 `D:\SPC\release\test\frontend`。
