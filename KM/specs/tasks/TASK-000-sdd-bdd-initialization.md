# TASK-000 SDD + BDD 開發框架初始化

## Goal

分析目前 KM 專案狀態，建立 SDD + BDD + AI Coding 開發框架與必要文件，讓後續工作能以 Spec、BDD 驗收、實作、測試、驗證與進度紀錄串接。

## Scope

- 盤點 KM 目前可見目錄。
- 建立 `KM/AGENTS.md`。
- 建立 `KM/specs/README.md` 與 `KM/specs/tasks/`。
- 建立 `KM/features/`。
- 建立 `KM/docs/progress.md` 與 `KM/docs/architecture.md`。
- 建立 `KM/tests/`。
- 同步本次文件型 Task 的進度與變更紀錄。

## Out of Scope

- 不修改 SPC、Portal、TransFiles 或 KM 既有業務功能。
- 不建立新 API、資料表、migration 或 UI。
- 不執行測試站或正式站發布。
- 不整理既有未追蹤檔案。

## Current Behavior

- `KM/` 目前僅觀察到 `testing/` 目錄，包含既有測試相關文件。
- `KM/specs/`、`KM/features/`、`KM/docs/` 與 `KM/tests/` 尚未存在。
- SPC 主專案已有共用 SDD 流程，但 KM 尚未有本地入口文件與 BDD 任務框架。

## Expected Behavior

- KM 具備可追溯的 SDD + BDD 工作入口。
- 後續每個 KM Task 都能在 `KM/specs/tasks/` 找到對應 Spec。
- 重要功能能在 `KM/features/` 或 Task Spec 中以 Given / When / Then 定義驗收行為。
- `KM/docs/progress.md` 可追蹤每個 Task 的狀態與驗證結果。

## Business Rules

- 一次只能處理一個 Task。
- Task 未完成 Spec 與 BDD 驗收前不得進入實作。
- 完成目前 Task 後必須 STOP，不自動開始下一個 Task。
- 文件型初始化不得改變既有業務行為。

## Technical Impact

- 僅新增文件與空目錄骨架。
- 不影響既有編譯、執行、部署或資料流。

## API Impact

無 API 影響。

## Database Impact

無資料庫影響。

## UI Impact

無 UI 影響。

## BDD Acceptance Criteria

### Scenario: 建立 KM SDD 入口
Given KM 專案尚未有本地 SDD + BDD 入口文件
When 執行 TASK-000 初始化
Then `KM/AGENTS.md` 必須定義一次一個 Task、Spec 先行、BDD 驗收與 STOP RULE

### Scenario: 建立 Task 規格位置
Given 後續 KM Task 需要對應 Spec
When 初始化完成
Then `KM/specs/README.md` 與 `KM/specs/tasks/` 必須存在

### Scenario: 建立 BDD 與進度紀錄位置
Given 重要功能需要 Given / When / Then 驗收條件
When 初始化完成
Then `KM/features/` 與 `KM/docs/progress.md` 必須存在並可追蹤狀態

## Test / Verification

- Happy Path：確認指定目錄與文件皆建立。
- Boundary Case：文件型 Task 不執行 build，但需明確標示不適用。
- Invalid Input：本 Task 不處理使用者未授權的功能需求或程式修改。
- Regression Risk：確認未修改既有業務程式與資料庫。

## Risks

- KM 既有 `testing/` 文件尚未納入新流程；後續需由使用者指定 Task 再逐步整理。
- 空目錄在 Git 不能直接追蹤；以 `.gitkeep` 保留 `features/`、`tests/` 與 `specs/tasks/`。

## Status

DONE：文件框架初始化完成；build、部署不適用。
