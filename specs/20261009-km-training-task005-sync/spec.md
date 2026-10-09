# KM-TRAINING-TASK-005 SPC 工作池同步規格

日期：2026-10-09
狀態：DONE
主系統：SPC 工作池
相依系統：KM
資料庫影響：無
發布影響：文件型 Task，不發布
Rollback：還原本同步規格、TODO、需求與變更紀錄

## Requirement

依 TODO.md 順序執行 `KM-TRAINING-TASK-005：管理報表與匯出`，主要規格落在 KM 專案，SPC 僅同步工作池狀態、需求索引與變更紀錄。

## KM 產出

- `specs/tasks/TASK-016-training-report-export.md`
- `features/training-report-export.feature`
- `docs/training-report-export.md`

## 驗收

Given `KM-TRAINING-TASK-004` 已完成
When KM 完成管理報表與匯出規格
Then SPC 工作池同步標示 `KM-TRAINING-TASK-005` 完成
And 文件說明此 Task 無程式、資料庫與發布影響

## 驗證

- 文件型 Task，build/test 不適用。
- 未讀取正式人員、部門、課程或完訓資料。
- 未刪除資料或檔案。
