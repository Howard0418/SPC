# KM-TRAINING-TASK-002 SPC 工作池同步規格

日期：2026-10-09
狀態：DONE
主系統：SPC 工作池
相依系統：KM
資料庫影響：無
發布影響：文件型 Task，不發布
Rollback：還原本同步規格、TODO、需求與變更紀錄

## Requirement

依 TODO.md 順序執行 `KM-TRAINING-TASK-002：課程與教材管理`，主要規格落在 KM 專案，SPC 僅同步工作池狀態、需求索引與變更紀錄。

## KM 產出

- `specs/tasks/TASK-013-training-course-material-management.md`
- `features/training-course-material.feature`
- `docs/training-course-material-management.md`

## 驗收

Given `KM-TRAINING-TASK-001` 已完成
When KM 完成課程與教材管理規格
Then SPC 工作池同步標示 `KM-TRAINING-TASK-002` 完成
And 文件說明此 Task 無程式、資料庫與發布影響

## 驗證

- 文件型 Task，build/test 不適用。
- 未讀取正式人員或講師名單。
- 未刪除資料或檔案。
