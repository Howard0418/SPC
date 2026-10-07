# SPC AI + BDD 開發模式導入

功能 ID：SPC-AI-BDD-20261007  
狀態：DONE  
日期：2026-10-07

## Goal

在既有 SDD 流程上，補齊 SPC 主專案的 BDD 驗收行為與 AI Coding 協作規則，讓後續開發依「需求 → 規格 → BDD → 實作 → 測試 → 驗證 → 進度紀錄」執行。

## Scope

- 更新 `AGENTS.md`，明確 SPC 採 SDD + BDD + AI Coding。
- 新增根層 `features/` 作為重要功能的 BDD 情境入口。
- 新增本規格與驗證紀錄。
- 更新 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`。

## Out of Scope

- 不修改任何現有業務功能。
- 不修改 API、資料庫、前端 UI 或部署設定。
- 不重寫既有歷史規格或測試。
- 不整理既有未追蹤檔案與測試輸出。

## Current Behavior

- SPC 已有 SDD 流程，所有新變更需先進 `specs/`。
- 既有規則要求核心邏輯先建測試，並在修改前記錄目的。
- 目前尚未有根層 `features/` 作為 BDD 驗收情境入口。
- `AGENTS.md` 已有一次一個小工作與 Token-saving 規則，但尚未明確列出 BDD 與 AI Coding 交付步驟。

## Expected Behavior

- 後續 SPC 開發 Task 必須先有 Spec，重要功能必須有 Given / When / Then 驗收條件。
- AI Coding 只依目前 Task 授權範圍讀檔、修改、測試與回報。
- 完成狀態須同時說明 Spec、BDD、測試、驗證、發布與風險。

## Business Rules

- 一次只能處理一個 Task。
- 不自動開始下一個 Task。
- 不順手重構未授權範圍。
- 涉及核心邏輯、資料正確性、權限、計算、匯入或發布的工作，需建立可驗證測試或明確人工驗收。
- 文件型流程導入不代表既有業務功能已完成 BDD 補件。

## Technical Impact

- 只新增/更新文件與 BDD 目錄入口。
- 不影響 build、runtime、IIS、資料庫 migration 或前端 bundle。

## API Impact

無 API 影響。

## Database Impact

無資料庫影響。

## UI Impact

無 UI 影響。

## Acceptance Criteria

- AC-001：`AGENTS.md` 明確描述 SDD + BDD + AI Coding 流程。
- AC-002：根層 `features/` 存在，可放置 BDD feature 或 Given / When / Then 情境。
- AC-003：本次流程導入有對應 Spec 與 verification。
- AC-004：需求索引與變更紀錄已同步。
- AC-005：未修改現有業務功能、API、資料庫或 UI。

## BDD Acceptance Criteria

### Scenario: 新 SPC 開發 Task 先建立 Spec
Given 使用者提出 SPC 開發或修正需求
When AI Coding 開始處理該需求
Then 必須先在 `specs/` 建立或更新對應 Spec，再進入實作

### Scenario: 重要功能具備 BDD 驗收
Given Task 影響 SPC 使用者流程、計算、匯入、權限或資料寫入
When 建立 Task 規格
Then 必須以 Given / When / Then 定義可驗收的行為條件

### Scenario: AI Coding 限定目前 Task 範圍
Given 目前只授權單一 SPC Task
When AI Coding 讀檔、修改與測試
Then 只能處理目前 Task 必要檔案與驗證，不得自動開始下一個 Task

## Risks

- 既有歷史規格未全面補 BDD；後續僅在新 Task 或使用者指定回補時逐步建立。
- BDD 情境品質取決於每次 Task 是否明確描述可觀察行為；阻擋性業務衝突仍需詢問使用者。
