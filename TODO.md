> 2026-10-27報告結案：F/G/H/E已完成，本報告剩0件；其餘19件維持。舊節為歷史，以最新整併計數及[驗證](specs/20261027-mes-spc-report-plan/verification.md)為準。

# SPC 全系統未完成小工作池

狀態：僅顯示未完成、待規格、待執行、待確認小工作。已完成小工作不再保留於本檔，完成紀錄請查 `CHANGELOG_CUSTOM.md` 與對應 `specs/`。


## 最新整併計數（2026-10-28，以此節優先於下方歷史計數）
- 核心未完成29件：Portal Grafana入口1件＋網站調整共9件待執行（原6＋截圖新增3）＋既有19件：十頁簡報F／G／H／E全數完成，剩餘三語13件暫停；SEC002掃毒受阻1；SEC004 Runtime及TransFiles005 EXE暫緩2；CAL-VERIFY-001／002與REPORT-DATA-VERIFY-001待確認3。
- 另11筆待人工驗收／覆蓋限制註記（12筆歷史中LOGO已核驗1筆）、4筆來源待核銷，不當成新增具名開發小工作；未知日期不造值，不恢復執行。
- 原進度表「小工作總表」213筆登錄中197件具名工作（168完成、29未完成）；報告A～H全數完成，10頁15圖已交付原路徑。來源／去重與限制見[盤點](specs/20261027-all-task-progress/inventory.md)及[十頁規劃](specs/20261027-mes-spc-report-plan/ten-slide-plan.md)。最新入池同步依[Portal Grafana規劃驗證](../PmrPortal/specs/20261028-grafana-entry-plan/verification.md)，前次[截圖補充驗證](specs/20261028-chemical-screenshot-followup/verification.md)保留，前次[網站調整規劃驗證](specs/20261028-spc-adjustment-backlog/verification.md)保留，前次[Overall驗證](specs/20261028-spc-overall/verification.md)保留；舊計數只保留歷史或子集合意義。

## 2026-10-28 SPC 網站調整（只規劃，尚未開工）

主規格：[SPC-ADJUST-20261028](specs/20261028-spc-adjustment-backlog/spec.md)；[測試矩陣](specs/20261028-spc-adjustment-backlog/tasks.md)。已依5張截圖補充[異常規格與三件新增](specs/20261028-chemical-screenshot-followup/spec.md)；原月報004／005不重複入池。以下依建議優先順序逐件執行，本次不立即實作；既有暫停／受阻工作維持。

|順序|工作代號|優先／主系統|工作與相依|
|---|---|---|---|
|1|SPC-ADJUST-1028-TASK-008|P0／Portal，相依SPC|保存成功後歷史Failed to fetch；只重試查詢，避免重複保存。|
|2|SPC-ADJUST-1028-TASK-007|P0／Portal，相依SPC|N2開線未出現、收線載入失敗；先沿008診斷，避免重複修復。|
|3|SPC-ADJUST-1028-TASK-009|P1／SPC，相依Portal|QE2硫酸10.8→13.971／14.04及硝酸差異；核對公式／F表／版本與原始精度。|
|4|SPC-ADJUST-1028-TASK-001|P1／SPC|管制線公式儲存後重算重繪；手動界限與分段優先序待核。|
|5|SPC-ADJUST-1028-TASK-002|P1／Portal，相依SPC|查詢NG與複驗優先；依009確認分析值來源，0有效。|
|6|SPC-ADJUST-1028-TASK-003|P1／SPC|測試入口HTTPS根網址，保留Launch及正式目標。|
|7|SPC-ADJUST-1028-TASK-004|P2／SPC|Excel三個獨立Sheet：製程管制、藥液管制、藥液趨勢；不得只在同張分區。|
|8|SPC-ADJUST-1028-TASK-005|P2／SPC|各Sheet已處置件數／未處置件數，含處置中，沿Overall去重。|
|9|SPC-ADJUST-1028-TASK-006|P3／SPC|淺藍直方圖，不改bins／頻數／統計。|

若確認003已阻擋使用／導向錯環境，依既有排序規則升為第一；這是後續排序條件，本次仍不開工。不填未開始的實際開發日期；規劃父批次不另重複計數。

## 2026-10-28 Portal Grafana入口（僅規劃）

- [ ] `PORTAL-GRAFANA-1028-TASK-001`：P2，沿用首頁快速連結，建議名稱「3F 環境監控」，新分頁開啟指定Grafana URL且保留所有參數。[Portal主規格](../PmrPortal/specs/20261028-grafana-entry-plan/spec.md)／[中央引用](specs/20261028-portal-grafana-entry-sync/spec.md)。
- 後續排在原載入／計算／查詢及月報004／005之後、配色006之前；只規劃，不立即設定或實作。不改Grafana登入／儀表板、不新建API／schema；未來可能經既有admin管理新增1筆連結設定，正式仍另授權。
- 本件只計1件，不另計中央同步父件，其他暫停／受阻維持。

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

## 最新完成與未完成數（2026-10-21正式網站發布後）
- 005 KM正式1.6.7已完成；006 SPC正式0.1.68/0.1.70、Portal正式191/234及必要DB更新完成，備份/hash/正常SSO/公式歷史/唯讀製圖通過。[最終驗證](specs/20261021-spc-portal-production-preflight/verification.md)。
- 本批原6件現未完成2件：002附件掃毒因不外送政策未確認而受阻；004 Runtime由使用者明示暫緩，無18:00自動維護。001/003/005/006已完成不再列入。
- TransFiles明列舊驗收仍1件TASK-005，首筆LOG人工故障驗收註記保留。正常跨日業務匯入按既有驗收案例後續操作，不將正式receipt0冒稱已做人工追溯驗收。

## I18N 工作池（2026-10-26最新：07E2完成，08拆五批後剩18小工作）
- 使用者最新授權[連續執行](specs/20261026-i18n-continuation/spec.md)：逐件完成後直接接續，不再等待逐項核准；下列舊STOP只作歷史，不作現行等待要求。
- [後續任務](specs/20261022-i18n-task01/tasks.md)：06B再拆三批，剩08E、09～21共14小工作待執行/待確認；已完成01～04、05A～E、06A／B1追溯見CHANGELOG_CUSTOM.md及各specs。
- 每次單件、單一系統；下一項08D 郵件與背景通知。測試發布不等於正式更新授權。
- 既有主要未完成仍2件：SEC-FOLLOWUP-002附件掃毒受阻、004 Runtime暫緩；TransFiles舊人工驗收TASK-005另1件。i18n規劃包與既有受阻／暫緩事項分列，不重啟維護。
- 已完成08C6，依連續授權接續08E 人讀報表與匯出。

## 2026-10-22 落塵空圖工作池（僅規劃，不立即執行）
- [規格](specs/20261022-particle-empty-chart-plan/spec.md)／[計畫](specs/20261022-particle-empty-chart-plan/plan.md)／[任務](specs/20261022-particle-empty-chart-plan/tasks.md)：原檔有日期／Count格式問題，實際批次與入庫狀態尚未查；不以少於20點解釋所有0筆。
- [x] SPC-PARTICLE-EMPTY-TASK-001（P1，已完成）：先追蹤原匯入落點／錯誤／查詢及必要最小修正。
- [x] SPC-PARTICLE-EMPTY-TASK-002（P1，已完成）：測試子集1152筆、32／4點與9位置比較已驗證；API 0.1.69已發布測試站，資料保留供驗收。
- 本次不改原Excel／程式、不寫DB、不發布；完成規劃後STOP。新2件＋i18n20包＝22件待辦／待確認，原受阻及暫緩2件、TransFiles舊驗收1件分列，不恢復其他工作。

## 2026-10-22 落塵TASK-001完成
- SPC-PARTICLE-EMPTY-TASK-001：測試站／測試庫量測及Particle批次0；轉檔工具漏接預覽分支已最小修正，51 Python／12 SPC測試與來源patch重現通過，獨立新EXE交付；網站及正式站未改、未寫測試資料。[驗證](specs/20261022-particle-empty-chart-plan/task001-verification.md)。
- 本案TASK-002完成：[操作與資料](specs/20261022-particle-empty-chart-plan/task002-input-record.md)、[驗證](specs/20261022-particle-empty-chart-plan/task002-verification.md)。本案無剩餘小工作；i18n20包待確認／待辦，原受阻／暫緩2件及TransFiles舊人工驗收1件另列，不自動執行。

## 2026-10-22 U-chart實站測試（P1，待必要輸入）
- SPC-PARTICLE-U-ACCEPTANCE-TASK-001已完成，移出待辦：[操作與驗證](specs/20261022-particle-u-acceptance/input-record.md)。依使用者確認原始計數／28.3 L匯入1152筆，32點與單點U圖驗證通過，原C圖不變。現剩i18n20包，受阻／暫緩2件及舊人工驗收1件另列。

## SPC正式連線與API調整規劃（2026-10-22，僅規劃）
- [規格與核對](specs/20261022-production-connection-format-plan/spec.md)／[排序](specs/20261022-production-connection-format-plan/tasks.md)。正式SQL172.16.110.16/PMR_SPC_2026已連線確認，API0.1.68/DLL0.1.68.0一致，無已知格式錯誤。
- [x] SPC-PROD-CONFIG-TASK-001（P2，待具體調整對象／格式）：先明確現況與預期範例，再依阻斷/安全/資料正確/高頻/文案排序最小修正；本次只規劃，不立即執行。
- 新增待確認1件＋既有i18n20包＝21件待辦／待確認；既有受阻／暫緩2件、TransFiles舊人工驗收1件分列。規劃完成後STOP，不重啟其他工作。

- SPC-PROD-CONFIG-TASK-001具體化為P1：PPC重複阻擋定位與NULL/並發防護覆蓋審查；正式完整鍵重複0組，已有後端409及條件式唯一索引。待使用者新增/編輯情境與項目識別，僅規劃，不實作/修資料。仍1件＋i18n20包＝21件，另受阻/暫緩2、舊驗收1。

- SPC-PROD-CONFIG-TASK-001已取得2620案例，依使用者要求解決編輯受阻，執行最小修正：[規格](specs/20261022-ppc-optional-machine-edit/spec.md)。不開始其他工作；正式發布另行授權。

- SPC-PROD-CONFIG-TASK-001：2620根因定位為後端抹除選填線別；單行修正、47passed/1skip與測試API0.1.70完成。正式獨立0.1.71包及Plan/4項回歸通過，待正式授權與2620真人儲存；本件不標DONE。仍本件1＋i18n20包＝21件，另受阻/暫緩2、舊驗收1。

- 2026-10-22 SPC-PROD-CONFIG-TASK-001正式單行修正已同意並發布API0.1.71，101檔/3份設定hash、備份VERIFYONLY及health通過；只剩2620真人儲存確認1項。正式部署完成，整案待驗收不標DONE；計數仍本件1＋i18n20包＝21件，另受阻/暫緩2、舊驗收1。

## 最新工作池：P1結案（2026-10-22）
- [x] SPC-PROD-CONFIG-TASK-001：使用者回覆「P1已確認」，2620規格下限0.2儲存驗收完成；正式API0.1.71，本件DONE，不再列入未完成數。
- 目前剩i18n TASK-02～21共20個待確認工作包；附件掃毒受阻及Runtime暫緩2件、TransFiles舊人工驗收1件另列。Portal三項真人確認未因本次SPC驗收自動核銷。
- 本次僅文件結案，不重跑測試或發布，不開始下一工作。

## TransFiles人工故障驗收暫緩（2026-10-22）
- 使用者指定：TASK-005「自動化驗證通過，EXE 人工故障驗收暫緩」。既有59項通過，無隔離環境，不安排立即驗收、不標DONE。[規格](specs/20261021-transfiles-login-failure-validation/spec.md)。
- 工作池仍i18n20包待確認；另列附件掃毒受阻1件、Runtime暫緩1件、TransFiles人工故障驗收暫緩1件。Portal既有真人確認另列，不受本次影響。

## 常態分析固定AD（2026-10-22，僅規劃）
- [x] SPC-NORMALITY-AD-TASK-001（P2，待執行確認）：移除偏態/峰態、取消方法下拉，管制圖/量測趨勢固定Anderson-Darling，後端預設與測試同步。[規格](specs/20261022-normality-fixed-ad-plan/spec.md)／[排序與限制](specs/20261022-normality-fixed-ad-plan/tasks.md)。
- 本件排於i18n之前；本次不立即實作/測試/發布。現共21件＝本件1＋i18n20包；另附件掃毒受阻1、Runtime/TransFiles驗收暫緩2，不自動恢復。

## 常態分析固定AD完成（2026-10-23）
- [x] SPC-NORMALITY-AD-TASK-001：兩頁移除偏態/峰態與方法下拉、固定AD，後端預設同步且保留AJB明確呼叫；測試API0.1.72/Web0.1.71已發布。
- 234項後端通過/9條件式略過、Node3、兩頁8情境及已發布8情境/同源路由通過，無資料修補。正式未更新。[驗證與回復](specs/20261022-normality-fixed-ad-plan/verification.md)。
- 本件DONE，工作池由21降為i18n20個待確認包；另附件掃毒受阻1、Runtime及TransFiles人工故障驗收暫緩2，不開始其他工作。

## 國際化目前待辦（2026-10-27）
- SPC三語化開發及品質特性名稱A／B／C已完成；共13個未完成三語化小工作（Portal6＋KM4＋共用3），09～21維持暫停，真人審稿仍待TASK20。
- [SPC最新驗證/回復](specs/20261026-i18n-task08e/verification.md)：測試Web0.1.107／API0.1.85；全部譯文初稿，真人審核仍在TASK20。
- 另附件掃毒受阻1、Runtime與TransFiles人工故障驗收暫緩2；Portal既有真人確認另列；本次不恢復其他系統。

- 2026-10-24：05C依大小拆C1～C3，範圍不增加；C1完成後20件未完成；規格 `specs/20261024-i18n-task05c1/spec.md`。

- 2026-10-24 05C2完成紀錄見CHANGELOG_CUSTOM.md及 `specs/20261024-i18n-task05c2/verification.md`；剩19件，未自動開始C3。

- 2026-10-24 05C3完成紀錄見CHANGELOG_CUSTOM.md及 `specs/20261024-i18n-task05c3/verification.md`；剩18件，05D未開始。

- 2026-10-24 05D完成紀錄見CHANGELOG_CUSTOM.md及 `specs/20261024-i18n-task05d/verification.md`；剩17件，05E未開始。

## 2026-10-26 TASK-06 拆分與開工
- 大型圖表範圍拆06A共用分段版本／計算證據、06B管制圖、06C趨勢／統計、06D警示，範圍不增加；原16件改為19件可追蹤小工作。
- 本次依「開始下一項」只執行[06A](specs/20261026-i18n-task06a/spec.md)，B～D及07～21保留，不同時開工。
- 06A完成：104新鍵／312列draft，62單元、本機29＋舊分段17＋shell17、發布29＋17/build/hash/health/同源通過；測試Web0.1.80/API0.1.72，正式未改。完成後剩18件（06B～D＋07～21），另受阻1／暫緩2。

## 2026-10-26 06B拆分與開工
- 依頁面規模拆B1查詢／監控摘要／下載及排除清單介面、B2圖內標籤／直方圖／總覽表、B3點位備註／排除操作及OCAP表單，原授權範圍不增加。
- 原18件改為20個可追蹤小工作；本次只做[06B1](specs/20261026-i18n-task06b1/spec.md)，其餘保留待辦。
- 06B1完成：70新鍵／210列draft，65單元、本機38＋分段17＋shell17／發布38＋17/build/hash/同源/health通過，測試Web0.1.81/API0.1.72，正式未改。剩19件（06B2／B3／C／D＋07～21），另受阻1／暫緩2。

- 2026-10-26 06B2完成：[驗證](specs/20261026-i18n-task06b2/verification.md)。99新鍵／297列draft，67 i18n＋4 presentation單元、新29／查詢38／分段17／AD8回歸及測試發布通過。Web0.1.82/API0.1.72；剩18件，依連續授權接續B3。

- 2026-10-26 06B3完成：[驗證](specs/20261026-i18n-task06b3/verification.md)。98鍵三語，70單元／本機22＋查詢38／分段17、發布OCAP15＋查詢38通過（本機座標7例在發布站明示略過），build/hash/health/同源通過；Web0.1.83/API0.1.72，剩17件直接接續06C。

- 2026-10-26 06C完成：[驗證](specs/20261026-i18n-task06c/verification.md)。54新鍵／72單元＋5顯示工具、本機18及原分段17／AD8、發布18與build/hash/health/同源通過；Web0.1.84/API0.1.72。剩16件接續06D。

- 2026-10-26 06D完成：[驗證](specs/20261026-i18n-task06d/verification.md)。117新鍵／75單元、本機48與發布48、build/hash/health/同源通過；Web0.1.85/API0.1.72，剩15件接續07。

- 2026-10-26 TASK-07依實際模組拆A設備、B粒子、C校驗、D設定與使用者、E其餘查詢／導覽；目前19件，先[07A](specs/20261026-i18n-task07a/spec.md)，無新增功能。

- 2026-10-26 07A完成：[驗證](specs/20261026-i18n-task07a/verification.md)。73鍵／78單元、本機41／發布41及build/hash/health/同源通過；Web0.1.86/API0.1.72，剩18件接續07B。

- 2026-10-26 07B完成：[驗證](specs/20261026-i18n-task07b/verification.md)。28鍵／81單元、本機25／發布25及build/hash/health/同源通過；Web0.1.87/API0.1.72，剩17件接續07C。

- 2026-10-26 07C完成：[驗證](specs/20261026-i18n-task07c/verification.md)。199鍵／86單元、本機45＋原回歸14／發布45及build/hash/health/同源通過；Web0.1.88/API0.1.72，剩16件接續07D。

- 2026-10-26 07D完成：[驗證](specs/20261026-i18n-task07d/verification.md)。173鍵／91單元、本機51／發布51及build/hash/health/同源通過；Web0.1.89/API0.1.72，剩15件接續07E。

- 2026-10-26 07E按公開路由拆E1查詢／系譜／咬蝕入口、E2手冊／SSO，無新增功能；目前16件，先[07E1](specs/20261026-i18n-task07e1/spec.md)。

- 2026-10-26 07E1完成：[驗證](specs/20261026-i18n-task07e1/verification.md)。94鍵／95單元、本機40／發布40及build/hash/health/同源通過；Web0.1.90/API0.1.72，剩15件接續07E2。

- 2026-10-26 07E2完成：[驗證](specs/20261026-i18n-task07e2/verification.md)。127鍵／97單元、本機41／最終發布41及build/hash/health/同源通過；Web0.1.91/API0.1.72，剩14件接續08。

- 2026-10-26 TASK-08拆A資源契約、B安全提示、C業務驗證、D郵件通知、E人讀報表；目前18件，先[08A](specs/20261026-i18n-task08a/spec.md)，背景無明示語言維持繁中、不增加schema。

- 2026-10-26 08A完成：12鍵／101前端與25後端測試、本機12／shell17／發布12、build/hash/health/同源通過；只測試Web0.1.92/API0.1.73，剩17件接續08B。[驗證](specs/20261026-i18n-task08a/verification.md)。

- 2026-10-26 08B完成：13鍵／102前端、31 API與12隔離身分測試，本機15／發布15及build/hash/health/同源通過。只測試Web0.1.93/API0.1.74，剩16件接續08C。

- 2026-10-26 08C依驗證範圍拆C1管制項目、C2其餘主檔、C3公式／圖表／查詢、C4匯入／設備／校驗／設定、C5逐頁descriptor；無新增業務功能，目前20件。

- 2026-10-26 08C1完成：26鍵／103前端、26後端、本機18／發布18及build/hash/health/同源通過。只測試Web0.1.94/API0.1.75；LSL0.2編輯排除自己／真正重複拒絕回歸通過，剩19件接續C2。

- 2026-10-26 08C2完成：24鍵／105前端、30後端、本機24／發布24及build/hash/health/同源通過，原string body與text/plain相容、舊關聯清單保持；只測試Web0.1.95/API0.1.76，剩18件，接續08C3 公式／圖表與查詢。

- 2026-10-26 C3分A固定API回應、B服務例外／動態驗證，目前19件；依連續授權逐批完成。

- 2026-10-26 08C3A完成：60鍵／106前端、34後端、本機27／發布27及build/hash/health/同源通過，11控制器除顯示metadata外與原文完全相同；只測試Web0.1.96/API0.1.77，剩18件，接續08C3B 服務例外與動態驗證。

- 2026-10-26 08C3B完成：79鍵／108前端、64必要後端、公式基準15與TransFiles來源特徵10、本機33／發布33及build/hash/health/同源通過，9服務除例外metadata外全文不變；只測試Web0.1.97/API0.1.78，剩17件，接續08C4 匯入／設備／校驗／設定驗證。

- 2026-10-26 C4分A固定API、B服務驗證、C逐列錯誤警示，目前19件；依連續授權逐批完成。

- 2026-10-26 08C4A完成：86鍵／109前端、14本批API測試、本機36／發布36與build/hash/health/同源通過；校驗完整124項為122通過、2項本批前基準同樣失敗，未宣稱全綠；只測試Web0.1.98/API0.1.79，剩18件，接續08C4B 服務與欄位驗證。

## 校驗既有驗證差異（非三語化功能，2026-10-26）
- [ ] CAL-VERIFY-001：核對未建 SPC 主檔但持合法 Editor JWT、未明示頁面權限的校驗管理業務基準。原版 4c66c77c 與本批均回 200，舊測試預期 403；不在三語化中更動權限或改掉原斷言。主系統 SPC、相依 Portal 僅 claims 基準，若需修改另立規格／權限回歸，正式另授權。
- [ ] CAL-VERIFY-002：恢復既有校驗真範本驗收所需原始 Excel，檔案 docs/QA-2-003-02A 儀器設備校驗管制一覽表-2026年-品保 - 更新.xlsx 目前缺少；不偽造59列原始證據。其餘生成式範本／隔離API測試已驗證。
- 證據與本批前重現：[08C4A 驗證](specs/20261026-i18n-task08c4a/verification.md)。兩項保留待辦，不冒稱124項全通過；三語化依連續授權接續C4B。

- 2026-10-26 08C4B完成：55鍵／110前端、21必要API與設備來源3、本機39／發布39與build/hash/health/同源通過；校驗124為122通過、同2項既有差異保留；只測試Web0.1.99/API0.1.80，剩17件，接續08C4C 匯入逐列錯誤與警示。

- 2026-10-26 08C4C完成：67鍵／112前端、24核心與6隔離HTTP、本機45／發布45與build/hash/health/同源通過；DB錯誤／payload與稽核欄位保持，校驗125為123通過／同2既有差異；只測試Web0.1.100/API0.1.81，剩16件，接續08C5 逐頁即時錯誤descriptor接線。

- 2026-10-26 C5依風險拆A共用／四頁、B其餘與巢狀明細，剩17件，逐項驗證發布。

- 2026-10-26 08C5A完成：115前端、49錯誤切換／17共用／39主檔／56製程群組回歸、發布49與build/hash/health/同源通過；已存錯誤無新請求即時切語言；只測試Web0.1.101/API0.1.81，剩16件，接續08C5B 其餘頁面與巢狀DTO接線。

- 2026-10-26 08C5B完成：117前端、31即時錯誤／DTO、49相容API與17共用、193操作回歸通過；發布31與build/hash/health/同源通過；只測試Web0.1.102/API0.1.81，剩15件，接續08D 郵件與背景通知。

- 2026-10-26 D分A共用SMTP與品質警報、B校驗Email/Chat；剩16件，無新增業務功能。

- 2026-10-26 08DA完成：27鍵／118前端、31必要API加nullable郵件補驗10、藥液59及校驗郵件5、12郵件版面、SMTP3／發布32與build/hash/health/同源通過；只測試Web0.1.103/API0.1.82，剩15件，接續08DB 校驗Email／Chat與背景通知。

- 2026-10-26 08DB完成：12鍵／119前端、51校驗通知與16共用API、12郵件版面、發布32與build/hash/health/同源通過；背景狀態／重試／原DTO保持；只測試Web0.1.104/API0.1.83，剩14件，接續08E 人讀報表與匯出。

- 2026-10-26 新盤點08C6已知成功／統計提示仍原文，依高頻畫面優先接線後回E，剩15件；不改算法。

### REPORT-DATA-VERIFY-001：既有CPK匯出月份與資料範圍業務確認（獨立於三語化）
- 觀察：ReportsController的month目前用於標題／檔名；量測與指標query未依month篩選。屬本批前行為，尚非已確認的新業務需求。需另定統計範圍／台北月份與歷史相容規格後修正；三語化不順手改算法或資料來源。

- 2026-10-26 08C6完成：10鍵／120前端、47不同必要API分批、藥液59與跨專案公式25、本機8圖表／42PPC／6邊界及發布8圖表／2PPC通過；5檔原公式SQL逐字保持；只測試Web0.1.106/API0.1.84，剩14件，接續08E 人讀報表與匯出。

- 2026-10-27 08E完成：62新鍵／121前端、35必要API、藥液59與校驗郵件18、本機及發布各5個報表驗收、build/hash/health/同源14通過；只測試Web0.1.107/API0.1.85。SPC開發批次完成，剩09～21共13件維持暫停，真人審稿未完成。[驗證](specs/20261026-i18n-task08e/verification.md)。

## 2026-10-27 I18N-CHAR-001 品質特性名稱跟隨語言（只規劃）
- [規格／截圖對照與格式建議](specs/20261027-i18n-characteristic-names/spec.md)／[計畫](specs/20261027-i18n-characteristic-names/plan.md)／[任務](specs/20261027-i18n-characteristic-names/tasks.md)。
- P2／待執行：A共用選名＋主檔／管制項目／量測 → B API投影＋圖表／摘要／警報／追溯 → C人讀報表／郵件及全SPC驗收。每次只執行一件，先於未開始的Portal／KM一般UI工作；已確認阻擋使用／安全／資料正確性修正仍優先。
- 使用既有中英文主檔，英文優先英文、中文優先原中文，缺值回退；編輯雙欄與原DTO不變。不自動翻譯或改「比重／Proportion」，ValueLabel另案。
- 本次未實作、測試或發布；三語化未完成16件＝原13＋新增3；另掃毒受阻1／Runtime與TransFiles人工驗收暫緩2維持。

- 2026-10-27 I18N-CHAR-001-A完成：主檔／管制項目／量測名稱隨語系切換、缺值回退與代碼搜尋，原編輯雙欄／DTO／數值保留；124語系、發布105瀏覽器及4項補驗、build/hash/health/同源14通過。只測試Web0.1.108/API0.1.85，正式未更新；三語化剩15件，其中SPC B／C兩件，未自動開始。[驗證與回復](specs/20261027-i18n-characteristic-names/verification.md)。

- 2026-10-27 I18N-CHAR-001-B完成：圖表／摘要／警報／追溯品質特性名稱三語接線，additive英文欄位保留原名、代碼及歷史；125語系／32 API、本機16與警報補驗4、發布16加原144回歸、build/hash/health/同源14通過。只測試Web0.1.109/API0.1.86，正式未更新；三語化剩14件，其中SPC C一件。[驗證與回復](specs/20261027-i18n-characteristic-names/verification.md)。

- 2026-10-27 I18N-CHAR-001-C完成：人讀報表／郵件附件、藥液總覽與F表品質特性名稱切語；歷史快照／CSV與原計算不變。126語系／40必要API、本機5及發布74瀏覽器、build/hash/health/同源14通過；只測試Web0.1.110/API0.1.87，正式未更新。SPC本案A／B／C完成，剩09～21共13件維持暫停，真人審稿未完成。[驗證與回復](specs/20261027-i18n-characteristic-names/verification.md)。

## SPC進度圖Excel更新（2026-10-27，兩件完成）
- [規格與現況對照](specs/20261027-spc-progress-workbook-plan/spec.md)／[排序清單](specs/20261027-spc-progress-workbook-plan/tasks.md)。
- [x] SPC-PROGRESS-001-A／P2：SPC更新明細、狀態／日期／圖例配置；先執行，不改Excel。
- [x] SPC-PROGRESS-001-B／P2，依賴A：備份後更新Excel SPC甘特圖，保留其他專案及原生圖形，重開與視覺／內容驗證。
- 2026-10-27 SPC-PROGRESS-001：依進度表.xlsx原甘特圖與使用者確認，只規劃更新Excel SPC區。10階段現況／格式問題已對照，新增A狀態日期與圖例明細、B圖表更新及驗證兩件P2，A先B後；未改原檔或程式／DB／站台。原三語13件維持暫停，既有受阻／暫緩／資料差異不重複立案。本次新增未完成Excel小工作2件；與三語13件合計本輪追蹤15件，非整個歷史工作池總數。

- 2026-10-27 SPC-PROGRESS-001-A完成：使用者授權兩件P2直接完成／取代原檔，狀態、圖例及版面明細已定稿；B接續，Excel尚未覆蓋。

- 2026-10-27 SPC-PROGRESS-001-B完成：備份後直接取代原進度表.xlsx，現況／圖例／來源同表更新；保留其他專案及35原生圖形，重開、內容／圖形、兩頁列印與回復檢查通過。[驗證](specs/20261027-spc-progress-workbook-plan/verification.md)。Excel兩件均完成，剩0件；原三語13件仍暫停（Portal6／KM4／共用3），外部受阻1、Runtime／TransFiles暫緩2另列，既有校驗／報表差異保留，不自動續做。

- [x] SPC-PROGRESS-001-R／P1：使用者回報進度表無法開啟，優先修復交付相容性；範圍及驗收見specs/20261027-spc-progress-workbook-plan/repair.md。

- 2026-10-27 SPC-PROGRESS-001-R完成：已修正前件重複分頁／列印範圍造成的OpenXML格式回歸，SDK錯誤1降0，原路徑已取代；內容／35原生圖形、冪等、重開／列印檢查通過。Excel人工未執行；本案剩0、三語13件仍暫停。[根因及驗證](specs/20261027-spc-progress-workbook-plan/repair.md)。

- [x] SPC-PROGRESS-001-G／P1：實際更新2026日期及甘特條，取代前件僅文字版本；依specs/20261027-spc-progress-workbook-plan/actual-gantt.md執行。

- 2026-10-27 SPC-PROGRESS-001-G完成：依使用者確認2026實際進度，已更新24週日期、20個紀錄起迄日期，重畫10個SPC甘特條／里程碑，保留其他專案11圖形及原排程；OpenXML錯誤0、日期／錨點／重開與2頁列印通過，原檔已取代。本案剩0；三語13件仍暫停。[驗證](specs/20261027-spc-progress-workbook-plan/actual-gantt.md)。

- [x] SPC-PROGRESS-RULE-001／P2：將進度表同步納入SPC更新規則，依specs/20261027-spc-progress-update-rule/spec.md處理。

- 2026-10-27 SPC-PROGRESS-RULE-001完成：AGENTS及開發規則加入原進度表同步入口，規定實際日期／狀態／原生甘特圖、備份／OpenXML驗證後取代原檔；純文件無進度影響可記不適用。本次未改xlsx、無站台發布；本件剩0，三語13件仍暫停。[規則](docs/spc-progress-update-rules.md)。

- [x] SPC-PROGRESS-001-O／P2：SPC甘特圖只從10月開始顯示，詳specs/20261027-spc-progress-workbook-plan/october-axis.md。

- 2026-10-27 SPC-PROGRESS-001-O完成：SPC圖改從2026/10/1逐日顯示，裁切跨月條／略過期前里程碑，保留原始日期及其他專案；OpenXML 0錯誤、重開／日期圖形比對與2頁列印通過，原檔已取代。規則同步；本案剩0，三語13件仍暫停。[驗證](specs/20261027-spc-progress-workbook-plan/october-axis.md)。

- [x] PROGRESS-PORTAL-KM-001／P2：入口網站與KM現況、日期、甘特圖及共用更新規則同步，詳specs/20261027-cross-project-progress/spec.md。

- 2026-10-27 PROGRESS-PORTAL-KM-001完成：原Excel加入Portal／KM十月日期、甘特條及16項明細，規則與三系統入口同步。內容／圖形回歸、OpenXML0錯誤與3頁列印通過，原檔已取代。本件剩0；三語13件仍暫停，其他受阻／暫緩與資料差異維持。[驗證](specs/20261027-cross-project-progress/spec.md)。

- [x] PROGRESS-ALL-TASKS-001／P2：所有明確小工作與人工追溯同步原Excel小工作總表，依specs/20261027-all-task-progress/spec.md執行。

- 2026-10-27 PROGRESS-ALL-TASKS-001完成：原Excel新增可篩選小工作總表，193登錄／核心未完成19，原表與13圖形保持、公式／來源／OpenXML0錯誤／重開列印檢查通過。已備份並取代原檔；本件剩0，原待辦與暫停狀態不變。[驗證](specs/20261027-all-task-progress/spec.md)。


## MES暨SPC進度報告調整（2026-10-27，只規劃）
[規格](specs/20261027-mes-spc-report-plan/spec.md)／[逐頁建議](specs/20261027-mes-spc-report-plan/slide-plan.md)／[排序](specs/20261027-mes-spc-report-plan/tasks.md)。保留背景與6頁，每頁1截圖；本次不改PPT／網站，不立即執行。

- 2026-10-27 REPORT-PPT-001規劃完成：原6頁唯讀盤點、背景／每頁一圖／逐頁格式建議及A～E五件P2已入池。只更新小工作總表登錄，PPT／網站未改，5件皆未開始。核心未完成由19增24，三語13與原受阻／暫緩不變。[逐頁建議](specs/20261027-mes-spc-report-plan/slide-plan.md)／[待辦](specs/20261027-mes-spc-report-plan/tasks.md)。

## 2026-10-27 REPORT-PPT-001-A 完成
已核對現況並取得六頁對應證據圖，來源與限制見 specs/20261027-mes-spc-report-plan/screenshot-sources.md。原PPT未修改，B～E四件待執行；進度表已備份同步A完成，核心未完成23件（原19＋報告4），人工12／待核銷4另列。OpenXML原檔0錯誤、儲存格／圖形回歸、重開與列印目視通過。無程式／DB變更，網站發布不適用。

## 2026-10-27 REPORT-PPT-001-B 完成
PPT工作副本第2頁已更新十月十階段進度截圖、截至日及測試／待辦摘要，原背景／標題／頁尾保留；另五頁XML與渲染像素保持。PPT封裝／版面、OpenXML零錯誤、重開渲染通過；PowerPoint人工未做。原PPT待E取代。原進度表已備份同步B完成，核心未完成22（原19＋報告C～E三件），人工12／待核銷4另列；無程式／DB或站台發布。詳 specs/20261027-mes-spc-report-plan/verification.md。

## 2026-10-27 REPORT-PPT-001-C 完成
工作副本第1／3頁標題／現況文案及各1張截圖已更新，保留10/30報告日並標10/27資料截點；原背景／母片保持，另4頁XML與渲染像素不變。PPT封裝／幾何、重開渲染及OpenXML零錯誤通過；PowerPoint人工未做，原PPT待E取代。原進度表已備份同步C完成，核心未完成21（原19＋報告D／E兩件），人工12／待核銷4另列。無程式／DB或網站發布，未開始D。詳 specs/20261027-mes-spc-report-plan/verification.md。

## 2026-10-27 REPORT-PPT-001-D 完成
依多點截圖要求，工作副本第4～6頁各2張互補實際畫面，全稿9張，原背景／第1～3頁保持；AD／藥液複驗欄／咬蝕背面均唯讀採證，無量測寫入或儀器連線。PPT封裝／幾何／OpenXML及渲染通過；原進度表同步D完成及E九圖驗收，核心未完成20（原19＋報告E一件），人工12／待核銷4另列。收尾原PPT不在原路徑，調整前備份仍在，E需處理最後交付；九張原圖已由嵌入素材恢復採證資料夾。無程式／DB或站台發布，未開始E。詳 specs/20261027-mes-spc-report-plan/verification.md。

## 2026-10-27 REPORT-PPT-001 十頁擴充（只規劃）
依最新需求規劃10頁投影片（含封面），保留背景與A～D成果；新增校正／落塵、異常／報表、近期改善及中長期方向四頁，目標15張實際證據圖。新增F採證／G現況／H規劃三件P2，原E改最後十頁交付，均未開始，不立即改PPT或開發網站。原進度表已備份同步，202登錄／核心23未完成／163完成，另人工12及待核銷4；OpenXML零錯誤、內容／圖形／重開列印驗證通過。詳 specs/20261027-mes-spc-report-plan/ten-slide-plan.md；無DB或站台發布。
