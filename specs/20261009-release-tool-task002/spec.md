# RELEASE-TOOL-TASK-002 備份與回復流程原型

日期：2026-10-09
狀態：DONE
主系統：SPC
相依系統：RELEASE-TOOL-TASK-001、非正式 IIS/網站資料夾
資料庫影響：無
發布影響：非發布工具原型；未操作正式站
Rollback：移除本規格與 `tools/release/ReleaseBackupRestore.ps1`

## Requirement

建立可在非正式目標演練的 PowerShell/CLI 原型，支援備份網站資料夾、匯出 IIS 設定、hash 紀錄與回復。此 Task 不連線正式機、不操作正式 IIS、不刪除資料。

## 產出

- `tools/release/ReleaseBackupRestore.ps1`

## 安全規則

- `EnvironmentName` 為 `prod`、`production`、`formal` 或 `正式` 時直接拒絕。
- 目標路徑若位於 `D:\Sites\PmrPortal` 直接拒絕。
- `Restore` 必須明確加上 `-AllowNonProductionRestore`。
- 回復只將備份檔複製回目標，不刪除目標多出的檔案。
- IIS 匯出只在非正式演練且本機有 `WebAdministration` 模組時執行；模組不存在時寫入 warning evidence。
- 本工具不保存帳密、憑證、token 或連線字串。

## CLI 範例

```powershell
pwsh -File tools/release/ReleaseBackupRestore.ps1 `
  -Mode Backup `
  -EnvironmentName Sandbox `
  -SitePath C:\ReleaseSandbox\site `
  -BackupRoot C:\ReleaseSandbox\backups `
  -BackupId sandbox-001 `
  -IisSiteName SandboxSite `
  -IncludeIisConfig `
  -WhatIf
```

```powershell
pwsh -File tools/release/ReleaseBackupRestore.ps1 `
  -Mode Restore `
  -EnvironmentName Sandbox `
  -SitePath C:\ReleaseSandbox\site `
  -BackupRoot C:\ReleaseSandbox\backups `
  -BackupId sandbox-001 `
  -AllowNonProductionRestore `
  -WhatIf
```

## BDD 驗收

### Scenario: 非正式目標備份網站資料夾

Given 操作者指定 `EnvironmentName` 為 Sandbox
When 執行 Backup 模式
Then 工具複製網站資料夾到備份目錄
And 產生 `backup-manifest.json` 與 `site-hashes.json`

### Scenario: 正式環境名稱被拒絕

Given 操作者指定 `EnvironmentName` 為 Production
When 執行 Backup 或 Restore 模式
Then 工具停止
And 不複製任何檔案

### Scenario: Portal 路徑被拒絕

Given 操作者指定 SitePath 位於 `D:\Sites\PmrPortal`
When 執行 Backup 或 Restore 模式
Then 工具停止
And 不操作該路徑

### Scenario: 非正式回復需明確授權旗標

Given 操作者指定 Restore 模式
When 未提供 `-AllowNonProductionRestore`
Then 工具停止
And 提示回復需要明確旗標

### Scenario: 回復不刪除目標多餘檔案

Given 目標資料夾有備份中不存在的檔案
When 執行 Restore 模式
Then 工具只複製備份檔案覆蓋到目標
And 不刪除目標多餘檔案

## 測試規劃

- Happy Path：在本機 sandbox 目錄執行 backup/restore，確認 manifest 與 hash evidence。
- Boundary Case：IIS 模組不存在、空資料夾、重複 BackupId。
- Invalid Input：Production 環境、Portal 路徑、缺 BackupId、Restore 無明確旗標。
- Regression Risk：工具不得連線正式機，不得刪除資料，不得寫入 Portal 正式路徑。

## 發布狀態

不發布測試站或正式站。此 Task 僅新增非正式演練工具原型。
