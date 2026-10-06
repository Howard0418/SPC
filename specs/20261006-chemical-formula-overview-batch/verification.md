# 藥液公式總覽與批次儲存頁驗證紀錄

## 2026-10-06 TASK-001/TASK-002 規格與最小方案

- 狀態：完成。
- 本階段只新增規格與計畫文件，未修改產品程式、未建置、未發布測試站。

### 已確認現況

- `ChemicalAnalysisOverviewView.vue` 已可顯示 CHEM 項目、篩選、匯出 Excel、導向單筆編輯。
- 總覽頁目前不能直接編輯公式或批次儲存。
- 單筆 `PUT /part-process-characteristics/{id}` 已整合 `ChemicalAnalysisFormulaVersionService`。
- 最小方案可先由前端逐筆呼叫既有 PUT，確保批次儲存也會留下版本紀錄。

### 待後續驗證

- 修改 1 筆公式後能儲存並新增版本紀錄。
- 修改多筆公式後只送出異動列，且每筆各自新增版本紀錄。
- 未修改時不送出。
- 前端 build 與測試站 smoke。

## 2026-10-06 TASK-003 前端 draft 與異動標示

- 狀態：完成。
- 修改：`ChemicalAnalysisOverviewView.vue` 新增每列公式 draft、dirty row 判斷、已變更標籤、列底色提示與單列還原。
- 修改：總覽表格可直接編輯濃度公式、調整公式、調整量公式與小數位；本階段不送出儲存 API。
- 修改：匯出 Excel 補上調整公式、調整量公式、小數位與是否變更欄位。

### 驗證

- `npm run build -- --mode testhost`：通過，產出 `index-CodgJfD4.js`、`index-CWZtvWtJ.css`。
- 本階段未發布測試站；正式站未發布。

## 2026-10-06 TASK-004 批次儲存確認與異動列送出

- 狀態：完成。
- 修改：藥液總覽頁新增「儲存變更」按鈕，按鈕顯示目前異動筆數，無異動時不可送出。
- 修改：儲存前以確認視窗列出最多 8 筆即將儲存的線別/槽位/分析項目摘要。
- 修改：儲存時只針對 dirty rows 逐筆呼叫既有 `PUT /part-process-characteristics/{id}`，payload 保留原主檔欄位，只替換 `chemicalAnalysisConfigJson`。
- 範圍：未新增後端 API；詳細成功/失敗列結果顯示留待 TASK-005。

### 驗證

- `npm run build -- --mode testhost`：通過，產出 `index-KMWpI8dd.js`、`index-pAvY4q_l.css`。
- 本階段未發布測試站；正式站未發布。

## 2026-10-06 TASK-005 儲存結果列與重新載入

- 狀態：完成。
- 修改：批次儲存後顯示成功/失敗筆數摘要。
- 修改：表格新增「儲存結果」欄，成功列顯示「已儲存」，失敗列顯示錯誤訊息並以紅底提示。
- 修改：儲存完成後重新載入資料；失敗列保留原草稿，方便修正後再次送出。
- 範圍：未新增後端 API；仍沿用既有單筆 PUT 與公式版本紀錄。

### 驗證

- `npm run build -- --mode testhost`：通過，產出 `index-7pDwhRVl.js`、`index-B3nZcc2H.css`。
- 本階段未發布測試站；正式站未發布。

## 2026-10-06 TASK-006 前端 build 與靜態檢查

- 狀態：完成。
- 本階段未修改產品程式，只做前端總驗證與靜態檢查。

### 驗證

- `npm run build -- --mode testhost`：通過，產出 `index-7pDwhRVl.js`、`index-B3nZcc2H.css`。
- 靜態檢查確認 `ChemicalAnalysisOverviewView.vue` 包含：
  - `formulaDrafts` 草稿資料。
  - `dirtyRows` 異動列判斷。
  - `saveChanges` 批次儲存流程。
  - `PUT /part-process-characteristics/${row.id}` 呼叫。
  - `saveResults` 與「儲存結果」欄。
- 本階段未發布測試站；正式站未發布。

## 2026-10-06 TASK-007 測試站 frontend 發布與 smoke test

- 狀態：完成。
- 發布：SPC 測試站 frontend 已發布；backend 未發布；正式站未發布。
- 備份：`release/test/frontend.backup-chemical-formula-overview-batch-20261006-142452`。

### 驗證

- Smoke：`http://172.16.110.27:8083/` 回 200，`text/html`。
- Smoke：新版 JS `/assets/index-7pDwhRVl.js` 回 200，`application/javascript`。
- Smoke：新版 CSS `/assets/index-B3nZcc2H.css` 回 200，`text/css`。
- Smoke：`http://172.16.110.27:8081/api/version` 回 200，`environment=test`。
- 備註：第一次 smoke 腳本使用 `$home` 變數時碰到 PowerShell `$HOME` 唯讀變數，修正測試腳本變數名後重跑通過；不是網站錯誤。

## 2026-10-06 TASK-008 文件同步與收尾

- 狀態：完成。
- 同步：`CHANGELOG_CUSTOM.md`、`TODO.md`、`docs/requirements.md` 與本規格任務/驗證紀錄。
- 結論：藥液公式總覽與批次儲存頁已完成測試站 frontend 發布；backend 未變更、未發布；正式站未發布。
