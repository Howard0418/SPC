# 正式機更新工具、全專案 AI+BDD 與 KM 教育訓練系統規劃

功能 ID：WORK-POOL-20261007-02  
狀態：DONE（僅規劃與排程，未實作）  
日期：2026-10-07

## Goal

針對三個新方向建立系統設計、執行計畫與小工作排序：

1. 正式機程式碼更新工具：正式機在另一台主機，需要備份、更新 IIS 網站資料夾與驗證。
2. 全專案改成 AI + BDD：將 SPC 已導入的 AI+BDD 模式推廣到已接入 SDD 的所有專案。
3. KM 專案加入教育訓練系統功能。

本次只加入工作池，不立即執行。

## Scope

- 設計正式機更新工具的安全發布流程與分階段任務。
- 設計全專案 AI+BDD 導入策略。
- 設計 KM 教育訓練系統的模組、角色、資料與驗收方向。
- 更新 `TODO.md`、需求索引與變更紀錄。

## Out of Scope

- 不連線正式機，不備份正式站，不更新 IIS。
- 不修改任何專案的業務程式。
- 不建立資料庫 schema、migration、API 或 UI。
- 不發布測試站或正式站。

## Current Behavior

- SPC 已導入 SDD + BDD + AI Coding。
- KM 已建立本地 SDD + BDD 文件框架。
- 其他專案已接入共用 SDD，但尚未逐一補齊 AI+BDD 本地規則與 BDD 入口。
- SPC 已有 production 交付包與正式 IIS 發布紀錄，但尚未有標準化正式機更新工具。
- KM 尚未有教育訓練系統功能規格。

## Expected Behavior

- 正式機更新工具以「先規格/演練，再測試站，最後正式授權」方式推進。
- 全專案 AI+BDD 分批導入，不一次大改所有程式。
- KM 教育訓練系統先建立功能邊界、角色、資料模型與 BDD，再分階段實作。

## Business Rules

- 正式站仍須另行明確授權，不得因本次規劃自動發布。
- 正式機工具必須先備份現有 IIS 目錄與設定，更新後可回復。
- 全專案 AI+BDD 不補造歷史規格；新需求與後續修改逐案套用。
- KM 教育訓練系統需保留課程、教材、報名/指派、完成紀錄與查詢追溯。

## System Design

### 正式機程式碼更新工具

建議採 PowerShell 工具與 manifest 驅動：

- `release/production` 產出交付包與 manifest。
- 工具接收目標主機、站台名稱、來源包、目標 IIS 實體路徑、備份路徑與 smoke test URL。
- 執行前檢查：manifest、hash、環境標記、目標不是 `D:\Sites\PmrPortal`、IIS site/app pool 存在、磁碟空間、備份路徑可寫。
- 發布流程：停止 app pool 或放置 `app_offline.htm` → 備份現有資料夾與 IIS 設定 → 複製新版本 → 保留 appsettings/web.config/附件等環境檔 → 啟動 app pool → smoke test。
- 回復流程：停止 app pool → 還原備份 → 啟動 → smoke test。
- 產出證據：manifest、備份位置、檔案 hash、IIS 設定、smoke 結果、回復指令。

### 全專案 AI + BDD

建議分三批：

- 第一批：SPC、PmrPortal、TransFiles、KM，因需求最活躍且跨系統整合多。
- 第二批：Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice。
- 第三批：python-pypxlib 等工具型專案，採輕量版入口與 BDD 範本。

每個專案最小導入：

- 更新或建立 `AGENTS.md`。
- 建立 `features/` 或在既有 specs 補 BDD 區段。
- 更新需求索引或本地進度文件。
- 新增一個導入 spec/verification。
- 不修改業務程式。

### KM 教育訓練系統

建議模組：

- 課程管理：課程名稱、分類、講師、時數、有效期限、教材附件。
- 訓練指派：依人員、部門、職務或群組指派必修/選修。
- 報名與簽到：開課梯次、名額、報名狀態、簽到/補課。
- 測驗/認證：測驗題庫、及格分數、證書或完成紀錄。
- 進度查詢：個人待完成、主管/人事查詢、逾期提醒。
- 稽核報表：完成率、課程歷程、匯出 Excel。

角色建議：

- 一般使用者：查看課程、報名、上課紀錄與完成狀態。
- 講師/課程管理者：維護課程、梯次、教材、簽到與成績。
- 人事/admin：全域管理、指派、報表與權限。

## API Impact

本次無 API 影響。後續可能新增：

- 發布工具：不一定需要 API，可先用 PowerShell/CLI。
- KM：課程、教材、指派、報名、簽到、測驗、報表 API。

## Database Impact

本次無資料庫影響。KM 後續可能需要課程、梯次、教材、指派、報名、完成紀錄、測驗題目與成績表。

## UI Impact

本次無 UI 影響。KM 後續需新增教育訓練主頁、課程清單、管理頁、個人進度與報表頁。

## Acceptance Criteria

- AC-001：`TODO.md` 已加入正式機更新工具、全專案 AI+BDD、KM 教育訓練系統小工作。
- AC-002：每類需求有系統設計、執行計畫、初步 BDD 與風險。
- AC-003：需求索引與變更紀錄已同步。
- AC-004：本次未實作、未發布、未操作正式機。

## BDD Acceptance Criteria

### Scenario: 正式機更新工具排入工作池
Given 正式機在另一台主機且更新前需要備份
When 本次只做規劃
Then TODO 必須包含正式機更新工具的規格、演練、實作與正式授權任務

### Scenario: 全專案 AI+BDD 分批導入
Given 多個專案已接入 SDD
When 使用者要求全專案改成 AI+BDD
Then TODO 必須規劃分批導入，不得一次修改所有專案業務程式

### Scenario: KM 教育訓練系統排入工作池
Given KM 專案需要教育訓練功能
When 本次只做規劃
Then TODO 必須包含課程、指派、報名/簽到、完成紀錄與報表等小工作

## Risks

- 正式機更新工具涉及遠端主機、IIS、正式資料與回復，必須先在測試/演練環境驗證。
- 全專案 AI+BDD 若一次大改容易干擾多專案，需分批且文件優先。
- KM 教育訓練系統可能涉及人員主檔、權限、教材檔案與個資，需先定義資料來源與角色。
