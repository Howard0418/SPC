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

## 目前小工作排序（2026-10-14）

目前有效未完成小工作：4 件（SEC-FOLLOWUP-002、004～006，見文末安全/正式更新工作池）。SEC-FOLLOWUP-001唯讀盤點/評估完成；舊批次完成與版本紀錄以各specs驗證為準，不代表本次有安裝或正式更新。

### 已完成追溯：SPC 改由 TransFiles 直接匯入（阻擋測試匯入／資料正確性）

規格：`specs/20261014-spc-import-via-transfiles-only-plan/spec.md`

`SPC-TRANSFILES-IMPORT-TASK-001`～`TASK-005` 已完成；完成紀錄請查 `CHANGELOG_CUSTOM.md` 與 `specs/20261014-spc-import-via-transfiles-only-plan/verification.md`。

### 已完成追溯：SPC 常態檢定與直方圖（統計正確性／圖表可讀性）

規格：`specs/20261012-spc-rawdata-normality-issues-plan/spec.md`

`SPC-RAWDATA-1012-TASK-001`～`TASK-003`、`SPC-NORMALITY-1012-TASK-001`、`SPC-HISTOGRAM-1012-TASK-001` 已完成；完成紀錄請查 `CHANGELOG_CUSTOM.md` 與 `specs/20261012-spc-rawdata-normality-issues-plan/verification.md`。

### 已完成追溯：SPC 線別分析項目總覽公式版本記錄（資料正確性）

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

## 暫停併行：架構改善 TODO

`TASK-002`～`TASK-010` 架構改善暫不插隊；若與本批 Portal/SPC 工作碰到相同區域，先以本批業務需求的小範圍修改優先。

### TASK-002：MES Sync 資料可靠性

狀態：已完成（2026-10-10，`specs/20261010-architecture-task002-mes-sync-reliability/`）；SPC 測試站 backend 已發布，正式站未發布

### TASK-003：診斷端點與錯誤資訊隔離

狀態：已完成（2026-10-10，`specs/20261010-architecture-task003-diagnostics-error-isolation/`）；SPC 測試站 backend 已發布，正式站未發布

### TASK-004：後端授權模型收斂

狀態：已完成（2026-10-11，`specs/20261011-architecture-task004-backend-authorization-consolidation/`）；SPC 測試站 backend 已發布，正式站未發布

### TASK-005：資料庫 migration 與啟動流程

狀態：已完成（2026-10-11，`specs/20261011-architecture-task005-database-startup-flow/`）；SPC 測試站 backend 已發布，正式站未發布

### TASK-006：發布流程與環境隔離

狀態：已完成（2026-10-12，`specs/20261012-architecture-task006-release-environment-isolation/`）；工具與文件型小工作，未發布測試站或正式站

### TASK-007：SMTP、檔案與背景排程可靠性

狀態：已完成（2026-10-12，`specs/20261012-architecture-task007-smtp-file-scheduler-reliability/`）；SPC 測試站 backend 已發布，正式站未發布

### TASK-008：稽核與操作者追溯

狀態：已完成（2026-10-12，`specs/20261012-architecture-task008-audit-operator-traceability/`）；SPC 測試站 backend 已發布，正式站未發布

### TASK-009：測試與可重建基線

狀態：已完成（2026-10-12，`specs/20261012-architecture-task009-reproducible-test-baseline/`）；工具與文件型小工作，未發布測試站或正式站

### TASK-010：大型模組拆分與架構治理

狀態：已完成（2026-10-12，`specs/20261012-architecture-task010-large-module-governance/`）；工具與文件型小工作，未發布測試站或正式站

## 2026-10-12 執行紀錄

- `PORTAL-BIRTHDAY-REPLAN-TASK-004`：已完成 Portal 測試生日祝賀按鍵。PmrPortal 人事生日通知設定頁新增「測試生日祝賀」按鍵與廣告式 Bootstrap 彈窗，套用目前祝詞與祝賀圖片並提供關閉；不新增生日資料寫入。驗證通過：Portal Web build 0 warnings / 0 errors，測試站設定頁未登入 401、API health 200/test、發布目錄含新 CSS；已發布 Portal 測試站 Web，正式站未發布。2026-10-12 補修：圖片上傳支援常見 Content-Type 變體，預覽圖片改用 Portal API 完整圖片網址；API 測試 7 passed，API/Web build 通過，已發布 Portal 測試站 API/Web。2026-10-12 版型調整：生日賀卡改為購物網站廣告式置中彈窗，含紅橘色圓角卡片、圖片置中、浮動關閉鈕與「收到祝福」CTA；已發布 Portal 測試站 Web。2026-10-12 儲存 500 補修：`feature_settings.message` 放寬為 `nvarchar(max)` 並補齊 EF migration metadata，240 字祝詞 JSON 可儲存；生日設定 API 測試 9 passed、API build 通過，已發布 Portal 測試站 API，測試 DB 已確認欄位與 migration history。2026-10-12 顯示模式調整：可選只顯示圖片或顯示賀詞 + 圖片；生日設定 API 測試 10 passed、API/Web build 通過，已發布 Portal 測試站 API/Web。目前剩餘未完成小工作 4 件（IIS 外部條件阻擋 2 件、咬蝕 X- 暫緩 2 件）。
- `ARCH-TASK-010-LARGE-MODULE-GOVERNANCE-20261012`：已完成大型模組拆分與架構治理基線。新增 `tools/architecture/Get-LargeModules.ps1` 與 `docs/architecture-governance.md`，固定大型模組盤點門檻、候選模組、責任邊界、拆分順序與最低驗證要求；不修改應用程式、不拆核心類別。驗證通過：門檻 700 可抓出 11 個超標模組並回傳非 0，門檻 10000 回傳 0，治理文件關鍵字檢查通過。本次未發布測試站或正式站。架構改善 `TASK-002`～`TASK-010` 已全部完成；目前剩餘未完成小工作 4 件（IIS 外部條件阻擋 2 件、咬蝕 X- 暫停 2 件）。
- `ARCH-TASK-009-REPRODUCIBLE-TEST-BASELINE-20261012`：已完成測試與可重建基線第一步改善。新增 `tools/testing/Invoke-SpcBaseline.ps1`，固定順序執行後端測試、後端 build、前端 build 與可選 smoke，支援 `-TestFilter`、`-SkipTests`、`-SkipFrontend`、`-SmokeUrl`。驗證通過：後端測試/build 模式與 build+smoke 模式皆通過；本次未修改應用程式、未發布、未清資料。
- `ARCH-TASK-008-AUDIT-OPERATOR-TRACEABILITY-20261012`：已完成稽核與操作者追溯第一步改善。新增共用 `AuditActorResolver`，SPC 點位備註/排除 `UpdatedBy` 改用同一套 claim 解析規則；不新增 schema、不改登入或 JWT 發行。驗證通過：操作者解析與點位備註測試 15 passed，後端 build 通過；已發布 SPC 測試站 backend，`/api/health` 200、`/api/version` 200/test、首頁 200。正式站未發布。
- `ARCH-TASK-007-SMTP-FILE-SCHEDULER-RELIABILITY-20261012`：已完成 SMTP、檔案與背景排程可靠性第一步改善。新增 `SpcReportScheduleProcessor`，報表週報/月報至少一位收件者寄送成功才更新 LastSent；全部 SMTP 寄送失敗會保留 LastSent 以便下次排程重試。不新增 schema、不改 SMTP 設定 UI。驗證通過：processor 測試 3 passed、後端 build 通過；已發布 SPC 測試站 backend，`/api/health` 200、`/api/version` 200/test、首頁 200。正式站未發布。
- `ARCH-TASK-006-RELEASE-ENVIRONMENT-ISOLATION-20261012`：已完成發布流程與環境隔離改善。新增共用 `ReleaseTargetGuard.psm1`，兩支 release 工具改用同一份正式環境與 `D:\Sites\PmrPortal` 路徑阻擋規則；新增 PowerShell 測試。驗證通過：guard 測試、Production/Portal 拒絕、Sandbox `-WhatIf` 載入。本次未操作正式站、未發布、未刪除資料。
- `SPC-RAWDATA-1012-TASK-001`：已完成 All 總表後 raw data 下載上下文修正。新增前端 raw data active PPC 解析工具，下載時依序使用 `ppcId`、總表列 `partProcessCharacteristicId`、舊欄位 `ppcId` 與 selected mapping，避免 All 總表 drilldown 後仍提示需先查詢管制圖。驗證通過：raw data 下載解析測試 5 passed、前端 build 通過；已發布 SPC 測試站 frontend，備份 `release/test/frontend.backup-rawdata-context-20261012-172446`，HTTPS smoke 通過。正式站未發布。目前剩餘未完成小工作 8 件（Raw Data 2 件、咬蝕 X- 2 件、常態/直方圖 2 件、IIS 外部條件阻擋 2 件）。
- `SPC-RAWDATA-1012-TASK-002`：已完成 Raw data `Value` 日期誤判與項目資料正確性修正。後端 CSV 組裝抽出 `RawDataCsvExporter`，日期欄位才格式化，`Value` 欄保留原始 invariant 字串，並過濾其他 `PartProcessCharacteristicId` 的列。驗證通過：RawDataCsvExporter 測試 2 passed、後端 build 通過；已發布 SPC 測試站 backend，備份 `release/test/backend.backup-rawdata-value-20261013-074103`，HTTPS smoke 通過。正式站未發布。目前剩餘未完成小工作 7 件（Raw Data 1 件、咬蝕 X- 2 件、常態/直方圖 2 件、IIS 外部條件阻擋 2 件）。
- `SPC-RAWDATA-1012-TASK-003`：已完成 Raw data 規格資訊補欄。CSV 新增 `SpecLSL`、`SpecTarget`、`SpecUSL`、`Unit` 欄位，來源為下載目標 `PartProcessCharacteristic` 的規格欄位，與圖表 limits 口徑一致。驗證通過：RawDataCsvExporter 測試 3 passed、後端 build 通過；已發布 SPC 測試站 backend，備份 `release/test/backend.backup-rawdata-spec-20261013-074946`，HTTPS smoke 通過。正式站未發布。目前剩餘未完成小工作 6 件（咬蝕 X- 2 件、常態/直方圖 2 件、IIS 外部條件阻擋 2 件）。
- `SPC-ETCH-X-TASK-001`：已完成咬蝕 X- 線別排除規格與測試案例。補齊 TransFiles/Portal/SPC 契約、預覽排除狀態、確認匯入不得送 SPC、測試案例矩陣與回復方式；本次未修改程式、未匯入資料、未發布。目前剩餘未完成小工作 5 件（咬蝕 X- 1 件、常態/直方圖 2 件、IIS 外部條件阻擋 2 件）。
- `SPC-ETCH-X-TASK-002`：已完成咬蝕 X- 線別排除實作與測試。TransFiles 解析咬蝕日報時讀取各區塊第 2 列線別標題，`X-` 前綴線別加入 `excluded` 與 Excel「排除清單」，不產生 SPC `reports`；未帶 `X-` 線別維持既有流程。驗證通過：TransFiles 咬蝕相關測試 11 passed、py_compile 通過；真實來源唯讀抽取 reports 114、excluded 8。未打包 EXE、未連線 Portal/SPC、未寫入資料，SPC 測試站發布不適用。正式站未發布。目前剩餘未完成小工作 4 件（常態/直方圖 2 件、IIS 外部條件阻擋 2 件）。
- `SPC-NORMALITY-1012-TASK-001`：已完成常態檢定與 Minitab 差異確認。SPC 後端回傳 `MethodNote`，前端管制圖/趨勢圖揭露 SPC 採 Adjusted Jarque-Bera，Minitab 常用 Anderson-Darling，p-value 與判定不可直接等同；固定樣本測試保護方法揭露與 p-value 範圍。驗證通過：NormalityTest 5 passed、後端 build 通過、前端 build 通過；已發布 SPC 測試站 backend/frontend，HTTPS smoke 通過。正式站未發布。目前剩餘未完成小工作 3 件（直方圖 1 件、IIS 外部條件阻擋 2 件）。
- `SPC-HISTOGRAM-1012-TASK-001`：已完成直方圖 scale/bin 改善。管制圖與趨勢圖直方圖 bins 改以未排除量測值範圍建立，規格/管制線不再納入 bins domain 壓縮主要分布；後端常態曲線縮放同步改用量測值 bin width。驗證通過：histogram 測試 2 passed、NormalityTest 6 passed、後端 build 通過、前端 build 通過；已發布 SPC 測試站 backend/frontend，HTTPS smoke 通過。正式站未發布。目前剩餘未完成小工作 0 件（IIS 外部條件阻擋已於 2026-10-12 補驗完成；正式站操作仍需另行授權）。

## 2026-10-11 執行紀錄

- `ARCH-TASK-005-DATABASE-STARTUP-FLOW-20261011`：已完成資料庫 migration 與啟動流程第一步收斂。新增 `DatabaseStartupPlanner`，將 provider 正規化與 SQLite `EnsureCreated` / SQL Server baseline + EF migration 決策抽成可測試規則；不新增 migration、不改相容性 SQL、不操作正式資料庫。Planner 測試 8 passed，後端 build 通過；已發布 SPC 測試站 backend，`/api/health` 200、`/api/version` 200/test、首頁 200。正式站未發布。
- `ARCH-TASK-004-BACKEND-AUTHORIZATION-CONSOLIDATION-20261011`：已完成後端授權模型收斂。新增共用 `AuthorizationAccess` 服務，校正儀器管理與儀器匯入 controller 改由同一服務判斷登入者角色、頁面權限與操作者啟用狀態；不改登入流程、JWT claims 或 DB schema。授權相關測試 11 passed，後端 build 通過；已發布 SPC 測試站 backend，`/api/health` 200、`/api/version` 200/test、首頁 200。正式站未發布。
- `COMPLETION-REMAINING-WORK-REMINDER-20261011`：已將「每執行完一個小工作後，最終回覆提醒目前剩餘未完成小工作數」寫入 `AGENTS.md` 小工作完成規則；同步需求索引、CHANGELOG 與變更目的。本次僅文件變更，未修改程式、未發布。

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

## Portal會議室預約可見性（2026-10-19，已完成）


[Portal主規格](D:/PmrPortal/specs/20261019-meeting-reservation-visibility/spec.md)；TASK-001／002已依後續授權完成測試站，正式站已發布（API1.0.182／Web1.0.225）。


## Portal會議室完整月份查詢（2026-10-19，已完成測試站）

- 已完成完整月份查詢與跨月行程保留，前案PRG／成功定位不變。
- [Portal主規格](D:/PmrPortal/specs/20261019-meeting-calendar-month-query/spec.md)。已依後續授權完成實作、驗證及Portal測試站發布；正式站已發布（API1.0.182／Web1.0.225）。

## KM待辦同步（2026-10-19）

1. 已完成 KM-DEPS-SECURITY-20261019：Negotiate/Pkcs 10.0.13、43項回歸、測試站1.6.5，正式未更新。
2. 已完成 KM-VERIFY-034-20261019：文件清單修正、50項測試、測試站1.6.6與桌面/手機空狀態驗收。
3. 已完成驗證 KM-VERIFY-036-20261019：TASK-041共59項通過及部署hash核對，驗證提交ec1499e已上傳，完成狀態文件上傳待授權。

上述為KM既有工作池同步，目前功能/驗證未完成0件（僅完成狀態文件上傳待授權），非本次新增範圍；外部附件掃毒／SSO nonce選型另列。

## 目前執行排序（2026-10-20，使用者恢復KM）
本批SPC選版TASK-001～006已完成；功能/驗證未完成0件（僅完成狀態文件上傳待授權），每次只執行一件。

### 已完成：SPC公式日期選版與追溯（工作池無未完成SPC項目）
- 規劃：[SPC-FORMULA-EFFECTIVE-TRACE-PLAN-20261020](specs/20261020-formula-effective-trace-plan/spec.md)；[工作與驗收](specs/20261020-formula-effective-trace-plan/tasks.md)。TASK-001～006已完成，正常真人操作依交付案例確認，不新增假造歷史。真正TransFiles DB直寫仍需受控程序及client，不用現有API匯入代替。
- 資料正確性優先；只允許一次執行一件，DB／公式高風險不併行。規格／界限獨立有效期保留後案需求，未納入本批實作；不視為已授權正式DB或發布。

已完成：
- SPC-FORMULA-TRACE-TASK-006：[實際公式版本查閱](specs/20261020-formula-trace-task006/spec.md)完成並發布測試API0.1.67／Web0.1.69；[驗證](specs/20261020-formula-trace-task006/verification.md)、[Antigravity案例](specs/20261020-formula-trace-task006/antigravity-tests.md)。正常真人操作待驗，正式未更新。
- SPC-FORMULA-TRACE-TASK-005：[受控SQL直寫與統計](specs/20261020-formula-trace-task005/spec.md)完成；測試API0.1.66、個人Windows低權限連線、SQL並發／rollback驗證通過；正常業務真人操作待TASK-006案例。
- SPC-FORMULA-TRACE-TASK-004：[Portal日期預覽與原子留存](specs/20261020-formula-trace-task004/spec.md)完成並發布兩系統測試站；[驗證及人工案例](specs/20261020-formula-trace-task004/verification.md)，真人操作待驗、正式未更新。
- SPC-FORMULA-TRACE-TASK-003：[日期選版與計算契約](specs/20261020-formula-trace-task003/spec.md)完成；78後端、7新增／9既有瀏覽器及SQL scenario通過。測試API0.1.64／Web0.1.68與TASK-002 schema已發布，policy未啟用；正式未操作，人工案例待驗。
- SPC-FORMULA-TRACE-TASK-002：[schema及真實SQL隔離演練](specs/20261020-formula-trace-task002/spec.md)完成；42相關passed／2 opt-in Skip，完整SQL scenario通過Up／Down／Up、期間／FK／immutable、四表故障rollback、併發及DML拒絕，備份VERIFYONLY成功。未升級共用測試／正式DB，未發布帶migration的新DLL。
- SPC-FORMULA-TRACE-TASK-001：[真實來源測試與直寫契約](specs/20261020-formula-trace-task001/spec.md)已完成；Python10、跨引擎／日期9 passed，預設相關回歸23 passed／1明確Skip。本件無應用／DB變更或發布；真正直寫、SQL權限與併發實證留後續。
- SPC-CHART-SEGMENT-VERSION-TASK-001：版本2唯讀明細已完成並發布測試API0.1.63／Web0.1.67；41後端相關、11新增瀏覽器案例及9既有回歸通過。真人Antigravity驗收待執行，測試庫無分段，完整選段以fixture驗證；正式未更新。
- SPC-CHEM-REFERENCE-HISTORY-20261019：公式備註與總覽共用歷史；原測試站API0.1.61／Web0.1.64，已由後案更新。
- SPC-VERSION-HISTORY-DATE-FILTER-20261019：[三類日期歷史](specs/20261019-version-history-date-filter/spec.md)，2026-10-20完成規格歷史、F既有migration補齊及台北日期篩選，37相關／9瀏覽器通過，測試站API0.1.62／Web0.1.65；正式未發布。全套6既有失敗已基準比對記錄。

### KM：已恢復，依序單件執行
- 已完成 KM-DEPS-SECURITY-20261019：[TASK-039](C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/KM/specs/tasks/TASK-039-dependency-security-patches.md)，測試站1.6.5，正式未更新。
- 已完成 KM-VERIFY-034-20261019：TASK-040文件清單修正及測試站1.6.6驗收；正式未更新。
- 已完成驗證 KM-VERIFY-036-20261019：TASK-041版本摘要9項及合計59項回歸通過，測試站1.6.6部署hash一致；無應用更新，驗證提交ec1499e已上傳。

每次只做一件；2026-10-20使用者已恢復KM，已完成安全修補及文件櫃核對。最後版本摘要已驗證，功能待辦0件，驗證提交ec1499e已上傳；僅完成狀態文件上傳待授權，正式發布仍需另行授權。共用Hosting Bundle/Runtime更新另列維運評估，不在本案單站升級。

## 緊急修正完成（2026-10-20）
- SPC-TEST-WEB-API-SAME-ORIGIN-20261020：[測試前端同源API](specs/20261020-test-web-api-same-origin/spec.md)，已修復／驗證並發布測試Web0.1.66；API0.1.62／DB不變，1新測試＋9回歸及實際bundle請求驗證通過，真正12個主檔API均200。此項不列入未完成數。

## 後續安全與正式更新（2026-10-20，使用者已要求執行）
主規格：[SECURITY-PRODUCTION-FOLLOWUPS-20261020](specs/20261020-security-production-followups/spec.md)。本批原6件；001及003已於2026-10-21完成，現剩4件，每次只做1件：
1. SEC-FOLLOWUP-001：已完成主機/Defender/Runtime/發布相依盤點與更新評估（唯讀），見[驗證與維護決策](specs/20261020-security-production-followups/verification.md)。
2. SEC-FOLLOWUP-002：KM附件隔離/掃描整合（受阻：WithSecure簽章/help/單一合成檔掃描成功，使用者無法確認不外送樣本政策；IIS身分尚待驗證，不逕啟Defender）；[KM最小方案](C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/KM/specs/20261021-attachment-scan-integration/plan.md)。
3. SEC-FOLLOWUP-003：已完成SQL共享nonce（使用者同意002受阻時獨立執行）；81項及真實Portal SSO驗證通過，測試庫023/測試站1.6.7；[驗證](C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/KM/specs/20261021-shared-sso-nonce/verification.md)，正式未更新。
4. SEC-FOLLOWUP-004：Runtime/Hosting Bundle升級準備中；7份官方新舊包SHA512/簽章通過，使用者確認三機皆有備份，無演練環境，改提逐台維護；已確認10/21 18:00及必要重啟，待實際備份識別/時點與回復方式核對；[計畫](specs/20261021-runtime-maintenance/plan.md)，未安裝。
5. SEC-FOLLOWUP-005：KM已驗證新版正式更新（相依安全前置）。
6. SEC-FOLLOWUP-006：發布前核對中，SPC缺4筆migration、Portal正式金鑰保存待核對；[核對](specs/20261021-spc-portal-production-preflight/verification.md)，未發布。
先前小工作0件為原工作池歷史；本批001及003完成後未完成4件（002受阻、004～006待執行）。正式更新已取得使用者執行意向，未知DB變更與共用主機維護仍須具體核對。

## 2026-10-21 TransFiles歷史記錄核銷
[舊500診斷](specs/20261021-transfiles-500-reconciliation/spec.md)已核銷；主要4件不變，TransFiles明列舊待驗3降2（TASK-003/005），首筆LOG人工故障驗收另保留註記。

- TransFiles TASK-003 [續驗](specs/20261021-transfiles-login-delivery-check/spec.md)：12測試/hash通過，指南指現存EXE，待使用者按測試登入；主要4/舊驗收2不變。

## 最新計數（2026-10-21 TASK-003人工驗收後）
主要4件不變；TransFiles TASK-003已由使用者成功畫面核銷，明列舊驗收剩1件TASK-005，首筆LOG人工故障驗收註記保留。

- TransFiles TASK-005 [自動GUI續驗](specs/20261021-transfiles-login-failure-validation/spec.md)：59測試通過，實體EXE故障類別未驗，主要4/明列舊待驗1件不變。

- SEC-FOLLOWUP-006 [四筆正式DDL範圍](specs/20261021-spc-portal-production-preflight/database-scope.md)已完成源碼審查與前置物件唯讀核對，待具體授權/複本/正式金鑰/發布；主要4件不變。

- SEC-FOLLOWUP-006已獲四筆DDL授權並完成正式備份/SQL複本升級兩次與60原表指紋核對；正式未套用。Pkcs既有警告/業務相容/Runtime/Portal金鑰仍待，主要4件不變。

- SEC-FOLLOWUP-006安全套件子步驟完成：Pkcs10.0.13、234通過/8略過，測試API0.1.68及設定/DB指紋正常；正式發布與其他前置仍待，主要4件/TransFiles1件不變。

## 2026-10-21 15:30最新進度
- 004：使用者委託指定備份，本機WBAdmin C/allCritical映像執行中（約20%，未完成）；兩正式Hyper-V客體/宿主與空間核對完成，18:00後ProductionOnly checkpoint尚未建立。Runtime未安裝，D應用備份待完成。
- 006：Portal正式SMTP WebDAV 405已修正，使用者儲存成功及新密文解密通過，金鑰不變/未寄信；此子步驟完成，非整版更新。[驗證](specs/20261021-spc-portal-production-preflight/verification.md)。
- 主要未完成仍4件（002外部政策阻擋、004進行中、005/006待後續）；TransFiles明列舊驗收1件不變。

- 2026-10-21使用者再次授權正式更新；004現正執行系統映像及應用/附件/keyring預先備份，尚未通過完成驗證；既定18:00停站窗不變、無自動排程。005/006依序保留，主要4件/TransFiles1件不變。

- 最新指示：Runtime004暫緩，網站005(KM)/006(SPC+Portal)先發布；以現有Runtime相容性驗證取代004硬性前置，不安排18:00共用維護。主要4件含暫緩004及受阻002，TransFiles1件不變。

- 006發布阻擋子步驟：正式SpcWeb為純靜態站、無/api代理，R1前端已回復0.1.63、API0.1.68/四筆schema保留。先補集中API client的正式專用白名單端點，Test仍固定同源且忽略舊VITE_API_BASE；不安裝ARR/Rewrite、不改IIS binding/角色，再發布測試與正式前端。屬006必要相容修正，未新增工作計數。
