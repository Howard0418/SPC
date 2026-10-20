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

目前未完成小工作：4 件（SPC選版TASK-001～005已完成，後續1件未執行；KM暫緩3件；測試SPC Web0.1.68／API0.1.66、Portal API1.0.183／Web1.0.226已發布，正式未更新）。EXE 1.1.0 製作交付包功能已完成。Windows更新EXE已完成並交付；本次未執行實際更新。環境切換與遠端入口已完成，四組真實預檢通過；本次未執行更新。正式版更新工具已完成隔離驗證；工具本次未在正式機執行更新。本次 SPC／Portal 正式站更新及來源上傳已完成。登入後人工業務驗收尚未執行，詳見發布驗證紀錄。

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

1. KM-DEPS-SECURITY-20261019：既有套件弱點評估修補。
2. KM-VERIFY-034-20261019：文件櫃畫面／發布核對。
3. KM-VERIFY-036-20261019：版本差異摘要驗證。

上述3件為KM既有工作池同步，非本次新增範圍；外部附件掃毒／SSO nonce選型另列。

## 目前執行排序（2026-10-19，使用者指定SPC優先）
本批SPC選版TASK-001～005已完成；未完成4件（SPC後續1件、KM暫緩3件），每次只執行一件。

### 待確認後序列執行：SPC公式日期選版與追溯
- 規劃：[SPC-FORMULA-EFFECTIVE-TRACE-PLAN-20261020](specs/20261020-formula-effective-trace-plan/spec.md)；[工作與驗收](specs/20261020-formula-effective-trace-plan/tasks.md)。TASK-001～005已完成，TASK-006待執行。真正TransFiles DB直寫仍需受控程序及client，不用現有API匯入代替。
- SPC-FORMULA-TRACE-TASK-006：實際版本明細、跨系統與Antigravity驗收。
- 資料正確性優先；只允許一次執行一件，DB／公式高風險不併行。規格／界限獨立有效期保留後案需求，未納入本批實作；不視為已授權正式DB或發布。

已完成：
- SPC-FORMULA-TRACE-TASK-005：[受控SQL直寫與統計](specs/20261020-formula-trace-task005/spec.md)完成；測試API0.1.66、個人Windows低權限連線、SQL並發／rollback驗證通過；正常業務真人操作待TASK-006案例。
- SPC-FORMULA-TRACE-TASK-004：[Portal日期預覽與原子留存](specs/20261020-formula-trace-task004/spec.md)完成並發布兩系統測試站；[驗證及人工案例](specs/20261020-formula-trace-task004/verification.md)，真人操作待驗、正式未更新。
- SPC-FORMULA-TRACE-TASK-003：[日期選版與計算契約](specs/20261020-formula-trace-task003/spec.md)完成；78後端、7新增／9既有瀏覽器及SQL scenario通過。測試API0.1.64／Web0.1.68與TASK-002 schema已發布，policy未啟用；正式未操作，人工案例待驗。
- SPC-FORMULA-TRACE-TASK-002：[schema及真實SQL隔離演練](specs/20261020-formula-trace-task002/spec.md)完成；42相關passed／2 opt-in Skip，完整SQL scenario通過Up／Down／Up、期間／FK／immutable、四表故障rollback、併發及DML拒絕，備份VERIFYONLY成功。未升級共用測試／正式DB，未發布帶migration的新DLL。
- SPC-FORMULA-TRACE-TASK-001：[真實來源測試與直寫契約](specs/20261020-formula-trace-task001/spec.md)已完成；Python10、跨引擎／日期9 passed，預設相關回歸23 passed／1明確Skip。本件無應用／DB變更或發布；真正直寫、SQL權限與併發實證留後續。
- SPC-CHART-SEGMENT-VERSION-TASK-001：版本2唯讀明細已完成並發布測試API0.1.63／Web0.1.67；41後端相關、11新增瀏覽器案例及9既有回歸通過。真人Antigravity驗收待執行，測試庫無分段，完整選段以fixture驗證；正式未更新。
- SPC-CHEM-REFERENCE-HISTORY-20261019：公式備註與總覽共用歷史；原測試站API0.1.61／Web0.1.64，已由後案更新。
- SPC-VERSION-HISTORY-DATE-FILTER-20261019：[三類日期歷史](specs/20261019-version-history-date-filter/spec.md)，2026-10-20完成規格歷史、F既有migration補齊及台北日期篩選，37相關／9瀏覽器通過，測試站API0.1.62／Web0.1.65；正式未發布。全套6既有失敗已基準比對記錄。

### 暫緩：KM（使用者明確要求先不要執行）
- KM-DEPS-SECURITY-20261019：既有套件弱點評估修補。
- KM-VERIFY-034-20261019：文件櫃畫面／發布核對。
- KM-VERIFY-036-20261019：版本差異摘要驗證。

每次只做一件；KM保留待辦，不自動恢復。使用者後續已確認SPC執行計畫；依序單件完成，正式發布仍需另行授權。

## 緊急修正完成（2026-10-20）
- SPC-TEST-WEB-API-SAME-ORIGIN-20261020：[測試前端同源API](specs/20261020-test-web-api-same-origin/spec.md)，已修復／驗證並發布測試Web0.1.66；API0.1.62／DB不變，1新測試＋9回歸及實際bundle請求驗證通過，真正12個主檔API均200。此項不列入未完成數。
