# PORTAL-TASK-003 工作池狀態同步

功能 ID：PORTAL-TASK-003-SYNC-20261007  
狀態：DONE  
日期：2026-10-07

## Goal

依使用者確認準備執行 `PORTAL-TASK-003：公告發佈格式自動帶組織單位` 時，核對發現 PmrPortal 已有完成規格與發布紀錄，因此同步 SPC 工作池，將該項自未完成小工作移除。

## Scope

- 更新 `TODO.md`，移除 `PORTAL-TASK-003`。
- 同步需求索引與變更紀錄。

## Out of Scope

- 不重做 Portal 公告主旨功能。
- 不修改 Portal 程式。
- 不發布 Portal 或 SPC。

## Current Behavior

SPC `TODO.md` 仍顯示 `PORTAL-TASK-003` 待規格。

## Expected Behavior

該功能已在 PmrPortal 規格 `specs/20261006-announcement-title-organization-prefix/spec.md` 記錄完成與測試站 API 發布，SPC `TODO.md` 不再列為未完成。

## Business Rules

- 已完成小工作不顯示在 `TODO.md`。
- 跨專案工作池狀態需以實際專案規格/變更紀錄校正。

## Technical Impact

僅文件整理。

## API Impact

無。

## Database Impact

無。

## UI Impact

無。

## Acceptance Criteria

- AC-001：`TODO.md` 不再顯示 `PORTAL-TASK-003`。
- AC-002：需求索引與變更紀錄已同步。
- AC-003：未修改 Portal/SPC 程式。

## BDD Acceptance Criteria

### Scenario: 已完成的跨專案小工作移出工作池
Given Portal 對應規格已完成
When SPC 工作池同步狀態
Then `TODO.md` 不再列出該待辦

## Risks

- 若 Portal 實站仍需真人驗收，應另開「人工驗收」小工作，而非重做功能。
