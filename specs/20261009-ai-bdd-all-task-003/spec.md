# 功能規格：AI-BDD-ALL-TASK-003 第二批支援/工具專案導入
- 功能 ID：AI-BDD-ALL-TASK-003
- 版本：1
- 狀態：完成
- 涉及專案：Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice、python-pypxlib
- 授權依據：`TODO.md` 第四順位 `AI-BDD-ALL-TASK-003`
- 現行需求基準：`docs/ai-bdd-guide.md`、`specs/20261009-ai-bdd-all-task-001/spec.md`
- 前案或相關規格：`specs/20261009-ai-bdd-all-task-002/spec.md`

## 目的、現況與證據
目的：依 `AI-BDD-ALL-TASK-001` 的分批策略，讓第二批支援/工具專案採輕量 AI+BDD 入口，補上規格、features 入口、STOP RULE 與導入紀錄。

現況：
- 六個專案已接入 SDD，但尚未明確列出 AI+BDD/STOP RULE 與 `features/` 入口。
- Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice 目前在本工作區不是 Git repository。
- python-pypxlib 是 Git repository，已可提交；外部 GitHub 推送需使用者明確授權。

## 範圍與非範圍
範圍：
- 六個支援/工具專案各自補 `AGENTS.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`features/.gitkeep`、`specs/20261009-ai-bdd-second-batch/`。
- SPC 建立本主規格與驗證紀錄，同步 `TODO.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md`。

非範圍：
- 不修改業務功能、程式、資料庫、活頁簿、exe、DLL、Db、打包或發布流程。
- 不補造歷史規格。
- 不讀取機敏設定、正式資料或人員資料。
- 不執行建置、打包、測試站或正式站發布。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 第二批支援/工具專案需有輕量 AI+BDD 入口 | AC-001 | 六個專案均有導入規格與 `features/.gitkeep` |
| R-002 | STOP RULE 需保留各工具特性 | AC-002 | AGENTS 補充各自高風險點，例如儀器、DB、活頁簿、輸出格式 |
| R-003 | 文件型導入不建置不發布 | AC-003 | 驗證紀錄標示建置、打包、發布不適用 |
| R-004 | Git 狀態需如實記錄 | AC-004 | 非 Git 專案與待授權推送狀態列入驗證紀錄 |

## BDD Acceptance Criteria
### Scenario: 支援工具採輕量 AI+BDD
Given 支援/工具專案已接入 SDD  
When 第二批導入完成  
Then 每個專案必須有 AI+BDD 規則、STOP RULE、features 入口與導入規格

### Scenario: 不修改工具或資料
Given 本次是文件型導入  
When 完成第二批導入  
Then 不得修改程式、資料庫、活頁簿、exe、DLL、Db 或發布設定

## 例外與邊界
- Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice 不是 Git repository，檔案已在工作區更新但無法提交。
- python-pypxlib 已本地提交；推送外部 GitHub remote 需使用者明確授權。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-09 | 初版 |
