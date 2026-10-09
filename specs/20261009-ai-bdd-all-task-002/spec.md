# 功能規格：AI-BDD-ALL-TASK-002 第一批高頻專案導入
- 功能 ID：AI-BDD-ALL-TASK-002
- 版本：1
- 狀態：完成
- 涉及專案：SPC、PmrPortal、TransFiles、KM
- 授權依據：`TODO.md` 第四順位 `AI-BDD-ALL-TASK-002`
- 現行需求基準：`docs/ai-bdd-guide.md`、`specs/20261009-ai-bdd-all-task-001/spec.md`
- 前案或相關規格：`specs/20261007-spc-ai-bdd-development/spec.md`

## 目的、現況與證據
目的：依 `AI-BDD-ALL-TASK-001` 的分批策略，先讓高頻專案 PmrPortal、TransFiles、KM 與 SPC 對齊 AI+BDD 入口、features 目錄與導入紀錄。

現況：
- SPC 已有完整 AGENTS 規則與 `features/` 入口，本次只補導入紀錄與工作池狀態。
- PmrPortal 已有 SDD 入口，但尚未明確列出 AI+BDD/STOP RULE 與 `features/` 入口。
- TransFiles 已有 SDD 入口，但尚未明確列出 AI+BDD/STOP RULE 與 `features/` 入口。
- KM 已有本地 SDD + BDD 框架，本次補共用指引引用與 `features/` 入口。

## 範圍與非範圍
範圍：
- PmrPortal：更新 AGENTS、需求索引、變更紀錄、規格與 `features/.gitkeep`。
- TransFiles：更新 AGENTS、需求索引、變更紀錄、規格與 `features/.gitkeep`。
- KM：補共用指引引用、需求索引、變更紀錄、任務規格與 `features/.gitkeep`。
- SPC：建立本主規格與驗證紀錄，同步 TODO、需求索引與變更紀錄。

非範圍：
- 不修改業務功能、資料庫、IIS、打包或發布流程。
- 不補造歷史規格。
- 不讀取機敏設定、正式資料或人員資料。
- 不執行建置、打包、測試站或正式站發布。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 第一批高頻專案需有 AI+BDD 入口 | AC-001 | PmrPortal、TransFiles、KM 均有本次導入規格或任務規格 |
| R-002 | 重要功能需能放置 BDD 情境 | AC-002 | PmrPortal、TransFiles、KM 均有 `features/.gitkeep` |
| R-003 | STOP RULE 不得覆蓋既有更嚴格規則 | AC-003 | 導入文字採補充方式，保留原 AGENTS 既有規則 |
| R-004 | 文件型導入不建置不發布 | AC-004 | 驗證紀錄標示建置、打包、發布不適用 |

## BDD Acceptance Criteria
### Scenario: Portal 導入 AI+BDD
Given PmrPortal 已接入 SDD  
When 第一批導入完成  
Then Portal AGENTS、需求索引、變更紀錄、features 入口與導入規格必須存在

### Scenario: TransFiles 導入 AI+BDD
Given TransFiles 已接入 SDD  
When 第一批導入完成  
Then TransFiles AGENTS、需求索引、變更紀錄、features 入口與導入規格必須存在

### Scenario: KM 保留既有框架並對齊共用指引
Given KM 已有 SDD + BDD 框架  
When 第一批導入完成  
Then KM 必須保留既有 STOP RULE，並引用共用 AI+BDD 指引與 BDD 範本

## 例外與邊界
- TransFiles 目前不是 Git repository，無法在該目錄提交；已更新檔案並在本驗證紀錄標示。
- KM 既有進度表使用盤點 Task ID；本次採 `TASK-011-ai-bdd-first-batch`，避免覆蓋既有 TASK-001。

## 假設與未決問題
- 假設：後續第二批支援/工具專案仍由 `AI-BDD-ALL-TASK-003` 處理。
- 未決問題：TransFiles 若需版本化提交，需先確認是否要建立或連接 Git repository。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-09 | 初版 |
