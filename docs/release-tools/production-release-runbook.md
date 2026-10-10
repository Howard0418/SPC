# SPC 正式機更新工具使用手冊與授權檢核

版本：1.0
日期：2026-10-10
適用範圍：SPC 正式機程式碼更新前的人工檢核與操作紀錄

## 使用前提

本手冊只提供正式發布前的檢核與操作格式，不代表已取得正式站執行授權。正式站備份、套用、回復、app_offline 移除與任何清理動作，都必須由使用者另行明確授權。

禁止將 SPC 發布目標指向 `D:\Sites\PmrPortal`。文件與證據不得記錄帳密、憑證、token、正式連線字串或其他機敏設定值。

## 正式發布核准清單

| 檢核項目 | 必填內容 | 結果 |
| --- | --- | --- |
| 授權人 | `<AUTHORIZED_USER>` | 待填 |
| 授權時間 | `<YYYY-MM-DD HH:mm:ss>` | 待填 |
| 授權範圍 | `<SPC production backend/frontend release>` | 待填 |
| releaseId | `<SPC-YYYYMMDD-NNN>` | 待填 |
| source commit | `<GIT_COMMIT>` | 待填 |
| package path | `<PACKAGE_PATH>` | 待填 |
| package sha256 | `<PACKAGE_SHA256>` | 待填 |
| target environment | `Production` | 待填 |
| IIS site | `<IIS_SITE_NAME>` | 待填 |
| IIS app pool | `<IIS_APP_POOL_NAME>` | 待填 |
| physical path | `<SPC_PRODUCTION_SITE_PATH>` | 待填 |
| backup root | `<BACKUP_ROOT>` | 待填 |
| preserved files | `<PRESERVED_FILES>` | 待填 |
| smoke URLs | `<SMOKE_URLS>` | 待填 |
| rollback owner | `<ROLLBACK_OWNER>` | 待填 |

任一項缺漏、目標環境不一致、來源包 hash 不一致、目標路徑不是 SPC 正式站、或目標路徑為 `D:\Sites\PmrPortal` 時，停止執行。

## 前置檢查

1. 確認使用者已明確授權正式站發布，且授權範圍只包含 SPC。
2. 確認 release manifest 的 releaseId、commit、package path、sha256、target environment、IIS site、app pool、physical path、backup root 與 smoke URLs 完整。
3. 確認 package hash 與 manifest 相符。
4. 確認目標 physical path 為 SPC 正式站路徑，且不是 `D:\Sites\PmrPortal`。
5. 確認 preserved files 包含正式站環境設定、web.config、憑證或上傳目錄等需要保留的項目。
6. 確認 backup root 可寫入，且本次 releaseId 尚未被使用。
7. 確認 smoke test URL 不會回傳或保存機敏內容。
8. 確認 rollback owner 可在發布失敗時立即執行回復。

## 參數範本

正式執行前需以授權時提供的安全參數取代 placeholder。不得把含機敏值的實際命令提交到 Git。

```powershell
$releaseId = "<SPC-YYYYMMDD-NNN>"
$environmentName = "Production"
$sitePath = "<SPC_PRODUCTION_SITE_PATH>"
$backupRoot = "<BACKUP_ROOT>"
$packagePath = "<PACKAGE_PATH>"
$iisSiteName = "<IIS_SITE_NAME>"
$appPoolName = "<IIS_APP_POOL_NAME>"
$smokeUrls = @(
  "<PRODUCTION_BASE_URL>/api/version",
  "<PRODUCTION_BASE_URL>/"
)
```

目前 `ReleaseBackupRestore.ps1` 與 `ReleaseApplyPackage.ps1` 原型會阻擋 Production 環境名稱，正式執行版必須在另一次已授權小工作中調整，且調整前需保留相同的 Portal 路徑阻擋、manifest/hash 檢核與證據輸出。

## 建議操作順序

1. 記錄授權資訊與 release manifest。
2. 產生來源包 hash，與 manifest 比對。
3. 建立正式站備份，包含網站資料夾、IIS 設定匯出與檔案 hash。
4. 檢查備份 evidence，確認 backupId 與 releaseId 可對應。
5. 套用交付包，保留正式環境設定。
6. 執行 smoke test：API version、首頁、新前端資產、未登入 401/403、必要登入流程。
7. 記錄發布 evidence、smoke test 結果與版本資訊。
8. 若 smoke test 失敗或授權者要求回復，立即依 rollback 步驟回復。
9. 任何 app_offline 移除、暫存清理或舊備份清理，另列待刪除清單等待確認。

## Rollback 步驟

1. 停止繼續套用新 package，保留現場 evidence。
2. 找出同一 releaseId 對應的 backupId。
3. 使用該 backupId 回復網站資料夾與必要設定。
4. 重新執行 smoke test。
5. 記錄 rollback evidence、回復後版本、HTTP 狀態與未完成項目。
6. 若回復仍失敗，停止後續變更並回報授權者，不得改用未記錄的手動刪除或覆寫。

## Evidence 格式

```json
{
  "releaseId": "<SPC-YYYYMMDD-NNN>",
  "action": "backup|apply|rollback|smoke",
  "startedAt": "<UTC_TIMESTAMP>",
  "finishedAt": "<UTC_TIMESTAMP>",
  "operator": "<OPERATOR_ALIAS>",
  "target": {
    "environment": "Production",
    "siteName": "<IIS_SITE_NAME>",
    "appPoolName": "<IIS_APP_POOL_NAME>",
    "physicalPath": "<SPC_PRODUCTION_SITE_PATH>"
  },
  "package": {
    "path": "<PACKAGE_PATH>",
    "sha256": "<PACKAGE_SHA256>",
    "sourceCommit": "<GIT_COMMIT>"
  },
  "backup": {
    "backupId": "<BACKUP_ID>",
    "backupRoot": "<BACKUP_ROOT>"
  },
  "smokeTests": [
    {
      "name": "version",
      "urlAlias": "production-api-version",
      "expectedStatus": 200,
      "actualStatus": 200,
      "result": "pass"
    }
  ],
  "pendingDeletions": [
    "<APP_OFFLINE_OR_TEMP_ITEM_REQUIRING_CONFIRMATION>"
  ],
  "notes": "<NO_SECRETS>"
}
```

## 停止條件

- 未取得正式站明確授權。
- manifest 缺少 releaseId、hash、commit、目標環境、IIS 目標或 smoke test。
- package hash 與 manifest 不一致。
- 目標不是 SPC，或目標路徑為 `D:\Sites\PmrPortal`。
- 備份失敗或 backup evidence 不完整。
- preserved files 無法確認。
- smoke test 需要記錄機敏 response body 才能判讀。
- rollback owner 不可用。

## 發布後紀錄

正式發布完成後需同步：

- `CHANGELOG_CUSTOM.md`：發布內容、備份、驗證、正式站狀態。
- `docs/requirements.md`：若本次發布改變有效需求或狀態。
- 對應 `specs/<release-task>/verification.md`：授權、操作、smoke test 與 rollback 狀態。
- 待刪除清單：app_offline、暫存目錄、舊備份或其他需人工確認刪除項目。
