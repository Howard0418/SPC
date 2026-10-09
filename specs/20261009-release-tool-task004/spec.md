# RELEASE-TOOL-TASK-004 測試站/沙盒演練與 rollback 驗證

日期：2026-10-09
狀態：DONE
主系統：SPC
相依系統：RELEASE-TOOL-TASK-002、RELEASE-TOOL-TASK-003
資料庫影響：無
發布影響：僅本機非正式 sandbox 演練；未操作正式站
Rollback：本 Task 已演練 `ReleaseBackupRestore.ps1 -Mode Restore`

## Requirement

使用非正式目標完整演練更新與回復，保留證據，確認正式機更新工具原型能在不操作正式機、不刪除資料的前提下完成備份、套用與 rollback。

## 演練範圍

- 建立非正式 sandbox 目標與 package。
- 執行 `ReleaseBackupRestore.ps1 -Mode Backup`。
- 執行 `ReleaseApplyPackage.ps1` 套用 package 並建立 app_offline。
- 執行 `ReleaseBackupRestore.ps1 -Mode Restore` 回復備份。
- 檢查回復後內容、環境設定與 evidence。

## 非本 Task 範圍

- 不連線正式機。
- 不操作正式 IIS。
- 不發布測試站或正式站。
- 不刪除 sandbox、暫存、app_offline 或 evidence 檔案。
- 不提交 sandbox 證據輸出。

## BDD 驗收

### Scenario: 非正式 sandbox 完整演練

Given 非正式 sandbox 網站資料夾與交付包
When 執行備份、套用與回復流程
Then 備份 manifest 與 hash evidence 已產生
And 套用 evidence 已產生
And 回復 evidence 已產生

### Scenario: 回復保留環境設定

Given sandbox 目標原本含測試環境設定
When 套用 package 後再執行回復
Then 目標環境設定回到備份版本
And 不使用 package 內的測試覆蓋設定

### Scenario: app_offline 不自動刪除

Given 更新流程建立 app_offline.htm
When 回復流程完成
Then app_offline.htm 仍保留
And 後續移除需由待刪除清單另行確認

### Scenario: 正式站未被操作

Given 本 Task 是 sandbox 演練
When 完成所有驗證
Then 未連線正式機
And 未操作正式 IIS
And 未發布正式站

## 驗證摘要

- Sandbox root：`release-staging/release-tool-task004/run-20261009-201622`
- BackupId：`sandbox-rollback-002`
- 備份：通過。
- 套用：通過。
- 回復：通過。
- 回復後 `index.html`：`sandbox v1`。
- 回復後 `appsettings.json`：測試環境設定 `Sandbox`。
- `app_offline.htm`：保留，未刪除。

## 後續拆分

下一個可執行小工作為 `RELEASE-TOOL-TASK-005`：正式機使用手冊與授權檢核。
