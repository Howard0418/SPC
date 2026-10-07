# KM Specs

KM 採用 SDD + BDD + AI Coding 模式。

## 目錄

- `KM/specs/tasks/`：每個 Task 的 Spec 與驗證紀錄。
- `KM/features/`：重要功能的 BDD feature 或 Given / When / Then 驗收情境。
- `KM/docs/progress.md`：Task 狀態、驗證與完成紀錄。
- `KM/docs/architecture.md`：目前架構觀察與技術邊界。
- `KM/tests/`：KM 專用自動化測試或測試說明。

## Task 規則

- 一次只處理一個 Task。
- 每個 Task 先建立 Spec，再進入 BDD、實作、測試與驗證。
- 文件型 Task 可不執行 build，但需在驗證紀錄說明原因。
- 未完成或未驗證項目不可標示 DONE。

## Spec 範本

```markdown
# TASK-XXX 標題

## Goal

## Scope

## Out of Scope

## Current Behavior

## Expected Behavior

## Business Rules

## Technical Impact

## API Impact

## Database Impact

## UI Impact

## BDD Acceptance Criteria

### Scenario: ...
Given ...
When ...
Then ...

## Test / Verification

- Happy Path:
- Boundary Case:
- Invalid Input:
- Regression Risk:

## Risks

## Status
```
