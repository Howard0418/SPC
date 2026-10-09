# RELEASE-TOOL-TASK-003 更新 IIS 網站資料夾流程

日期：2026-10-09
狀態：DONE
主系統：SPC
相依系統：RELEASE-TOOL-TASK-001、RELEASE-TOOL-TASK-002、非正式 IIS/網站資料夾
資料庫影響：無
發布影響：非發布工具原型；未操作正式站
Rollback：還原本規格與 `tools/release/ReleaseApplyPackage.ps1`

## Requirement

建立可在非正式目標演練的 IIS 網站資料夾更新流程原型，涵蓋 app_offline、app pool 停啟、複製交付包、保留環境設定、smoke test 與證據輸出。

本 Task 不連線正式機、不操作正式 IIS、不刪除資料。`app_offline.htm` 移除屬刪除動作，本輪僅列入待刪除清單，不自動執行。

## 產出

- `tools/release/ReleaseApplyPackage.ps1`

## 安全規則

- `EnvironmentName` 為 `prod`、`production`、`formal` 或 `正式` 時直接拒絕。
- 目標路徑若位於 `D:\Sites\PmrPortal` 直接拒絕。
- 套用交付包時不刪除目標多餘檔案。
- 預設保留 `appsettings.json`、`appsettings.Production.json`、`web.config`；可用 `-PreserveRelativePath` 覆寫。
- `app_offline.htm` 可建立，但不自動刪除；需另經使用者確認刪除。
- App Pool 控制只在非正式目標且本機有 `WebAdministration` 模組時執行。
- Smoke test 只記錄 HTTP 狀態與錯誤訊息，不保存機敏回應內容。

## CLI 範例

```powershell
pwsh -File tools/release/ReleaseApplyPackage.ps1 `
  -EnvironmentName Sandbox `
  -SitePath C:\ReleaseSandbox\site `
  -PackagePath C:\ReleaseSandbox\package `
  -CreateAppOffline `
  -SmokeUrl http://localhost:5080/api/version `
  -WhatIf
```

## BDD 驗收

### Scenario: 非正式目標套用交付包

Given 操作者指定 `EnvironmentName` 為 Sandbox
When 執行更新流程
Then 工具將交付包複製到網站資料夾
And 不刪除目標多餘檔案
And 產生 release evidence JSON

### Scenario: 保留環境設定

Given 目標網站已有環境設定檔
When 交付包包含同名設定檔
Then 工具先將目標設定檔複製到 staging
And 套用後保留目標環境設定

### Scenario: 正式環境名稱被拒絕

Given 操作者指定 `EnvironmentName` 為 Production
When 執行更新流程
Then 工具停止
And 不複製任何檔案

### Scenario: Portal 路徑被拒絕

Given 操作者指定 SitePath 位於 `D:\Sites\PmrPortal`
When 執行更新流程
Then 工具停止
And 不操作該路徑

### Scenario: app_offline 移除不自動執行

Given 工具建立 app_offline.htm
When 更新流程完成
Then 工具不自動刪除 app_offline.htm
And 需要移除時需列入待刪除清單並由使用者確認

## 測試規劃

- Happy Path：非正式資料夾套用 package、保留設定、寫出 evidence、執行 smoke URL。
- Boundary Case：IIS 模組不存在、無 smoke URL、設定檔不存在、package 空資料夾。
- Invalid Input：Production 環境、Portal 路徑、package 不存在、ControlAppPool 缺 AppPoolName。
- Regression Risk：工具不得連線正式機，不得刪除資料，不得寫入 Portal 正式路徑，不得保存機敏 response body。

## 發布狀態

不發布測試站或正式站。此 Task 僅新增非正式演練工具原型。
