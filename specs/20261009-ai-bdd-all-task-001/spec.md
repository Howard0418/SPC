# 功能規格：全專案 AI + BDD 導入盤點與共用範本
- 功能 ID：AI-BDD-ALL-TASK-001
- 版本：1
- 狀態：完成
- 涉及專案：SPC、PmrPortal、TransFiles、KM、Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice、python-pypxlib
- 授權依據：`TODO.md` 第四順位 `AI-BDD-ALL-TASK-001`
- 現行需求基準：`docs/sdd-workflow.md`、`docs/sdd-projects.md`、`docs/requirements.md`
- 前案或相關規格：`specs/20261007-release-ai-bdd-km-training-plan/spec.md`、`specs/20261007-spc-ai-bdd-development/spec.md`、`KM/specs/tasks/TASK-000-sdd-bdd-initialization.md`

## 目的、現況與證據
目的：將已接入 SDD 的十個專案先完成 AI + BDD 導入盤點，建立共用段落、BDD 範本與 STOP RULE，作為後續分批導入的穩定入口。

現況：
- `docs/sdd-projects.md` 已列出十個專案均已接入共用 SDD。
- SPC 已在 `AGENTS.md` 補入 SDD + BDD + AI Coding 規則。
- KM 已建立本地 SDD + BDD 框架與 STOP RULE。
- 其他專案仍以 SDD 入口為主，尚待後續 TASK-002/TASK-003 分批對齊。

本次只做流程文件與盤點，不修改任何業務功能、資料庫、IIS 或正式/測試站。

## 範圍與非範圍
範圍：
- 建立全專案 AI + BDD 共用指引。
- 建立 BDD feature 範本。
- 建立十專案導入盤點表與分批順序。
- 定義 STOP RULE，避免 AI Coding 擴張範圍或在未完成規格時進入實作。
- 同步 `TODO.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md`。

非範圍：
- 不修改各專案 AGENTS.md。
- 不建立或修改各專案 features 目錄。
- 不補造歷史規格或歷史 BDD。
- 不讀取機敏設定、憑證、正式資料或人員資料。
- 不執行建置、測試站發布或正式站操作。

## 導入盤點
| 專案 | 目前狀態 | 本次判定 | 後續任務 |
|---|---|---|---|
| SPC | SDD + BDD + AI Coding 已進入 AGENTS.md | 可作為共用範本來源 | 持續逐 Task 執行 |
| PmrPortal | 已接入 SDD，近期 Portal 生日任務已用規格與 BDD 驗收 | 第一批高頻專案 | TASK-002 |
| TransFiles | 已接入 SDD | 第一批高頻專案 | TASK-002 |
| KM | 已建立 SDD + BDD + AI Coding 框架 | 第一批高頻專案，但需避免與教育訓練功能混在同一 Task | TASK-002 |
| Chameleon | 已接入 SDD | 第二批支援/工具專案 | TASK-003 |
| DH_Temperature | 已接入 SDD | 第二批支援/工具專案 | TASK-003 |
| PMR_ERP撈取工單 | 已接入 SDD | 第二批支援/工具專案 | TASK-003 |
| DS2000 | 已接入 SDD，範圍限 Ri320Bridge | 第二批支援/工具專案 | TASK-003 |
| Voice | 已接入 SDD，僅文件入口 | 第二批支援/工具專案 | TASK-003 |
| python-pypxlib | 已接入 SDD | 第二批支援/工具專案 | TASK-003 |

## 共用段落
後續各專案可依專案語氣納入以下段落：

```markdown
## SDD + BDD + AI Coding
- 新增或修改功能時遵循 Requirement → Spec → BDD → Implementation → Test → Verify → 文件同步。
- 每個 Task 必須先有對應 specs/ 規格；重要功能需以 Given / When / Then 描述可觀察驗收行為。
- AI Coding 只處理目前 Task 必要檔案、測試與文件；不得順手重構未授權範圍或自動開始下一個 Task。
- 涉及資料寫入、權限、計算、匯入、發布或跨系統契約時，驗收需能對應自動化測試或明確人工驗證。
- 未完成 Spec、BDD、必要驗證與文件同步，不得標示 DONE。
```

## STOP RULE
詳見 `docs/ai-bdd-guide.md`。摘要：
- 沒有對應規格時停止實作，先補規格。
- 需求影響正式站、schema migration、角色權限、SSO/AD、IIS binding、機敏資料或正式資料時，停止並確認授權。
- 需要刪除資料或檔案時，停止；依使用者規則改寫待刪清單 MD。
- 同一 Task 之外的重構或新功能，不納入本次。
- 測試或發布失敗時，不標示 DONE；記錄阻擋與下一步。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 全專案 AI+BDD 導入需先完成盤點，不一次修改所有專案 | AC-001 | 規格列出十專案狀態與 TASK-002/TASK-003 分批順序 |
| R-002 | 後續專案可引用同一套共用段落與 STOP RULE | AC-002 | `docs/ai-bdd-guide.md` 存在並包含共用段落、STOP RULE、完成定義 |
| R-003 | 重要功能 BDD 情境需有統一格式 | AC-003 | `docs/templates/sdd/bdd-feature.md` 存在並提供 Given/When/Then 範本 |
| R-004 | 文件型 Task 不執行建置或發布，但仍需文件驗證 | AC-004 | 驗證紀錄標示建置/發布不適用，並完成連結/內容檢查 |

## BDD Acceptance Criteria
### Scenario: 建立全專案導入盤點
Given 十個專案已接入 SDD  
When 執行 AI-BDD-ALL-TASK-001  
Then 規格必須列出每個專案目前狀態、分批導入順序與後續任務

### Scenario: 建立共用 STOP RULE
Given AI Coding 可能跨越目前 Task 範圍  
When 建立共用導入指引  
Then 指引必須明確列出何時停止、何時只記錄待辦、何時需要使用者授權

### Scenario: 建立 BDD 範本
Given 後續專案需要可複用的 BDD 格式  
When 建立共用範本  
Then 範本必須包含 Feature、Background、Scenario、Given、When、Then 與驗證對應欄位

## 例外與邊界
- 本次盤點以現有文件為準，不讀取各專案機敏設定。
- 未確認專案不得宣稱已完成 AI+BDD 導入；僅標示待 TASK-002/TASK-003。
- 若專案已有更嚴格規則，後續導入不得覆蓋既有規則。

## 假設與未決問題
- 假設：後續 TASK-002/TASK-003 仍會逐專案建立或更新本地入口，不在本次一次完成。
- 未決問題：各專案是否需要完整 features 目錄或只在 specs 內寫 BDD，留待分批導入時依專案規模決定。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-09 | 初版 |
