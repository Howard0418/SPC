# RELEASE-TOOL 待刪除項目

日期：2026-10-09

目前沒有執行任何刪除。

未來若備份或回復流程需要清理暫存、舊備份、舊發布包或 app_offline 檔案，需在實作前列入本文件或另開待刪除清單，經使用者確認後才可執行。

## 待使用者確認後才可刪除

- `app_offline.htm`：`RELEASE-TOOL-TASK-003` 的更新流程原型可建立暫停檔，但不自動刪除；若演練或正式流程需要移除，需由使用者另行確認。
- Release staging 暫存目錄：`ReleaseApplyPackage.ps1` 可能建立在系統暫存目錄的 staging 資料夾；清理前需先列出實際路徑並取得確認。
- `D:\SPC\release-staging\release-tool-task004\run-20261009-201622\site\app_offline.htm`：RELEASE-TOOL-TASK-004 sandbox 演練產生，依使用者規則未刪除。
- `D:\SPC\release-staging\release-tool-task004\run-20261009-201622`：RELEASE-TOOL-TASK-004 sandbox 演練資料與證據，未提交；若之後要清理需另行確認。
