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

狀態：已完成 TASK-001～TASK-005；TASK-006 已建立 HTTP 測試單站，待 HTTPS binding/cert 才能完成 AC-007

待執行：

- T-006：補齊測試站 HTTPS binding/cert 與 mixed content 驗證；不發布正式站。
- T-007：執行登入、Portal SSO、JWT、SQL、401、403、Vue refresh、mixed content smoke。
- T-008：同步需求索引、變更紀錄與驗證證據。

### 第二順位：SPC 線別分析項目總覽公式版本記錄（資料正確性）

#### SPC-CHEM-OVERVIEW-VERSION-TASK-001：總覽頁公式修改共用藥液公式版本記錄與參照文件備註

狀態：待執行；主規格 `specs/20261007-chemical-overview-formula-versioning/spec.md`

範圍：線別分析項目總覽若修改藥液分析公式，必須與 SPC 管制項目設定頁共用同一個 `ChemicalAnalysisFormulaVersion` 版本記錄與回復功能，並在每筆版本記錄保存參照文件/修改依據備註；不得新增第二套版本邏輯。

#### SPC-CHART-POINT-REMARK-TASK-001：SPC 管制圖量測點備註

狀態：待執行；主規格 `specs/20261007-spc-chart-point-remarks/spec.md`

範圍：SPC 管制圖每個可定位量測點右鍵可新增/編輯備註；備註需可重新查詢顯示，不影響管制界線、OOC/OOS、Cpk 或既有點位排除/隱藏狀態。

### 第三順位：Portal 生日通知調整（個資與人事權限）

#### PORTAL-BIRTHDAY-REPLAN-TASK-001：權限管理加入出生年月日欄位

狀態：待規格；主規格 `specs/20261007-portal-birthday-replan/spec.md`

範圍：取消獨立生日資料維護專區；在人員/權限管理編輯表單加入出生年月日欄位，限制人事或授權管理角色維護，並保護個資可見性與稽核。

#### PORTAL-BIRTHDAY-REPLAN-TASK-002：人事專區生日通知設定頁

狀態：待 PORTAL-BIRTHDAY-REPLAN-TASK-001

範圍：在人事專區新增生日通知設定頁，可修改生日祝詞、上傳/替換生日祝賀圖片、預覽通知效果；需限制圖片格式、大小與儲存路徑。

#### PORTAL-BIRTHDAY-REPLAN-TASK-003：登入生日通知套用新版設定

狀態：待 PORTAL-BIRTHDAY-REPLAN-TASK-002

範圍：使用權限管理中的出生年月日判斷生日，登入時顯示人事設定的祝詞與圖片；驗證一般使用者只看到自己的生日通知。

#### PORTAL-GROUP-INSURANCE-CANCELLED：團保專區取消

狀態：取消；除非使用者重新提出，不再排入未完成小工作，不讀取或使用 `D:\PmrPortal\GroupInsurance`。

### 第四順位：全專案 AI + BDD 導入（流程治理）

#### AI-BDD-ALL-TASK-001：全專案導入盤點與共用範本

狀態：待規格

初步範圍：盤點 SPC、PmrPortal、TransFiles、KM、Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice、python-pypxlib 的 AI+BDD 狀態，建立共用段落、BDD 範本與 STOP RULE。

#### AI-BDD-ALL-TASK-002：第一批高頻專案導入

狀態：待 AI-BDD-ALL-TASK-001

範圍：PmrPortal、TransFiles、KM 與 SPC 對齊 AI+BDD 入口、features 目錄與導入紀錄；不補造歷史規格。

#### AI-BDD-ALL-TASK-003：第二批支援/工具專案導入

狀態：待 AI-BDD-ALL-TASK-002

範圍：Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice、python-pypxlib 依專案大小採輕量導入。

### 第五順位：KM 教育訓練系統（新功能）

#### KM-TRAINING-TASK-001：教育訓練系統需求規格與資料來源盤點

狀態：待規格

初步範圍：定義一般使用者、講師/課程管理者、人事/admin；定義課程、教材、梯次、指派、報名、簽到、測驗、完成紀錄與報表；盤點人員資料來源。

#### KM-TRAINING-TASK-002：課程與教材管理

狀態：待 KM-TRAINING-TASK-001

範圍：課程主檔、分類、講師、時數、教材附件、啟用/停用與版本紀錄。

#### KM-TRAINING-TASK-003：訓練指派、報名與簽到

狀態：待 KM-TRAINING-TASK-002

範圍：依人員/部門/職務指派，梯次報名，名額限制，簽到與補課紀錄。

#### KM-TRAINING-TASK-004：測驗、完成認定與個人進度

狀態：待 KM-TRAINING-TASK-003

範圍：題庫、測驗、及格分數、完成證明、個人待辦與完成歷程。

#### KM-TRAINING-TASK-005：管理報表與匯出

狀態：待 KM-TRAINING-TASK-004

範圍：完成率、逾期清單、課程歷程、部門統計與 Excel 匯出。

### 第六順位：正式機程式碼更新工具（正式發布/IIS 高風險）

#### RELEASE-TOOL-TASK-001：正式機更新工具規格與環境盤點

狀態：待規格；正式站操作需另行授權

初步範圍：盤點正式機連線方式、IIS site/app pool、實體路徑、備份路徑、服務帳號與權限；定義 release manifest、hash、版本、目標環境、來源包與 smoke test URL；本階段只規格，不連線正式機。

#### RELEASE-TOOL-TASK-002：備份與回復流程原型

狀態：待 RELEASE-TOOL-TASK-001

範圍：建立可在非正式目標演練的 PowerShell/CLI，支援備份網站資料夾、匯出 IIS 設定、hash 紀錄與回復。

#### RELEASE-TOOL-TASK-003：更新 IIS 網站資料夾流程

狀態：待 RELEASE-TOOL-TASK-002

範圍：app_offline/app pool 停啟、複製交付包、保留環境設定、移除暫停檔、smoke test。

#### RELEASE-TOOL-TASK-004：測試站/沙盒演練與 rollback 驗證

狀態：待 RELEASE-TOOL-TASK-003

範圍：使用非正式目標完整演練更新與回復，保留證據；不得操作正式機。

#### RELEASE-TOOL-TASK-005：正式機使用手冊與授權檢核

狀態：待 RELEASE-TOOL-TASK-004；正式執行需使用者另行授權

範圍：整理操作手冊、參數範本、前置檢查、回復步驟、證據格式與核准清單。

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

狀態：待確認

### TASK-003：診斷端點與錯誤資訊隔離

狀態：待確認

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
