# SPC 咬蝕 X- 任務暫緩排序同步

## Goal
- 依使用者指示，將 `SPC-ETCH-X-TASK-001/002` 暫緩，不列為下一個執行順位。

## Scope
- 更新 `TODO.md` 排序與狀態。
- 記錄暫緩原因為需求尚未想完整。

## Out of Scope
- 不撰寫咬蝕 X- 詳細規格。
- 不修改 SPC/Portal 程式。

## Current Behavior
- 咬蝕 X- 任務列為第一順位。

## Expected Behavior
- 咬蝕 X- 任務移至暫緩區，下一個可執行順位為單一 IIS Site。

## Business Rules
- 使用者未確認需求前，不開始咬蝕 X- 規格或實作。

## Technical Impact
- 無。

## API Impact
- 無。

## Database Impact
- 無。

## UI Impact
- 無。

## Acceptance Criteria
- Given 使用者指定 1、2 先跳過，When 更新工作池，Then `SPC-ETCH-X-TASK-001/002` 狀態標示暫緩。
- Given 使用者詢問下一個可做小工作，When 查看 `TODO.md`，Then 第一順位為單一 IIS Site。

## Risks
- 咬蝕 X- 需求未定期間不應安排相關實作。
