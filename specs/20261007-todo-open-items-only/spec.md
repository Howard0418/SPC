# TODO 只顯示未完成小工作

功能 ID：TODO-OPEN-ONLY-20261007  
狀態：DONE  
日期：2026-10-07

## Goal

依使用者要求，將 `TODO.md` 改為只顯示未完成、待規格、待執行、待確認的小工作；已全數完成的小工作自工作池移除，完成紀錄改由 `CHANGELOG_CUSTOM.md` 與對應 `specs/` 保存。

## Scope

- 整理 `TODO.md`。
- 移除已完成的 SPC-1002、SPC 點位排除、SPC 藥液公式與 Portal 已完成小工作。
- 保留待規格、待執行、待確認項目。
- 更新需求索引與變更紀錄。

## Out of Scope

- 不修改任何業務程式。
- 不刪除既有 specs 或 changelog 完成紀錄。
- 不改變小工作實際狀態，只調整工作池顯示。

## Current Behavior

`TODO.md` 同時包含已完成與未完成小工作，查詢目前待辦時需要人工略過已完成區塊。

## Expected Behavior

`TODO.md` 只列出未完成/待確認小工作；完成項目不再顯示於工作池。

## Business Rules

- 小工作完成並同步文件後，須自 `TODO.md` 移除。
- 完成紀錄保留於 `CHANGELOG_CUSTOM.md` 與對應規格/驗證文件。
- 「已完成，待使用者確認」仍保留，直到使用者確認是否結案或轉後續任務。

## Technical Impact

僅文件整理，無技術執行影響。

## API Impact

無。

## Database Impact

無。

## UI Impact

無。

## Acceptance Criteria

- AC-001：`TODO.md` 標題與規則明確說明只顯示未完成小工作。
- AC-002：已全數完成的小工作不再出現在 `TODO.md`。
- AC-003：待規格、待執行、待確認項目仍保留。
- AC-004：需求索引與變更紀錄已同步。

## BDD Acceptance Criteria

### Scenario: 查詢目前小工作
Given 使用者詢問目前全系統小工作
When 讀取 `TODO.md`
Then 只會看到未完成、待執行或待確認的小工作

## Risks

- 已完成項目從 `TODO.md` 移除後，需改查 `CHANGELOG_CUSTOM.md` 或 specs 追溯細節。
