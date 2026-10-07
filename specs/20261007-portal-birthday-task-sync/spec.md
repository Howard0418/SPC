# PORTAL-TASK-004 完成同步

## Goal
- 同步全系統小工作池狀態，將已完成的 Portal 生日資料管理與登入生日通知自未完成清單移除。

## Scope
- 更新 `TODO.md` 未完成小工作池。
- 記錄 Portal 對應完成狀態與驗證摘要。

## Out of Scope
- 不修改 SPC/Portal 業務程式。
- 不發布 SPC。

## Current Behavior
- `TODO.md` 仍列出 `PORTAL-TASK-004`。

## Expected Behavior
- `TODO.md` 僅保留未完成小工作，下一個 Portal 小工作為 `PORTAL-TASK-005`。

## Business Rules
- 已完成小工作不留在未完成工作池。
- 完成紀錄查 Portal 與 SPC `CHANGELOG_CUSTOM.md`。

## Technical Impact
- 無。

## API Impact
- 無。

## Database Impact
- 無。

## UI Impact
- 無。

## Acceptance Criteria
- Given `PORTAL-TASK-004` 已在 Portal 完成並發布測試站，When 同步 SPC 工作池，Then `TODO.md` 不再列出 `PORTAL-TASK-004`。
- Given 使用者詢問下一個小工作，When 查看 `TODO.md`，Then 第一順位下一項為 `PORTAL-TASK-005`。

## Risks
- 本規格只同步工作池；Portal 實作細節以 Portal 專案規格為準。
