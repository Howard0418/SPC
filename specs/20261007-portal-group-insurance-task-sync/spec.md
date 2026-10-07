# PORTAL-TASK-005 完成同步

## Goal
- 同步全系統小工作池狀態，將已完成的 Portal 團保專區自未完成清單移除。

## Scope
- 更新 `TODO.md` 未完成小工作池。
- 記錄 Portal 對應完成狀態與驗證摘要。

## Out of Scope
- 不修改 SPC/Portal 業務程式。
- 不發布 SPC。

## Current Behavior
- `TODO.md` 仍列出 `PORTAL-TASK-005`。

## Expected Behavior
- `TODO.md` 僅保留未完成小工作，下一個第一順位為 SPC 咬蝕 X- 規格。

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
- Given `PORTAL-TASK-005` 已在 Portal 完成並發布測試站，When 同步 SPC 工作池，Then `TODO.md` 不再列出 `PORTAL-TASK-005`。

## Risks
- 本規格只同步工作池；Portal 實作細節以 Portal 專案規格為準。
