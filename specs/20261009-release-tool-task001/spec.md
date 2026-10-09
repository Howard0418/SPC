# RELEASE-TOOL-TASK-001 正式機更新工具規格與環境盤點

日期：2026-10-09
狀態：DONE
主系統：SPC
相依系統：IIS、正式站主機、發布來源包
資料庫影響：無
發布影響：文件型 Task，不連線正式機、不發布
Rollback：還原本規格、驗證、TODO、需求與變更紀錄

## Requirement

建立正式機程式碼更新工具的前置規格與環境盤點欄位，涵蓋連線方式、IIS site/app pool、實體路徑、備份路徑、服務帳號與權限、release manifest、hash、版本、目標環境、來源包與 smoke test URL。

本 Task 僅定義規格與盤點表，不連線正式機、不查詢正式 IIS、不讀取正式設定、不執行備份或發布。

## 高風險邊界

- 正式站操作需使用者另行明確授權。
- 不可發布到 `D:\Sites\PmrPortal`。
- 不記錄帳密、憑證、token、連線字串或任何機敏設定值。
- 正式路徑、主機、服務帳號與 URL 在文件中只允許使用 placeholder，實際值需於授權執行時由操作者在安全通道提供。
- 本 Task 不刪除任何資料；未來若工具需要清理舊備份或暫存檔，需另列待刪除 MD 供使用者確認。

## 環境盤點欄位

| 類別 | 欄位 | 說明 |
| --- | --- | --- |
| 目標環境 | EnvironmentName | `Production`、`Staging` 或沙盒名稱 |
| 連線方式 | ConnectionMethod | 例如遠端 PowerShell、RDP、手動複製；本 Task 不實測 |
| 連線目標 | HostAlias | 使用別名或代號，不記錄真實主機機敏資訊 |
| IIS Site | SiteName | 目標 IIS Site 名稱 |
| IIS App Pool | AppPoolName | 目標 App Pool 名稱 |
| 實體路徑 | PhysicalPath | 目標網站根目錄；規格中使用 `<PRODUCTION_SITE_PATH>` |
| 備份路徑 | BackupRoot | 備份根目錄；規格中使用 `<BACKUP_ROOT>` |
| 服務帳號 | ServiceAccountAlias | 只記錄代號與權限需求，不記錄帳密 |
| 權限需求 | RequiredPermissions | 讀取、寫入、App Pool 控制、IIS 設定匯出 |
| 來源包 | PackagePath | 待發布 package 的本機或交付位置 |
| 設定保留 | PreservedFiles | 例如 appsettings、web.config、憑證、上傳目錄 |
| Smoke Test | SmokeUrls | 健康檢查、首頁、API version、登入前 401/403 |

## Release Manifest 欄位

```json
{
  "manifestVersion": "1",
  "releaseId": "SPC-YYYYMMDD-NNN",
  "system": "SPC",
  "targetEnvironment": "<TARGET_ENVIRONMENT>",
  "package": {
    "path": "<PACKAGE_PATH>",
    "sha256": "<PACKAGE_SHA256>",
    "createdAt": "<UTC_TIMESTAMP>",
    "sourceCommit": "<GIT_COMMIT>",
    "buildConfiguration": "Release"
  },
  "target": {
    "siteName": "<IIS_SITE_NAME>",
    "appPoolName": "<IIS_APP_POOL_NAME>",
    "physicalPath": "<PRODUCTION_SITE_PATH>"
  },
  "preserve": [
    "<PRESERVED_CONFIG_OR_DIRECTORY>"
  ],
  "backup": {
    "root": "<BACKUP_ROOT>",
    "required": true,
    "includeIisConfig": true,
    "includeHashes": true
  },
  "smokeTests": [
    {
      "name": "version",
      "url": "<SMOKE_TEST_URL>/api/version",
      "expectedStatus": 200
    }
  ],
  "approval": {
    "approvedBy": "<AUTHORIZED_USER>",
    "approvedAt": "<UTC_TIMESTAMP>",
    "scope": "Production release requires explicit approval"
  }
}
```

## BDD 驗收

### Scenario: 建立正式機環境盤點欄位

Given 使用者要求建立正式機更新工具規格
When 文件完成環境盤點欄位
Then 文件包含連線方式、IIS site、app pool、實體路徑、備份路徑、服務帳號與權限欄位
And 文件不包含帳密、憑證、token 或正式連線字串

### Scenario: 定義 release manifest

Given 更新工具需要可追溯發布來源
When 文件定義 release manifest
Then manifest 包含版本、來源包、hash、commit、目標環境、IIS 目標、保留項目、備份與 smoke test
And 所有正式環境值使用 placeholder

### Scenario: 阻擋未授權正式站操作

Given 尚未取得正式站執行授權
When 執行本 Task
Then 不連線正式機
And 不查詢或修改正式 IIS
And 不發布正式站

### Scenario: 避免刪除資料

Given 未來發布工具可能需要清理暫存或舊備份
When 本 Task 建立規格
Then 文件明確要求不得自動刪除資料
And 需要刪除的項目需另寫 MD 供使用者確認

## 測試規劃

- Happy Path：確認規格涵蓋環境盤點、manifest、備份、hash、smoke test 與授權欄位。
- Boundary Case：正式值未知時以 placeholder 保留，不阻塞規格完成。
- Invalid Input：manifest 缺 hash、缺來源 commit、缺目標環境或包含機敏值時不得進入後續實作。
- Regression Risk：不得放寬正式站授權要求，不得誤將 Portal 路徑列為 SPC 發布目標。

## 後續拆分

下一個可執行小工作為 `RELEASE-TOOL-TASK-002`：備份與回復流程原型，且僅能在非正式目標演練。
