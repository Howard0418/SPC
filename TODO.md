# SPC 全系統未完成小工作池

狀態：僅顯示未完成、待規格、待執行、待確認小工作。已完成小工作不再保留於本檔，完成紀錄請查 `CHANGELOG_CUSTOM.md` 與對應 `specs/`。

## 執行規則

- 每次只執行一個 TASK。
- 開始前先確認該 TASK 的範圍與驗收條件。
- 依優先順序執行；阻擋正式/測試使用的錯誤 > 安全與權限隔離 > 資料正確性與可回復 > 使用者高頻操作 > 文件/文案 > 架構整理。
- 同時碰登入/權限、發布/IIS、資料庫 migration、公式計算或公告權限的小工作不可併行；先做範圍小且可獨立驗證者。
- 測試通過後只發布 SPC 測試站；正式站需另行授權。
- 未完成小工作須有對應 Spec/BDD；完成後同步需求索引與變更紀錄，並自本檔移除。
- 未經確認不提前執行下一個 TASK。

## 目前小工作排序（2026-10-07）

### 第一順位：單一 IIS Site（環境與發布）

#### IIS-TASK-003～IIS-TASK-008：沿用 `specs/20261005-single-iis-site/tasks.md`

狀態：已完成 TASK-001～TASK-005；TASK-006 已建立 HTTP 測試單站；HTTPS binding/cert 為外部環境阻擋，待提供後才能完成 AC-007，不標示 DONE

待執行：

- T-006：待測試站 HTTPS binding/cert 可用後補驗 mixed content；不發布正式站。
- T-007：已完成可離線資產 smoke；待可用 HTTP 請求/瀏覽器環境與測試帳號後，補登入、Portal SSO、JWT、SQL、401、403、Vue refresh smoke；HTTPS mixed content 隨 T-006 阻擋。
- T-008：已完成需求索引、變更紀錄與驗證證據同步；T-006/T-007 外部阻擋與待測項保留。

### 第二順位：SPC 線別分析項目總覽公式版本記錄（資料正確性）

`SPC-CHEM-OVERVIEW-VERSION-TASK-001` 已完成並補驗 HTTP smoke；完成紀錄請查 `CHANGELOG_CUSTOM.md` 與 `specs/20261007-chemical-overview-formula-versioning/verification.md`。

`SPC-CHART-POINT-REMARK-TASK-001` 已完成並發布 SPC 測試站；完成紀錄請查 `CHANGELOG_CUSTOM.md`、`docs/requirements.md` 與 `specs/20261007-spc-chart-point-remarks/verification.md`。

### 第三順位：Portal 生日通知調整（個資與人事權限）

`PORTAL-BIRTHDAY-REPLAN-TASK-001`～`TASK-003` 已完成並發布 Portal 測試站；團保專區已取消。完成紀錄請查 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`specs/20261007-portal-birthday-replan/` 與 Portal 對應 specs。

### 第四順位：全專案 AI + BDD 導入（流程治理）

#### AI-BDD-ALL-TASK-001：全專案導入盤點與共用範本

狀態：已完成文件型小工作；主規格 `specs/20261009-ai-bdd-all-task-001/spec.md`

初步範圍：盤點 SPC、PmrPortal、TransFiles、KM、Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice、python-pypxlib 的 AI+BDD 狀態，建立共用段落、BDD 範本與 STOP RULE。

#### AI-BDD-ALL-TASK-002：第一批高頻專案導入

狀態：已完成文件型導入；主規格 `specs/20261009-ai-bdd-all-task-002/spec.md`

範圍：PmrPortal、TransFiles、KM 與 SPC 對齊 AI+BDD 入口、features 目錄與導入紀錄；不補造歷史規格。

#### AI-BDD-ALL-TASK-003：第二批支援/工具專案導入

狀態：已完成文件型導入；主規格 `specs/20261009-ai-bdd-all-task-003/spec.md`

範圍：Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice、python-pypxlib 依專案大小採輕量導入。

### 第五順位：KM 教育訓練系統（新功能）

#### KM-TRAINING-TASK-001：教育訓練系統需求規格與資料來源盤點

狀態：已完成（2026-10-09，KM 規格 `specs/tasks/TASK-012-training-requirements-inventory.md`；SPC 同步 `specs/20261009-km-training-task001-sync/`）

初步範圍：定義一般使用者、講師/課程管理者、人事/admin；定義課程、教材、梯次、指派、報名、簽到、測驗、完成紀錄與報表；盤點人員資料來源。

#### KM-TRAINING-TASK-002：課程與教材管理

狀態：已完成（2026-10-09，KM 規格 `specs/tasks/TASK-013-training-course-material-management.md`；SPC 同步 `specs/20261009-km-training-task002-sync/`）

範圍：課程主檔、分類、講師、時數、教材附件、啟用/停用與版本紀錄。

#### KM-TRAINING-TASK-003：訓練指派、報名與簽到

狀態：已完成（2026-10-09，KM 規格 `specs/tasks/TASK-014-training-assignment-registration-attendance.md`；SPC 同步 `specs/20261009-km-training-task003-sync/`）

範圍：依人員/部門/職務指派，梯次報名，名額限制，簽到與補課紀錄。

#### KM-TRAINING-TASK-004：測驗、完成認定與個人進度

狀態：已完成（2026-10-09，KM 規格 `specs/tasks/TASK-015-training-exam-completion-progress.md`；SPC 同步 `specs/20261009-km-training-task004-sync/`）

範圍：題庫、測驗、及格分數、完成證明、個人待辦與完成歷程。

#### KM-TRAINING-TASK-005：管理報表與匯出

狀態：已完成（2026-10-09，KM 規格 `specs/tasks/TASK-016-training-report-export.md`；SPC 同步 `specs/20261009-km-training-task005-sync/`）

範圍：完成率、逾期清單、課程歷程、部門統計與 Excel 匯出。

### 第六順位：正式機程式碼更新工具（正式發布/IIS 高風險）

#### RELEASE-TOOL-TASK-001：正式機更新工具規格與環境盤點

狀態：已完成規格（2026-10-09，`specs/20261009-release-tool-task001/`）；正式站操作仍需另行授權

初步範圍：盤點正式機連線方式、IIS site/app pool、實體路徑、備份路徑、服務帳號與權限；定義 release manifest、hash、版本、目標環境、來源包與 smoke test URL；本階段只規格，不連線正式機。

#### RELEASE-TOOL-TASK-002：備份與回復流程原型

狀態：已完成原型（2026-10-09，`tools/release/ReleaseBackupRestore.ps1`、`specs/20261009-release-tool-task002/`）；僅限非正式目標演練

範圍：建立可在非正式目標演練的 PowerShell/CLI，支援備份網站資料夾、匯出 IIS 設定、hash 紀錄與回復。

#### RELEASE-TOOL-TASK-003：更新 IIS 網站資料夾流程

狀態：已完成原型（2026-10-09，`tools/release/ReleaseApplyPackage.ps1`、`specs/20261009-release-tool-task003/`）；僅限非正式目標演練

範圍：app_offline/app pool 停啟、複製交付包、保留環境設定、移除暫停檔、smoke test。

#### RELEASE-TOOL-TASK-004：測試站/沙盒演練與 rollback 驗證

狀態：已完成沙盒演練（2026-10-09，`specs/20261009-release-tool-task004/`）；不得操作正式機

範圍：使用非正式目標完整演練更新與回復，保留證據；不得操作正式機。

正式機更新工具本批小工作已完成到手冊與授權檢核；正式站實際操作仍需使用者另行授權，且不得發布到 `D:\Sites\PmrPortal`。

## 暫緩：SPC 咬蝕 X- 不列入 SPC（需求未完整）

#### SPC-ETCH-X-TASK-001：咬蝕 X- / 不生產資料匯入排除規格

狀態：使用者指定先跳過；需求尚未想完整

初步範圍：釐清 `X-`、不生產、停線、免檢等來源欄位與判定值；保留匯入資料與批次追溯，但不列入 SPC 管制圖、CL/UCL/LCL、OOC/OOS 與總覽統計。

待確認：X- 是否只在檢測值欄；排除資料是否在 Portal 查詢顯示；歷史資料是否補標。

#### SPC-ETCH-X-TASK-002：咬蝕 X- 排除實作與測試

狀態：使用者指定先跳過；待 SPC-ETCH-X-TASK-001 需求重新確認

初步範圍：依規格調整匯入預覽、確認流程、SPC 查詢/計算排除口徑，並建立測試與必要端到端驗證。

## 暫停併行：架構改善 TODO

`TASK-002`～`TASK-010` 架構改善暫不插隊；若與本批 Portal/SPC 工作碰到相同區域，先以本批業務需求的小範圍修改優先。

### TASK-002：MES Sync 資料可靠性

狀態：已完成（2026-10-10，`specs/20261010-architecture-task002-mes-sync-reliability/`）；SPC 測試站 backend 已發布，正式站未發布

### TASK-003：診斷端點與錯誤資訊隔離

狀態：已完成（2026-10-10，`specs/20261010-architecture-task003-diagnostics-error-isolation/`）；SPC 測試站 backend 已發布，正式站未發布

### TASK-004：後端授權模型收斂

狀態：待確認

### TASK-005：資料庫 migration 與啟動流程

狀態：待確認

### TASK-006：發布流程與環境隔離

狀態：待確認

### TASK-007：SMTP、檔案與背景排程可靠性

狀態：待確認

### TASK-008：稽核與操作者追溯

狀態：待確認

### TASK-009：測試與可重建基線

狀態：待確認

### TASK-010：大型模組拆分與架構治理

狀態：待確認

## 2026-10-10 執行紀錄

- `ARCH-TASK-002-DIAGNOSTICS-MES-SYNC-RELIABILITY-20261010`：已完成 MES Sync 資料可靠性改善。新增可單測的 `MesSyncMessageBatchProcessor` 與 `IMesSyncMessageHandler`，背景服務改呼叫批次處理器；無效 JSON 與未支援 MessageType 會標示 `Failed` 並留下錯誤，不再假標 `Processed`。針對性測試 3 passed，後端 build 通過；已發布 SPC 測試站 backend，`/api/health` 200、`/api/version` 200/test、首頁 200。正式站未發布。
- `ARCH-TASK-003-DIAGNOSTICS-ERROR-ISOLATION-20261010`：已完成診斷端點與錯誤資訊隔離。新增安全錯誤回應工廠、調整全域 exception handler 非 Development 不回傳 exception detail，新增匿名 `/api/health`。針對性測試 3 passed，後端 build 通過；已發布 SPC 測試站 backend，`/api/health` 200、`/api/version` 200/test、首頁 200。正式站未發布。
- `TODO-COMPLETED-ITEMS-CLEANUP-20261010`：已整理 `TODO.md` 未完成排序區，移除已完成或取消的 SPC 管制圖點位備註、Portal 生日通知三個 Task 與團保取消項目；追溯改查需求索引、CHANGELOG 與對應 specs。本次僅文件變更，未修改功能、未發布。
- `SPC-CHEM-OVERVIEW-VERSION-TASK-001`：已補驗 SPC 測試站 HTTP smoke。`8084/api/version` 200/test、`8084/` 200、既有 `8081/api/version` 200、`8083/` 200；單站前端 JS 包含 `chemicalAnalysisFormulaVersionReason` 且不含 `/api/api`。本次僅驗證與文件同步，未修改程式、未發布正式站。
- `IIS-TASK-008`：已同步單一 IIS Site 的需求索引、變更紀錄與驗證證據。HTTP 測試單站 `SpcSingleTest` 已建立且 Vue/API 基本 smoke 有紀錄；HTTPS binding/cert、mixed content、真人登入、Portal SSO、JWT 與 Viewer 403 仍待外部條件，不標示 T-006/T-007 完成。本次僅文件變更，未操作 IIS、未發布。
- `RELEASE-TOOL-TASK-005`：已完成正式機更新工具使用手冊與授權檢核。SPC 產出 `specs/20261010-release-tool-task005/spec.md` 與 `docs/release-tools/production-release-runbook.md`，整理操作手冊、參數範本、前置檢查、回復步驟、證據格式與核准清單。本次僅文件變更，未連線正式機、未操作 IIS、未讀取機敏設定、未刪除資料、未發布。

## 2026-10-09 執行紀錄

- `KM-TRAINING-TASK-001`：已完成教育訓練系統需求規格與資料來源盤點。KM 產出 `specs/tasks/TASK-012-training-requirements-inventory.md`、`features/training-system.feature`、`docs/training-system-requirements.md`；SPC 產出 `specs/20261009-km-training-task001-sync/`。本次僅文件與規格變更，未刪除資料、未讀取正式人員名單、未發布。
- `KM-TRAINING-TASK-002`：已完成課程與教材管理規格。KM 產出 `specs/tasks/TASK-013-training-course-material-management.md`、`features/training-course-material.feature`、`docs/training-course-material-management.md`；SPC 產出 `specs/20261009-km-training-task002-sync/`。本次僅文件與規格變更，未刪除資料、未讀取正式人員或講師名單、未發布。
- `KM-TRAINING-TASK-003`：已完成訓練指派、報名與簽到規格。KM 產出 `specs/tasks/TASK-014-training-assignment-registration-attendance.md`、`features/training-assignment-registration-attendance.feature`、`docs/training-assignment-registration-attendance.md`；SPC 產出 `specs/20261009-km-training-task003-sync/`。本次僅文件與規格變更，未刪除資料、未讀取正式人員、部門或職務名單、未發布。
- `KM-TRAINING-TASK-004`：已完成測驗、完成認定與個人進度規格。KM 產出 `specs/tasks/TASK-015-training-exam-completion-progress.md`、`features/training-exam-completion-progress.feature`、`docs/training-exam-completion-progress.md`；SPC 產出 `specs/20261009-km-training-task004-sync/`。本次僅文件與規格變更，未刪除資料、未讀取正式人員、題庫或測驗資料、未發布。
- `KM-TRAINING-TASK-005`：已完成管理報表與匯出規格。KM 產出 `specs/tasks/TASK-016-training-report-export.md`、`features/training-report-export.feature`、`docs/training-report-export.md`；SPC 產出 `specs/20261009-km-training-task005-sync/`。本次僅文件與規格變更，未刪除資料、未讀取正式人員、部門、課程或完訓資料、未發布。
- `RELEASE-TOOL-TASK-001`：已完成正式機更新工具規格與環境盤點欄位。SPC 產出 `specs/20261009-release-tool-task001/`，定義正式機連線方式、IIS site/app pool、實體路徑、備份路徑、服務帳號權限、release manifest、hash、版本、目標環境、來源包與 smoke test URL。本次僅文件與規格變更，未連線正式機、未讀取機敏設定、未刪除資料、未發布。
- `RELEASE-TOOL-TASK-002`：已完成備份與回復流程原型。新增 `tools/release/ReleaseBackupRestore.ps1` 與 `specs/20261009-release-tool-task002/`，支援非正式目標備份網站資料夾、IIS 設定匯出、hash 紀錄與回復；已驗證語法、Production 阻擋與 `D:\Sites\PmrPortal` 阻擋。本次未連線正式機、未讀取機敏設定、未刪除資料、未發布。
- `RELEASE-TOOL-TASK-003`：已完成更新 IIS 網站資料夾流程原型。新增 `tools/release/ReleaseApplyPackage.ps1` 與 `specs/20261009-release-tool-task003/`，支援非正式目標 app_offline 建立、app pool 停啟、複製交付包、保留環境設定與 smoke test evidence；`app_offline.htm` 移除列入待刪除清單，未自動刪除。本次未連線正式機、未讀取機敏設定、未刪除資料、未發布。
- `RELEASE-TOOL-TASK-004`：已完成非正式 sandbox 備份、套用與 rollback 演練。Sandbox root 為 `release-staging/release-tool-task004/run-20261009-201622`；首次演練抓到 wildcard copy 問題並修正；重跑後備份、套用、回復均通過，回復後測試環境設定保留，`app_offline.htm` 依規則未刪除並列入待刪除清單。本次未連線正式機、未讀取機敏設定、未刪除資料、未發布。
