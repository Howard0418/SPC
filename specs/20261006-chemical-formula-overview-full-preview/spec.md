# 小型變更：藥液公式總覽完整預覽
- 功能 ID：20261006-chemical-formula-overview-full-preview
- 版本：1
- 狀態：完成；測試站 frontend 已發布
- 專案／授權依據／需求基準：2026-10-06 使用者確認希望 `/chemical-analysis-overview` 三個公式一眼就能看到，不要摺疊或隱藏。

## 問題、預期行為與範圍
- 現況：公式欄位以 textarea 顯示，即使加高後，長公式仍可能需要在欄位內捲動或視覺上像被收納。
- 預期：預設以完整換行的公式預覽顯示濃度公式、調整公式與調整量公式；按「編輯」後才切換為 textarea，保留既有批次儲存能力。
- 範圍：只調整 SPC 前端總覽頁 UI；不改 API、不改公式內容、不改儲存與計算。

## 需求與驗收
- R-001：三個公式欄位預設完整換行顯示，不使用 textarea 捲動呈現。
- R-002：使用者仍可按「編輯」進入 textarea 修改，既有 dirty 判斷、還原與批次儲存維持。
- AC-001：前端 build 通過。
- AC-002：SPC 測試站 frontend 發布後，新版 JS/CSS 可載入。

## 計畫與任務
- 受影響檔案：
  - `frontend/mes-spc-web/src/views/ChemicalAnalysisOverviewView.vue`
  - `docs/requirements.md`
  - `CHANGELOG_CUSTOM.md`
- [x] T-001：新增列層級公式編輯狀態。
- [x] T-002：公式欄位預設改為完整換行預覽，編輯狀態才顯示 textarea。
- [x] T-003：前端 build。
- [x] T-004：同步需求索引與變更紀錄。
- [x] T-005：發布測試站 frontend。

## 驗證
- 方式、環境：`npm run build -- --mode testhost`
- 實際結果及證據：
  - 通過；產出 `index-BS8crdCY.js`、`index-CC7a4yWE.css`。
  - 測試站首頁 `/` 回 200/text-html。
  - 新版 JS `/assets/index-BS8crdCY.js` 回 200/application-javascript。
  - 新版 CSS `/assets/index-CC7a4yWE.css` 回 200/text-css。
  - 後端 `/api/version` 回 200，environment=test。
- 發布狀態：2026-10-06 已發布 SPC 測試站 frontend；備份 `release/test/frontend.backup-chemical-formula-full-preview-20261006`；backend 未變更、未發布；正式站未發布。

## Rollback
- 還原本次前端檔案；若需回復測試站，使用 `D:\SPC\release\test\frontend.backup-chemical-formula-full-preview-20261006` 還原 `D:\SPC\release\test\frontend`。
