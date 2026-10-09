# KM-TRAINING-TASK-003 SPC 工作池同步規格

日期：2026-10-09
狀態：DONE
主系統：SPC 工作池
相依系統：KM
資料庫影響：無
發布影響：文件型 Task，不發布
Rollback：還原本同步規格、TODO、需求與變更紀錄

## Requirement

依 TODO.md 順序執行 `KM-TRAINING-TASK-003：訓練指派、報名與簽到`，主要規格落在 KM 專案，SPC 僅同步工作池狀態、需求索引與變更紀錄。

## KM 產出

- `specs/tasks/TASK-014-training-assignment-registration-attendance.md`
- `features/training-assignment-registration-attendance.feature`
- `docs/training-assignment-registration-attendance.md`

## 驗收

Given `KM-TRAINING-TASK-002` 已完成
When KM 完成訓練指派、報名與簽到規格
Then SPC 工作池同步標示 `KM-TRAINING-TASK-003` 完成
And 文件說明此 Task 無程式、資料庫與發布影響

## 驗證

- 文件型 Task，build/test 不適用。
- 未讀取正式人員、部門或職務名單。
- 未刪除資料或檔案。
