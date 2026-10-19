# 測試／正式環境切換與遠端發布

版本 1.0｜2026-10-19｜[規格](../../specs/20261019-release-environment-switch/spec.md)｜[備份與回復細節](production-release-runbook.md)

可使用[Windows更新執行檔](desktop-updater-guide.md)操作相同流程。

使用 [Invoke-Release.ps1](../../tools/release/Invoke-Release.ps1)，從工作電腦選擇環境與系統。預設為 **Test／Plan**，正式須明確指定 Production；不接受自行指定主機、站台或路徑。執行前需完成該次授權、測試及交付包審查。

| 選擇 | 主機 | IIS／pool | DB |
| --- | --- | --- | --- |
| Test＋SPC | PC-PA16560048.pmr.com.tw／172.16.110.27 | SpcSingleTest；backend＋frontend 兩內容目錄，同一站台／pool | PMR_SPC_TEST |
| Test＋Portal | PC-PA16560048.pmr.com.tw／172.16.110.27 | PmrPortalApi（pool PmrPortal）、PmrPortalWeb | PMR_PORTAL_TEST |
| Production＋SPC | PMR-SPC-SRV.pmr.com.tw／172.16.119.140 | SpcApi、SpcWeb | PMR_SPC_2026 |
| Production＋Portal | PMR-WEB.pmr.com.tw／172.16.119.130 | PmrPortalApi、PmrPortalWeb | PMR_PORTAL_UAT |

測試 SPC 位於 D:\SPC\release\test\backend、frontend；測試 Portal 位於 D:\PmrPortal\publish\portal-api、portal-web。正式路徑沿用正式手冊白名單。所有產包／檢查器 workspace 都不能放入任何測試／正式網站，也禁止 D:\Sites\PmrPortal。

## 一次設定

使用已有 WinRM／Kerberos 權限的 Windows 帳號，目標主機必須可透過完整網域名稱連線並具備 IIS／SQL 備份及遷移所需權限。工具不存帳密、不修改 TrustedHosts、不降低驗證或使用任意 IP fallback。若選到本機，會直接使用本機路徑；工作電腦若為32位元PowerShell，會啟動64位元 Windows PowerShell操作IIS。

遠端執行器需要 Windows PowerShell5.1、WebAdministration 與相應.NET Runtime。腳本須符合機器的執行原則，工具不自動改執行原則。測試時若使用 process-only ExecutionPolicy，不能當成正式機永久設定。

## 準備兩環境的交付包

沿用正式手冊中的 EF catalogue 與已審查 migration SQL 產出方式；使用同一產包器，多指定 Environment：

```powershell
.\tools\release\New-ProductionReleaseBundle.ps1 -Environment Test -System SPC `
  -ReleaseId <唯一ID> -SourceCommit <40位commit> `
  -ApiPackage <測試backend交付目錄> -WebPackage <測試frontend交付目錄> `
  -ApiVersion <新API版本> -WebVersion <新Web版本> `
  -PreviousApiVersion <目前測試API版本> -PreviousWebVersion <目前測試Web版本> `
  -MigrationCatalog <catalogue.json> -OutputDirectory <測試交付目錄>
```

正式包改為 `-Environment Production`，使用正式前端與正式舊版本資訊。Portal 改為 `-System Portal`；Portal API/Web 仍必須先由既有 publish-all.ps1 產包。需要 migration 時同時提供 SQL 與該環境全部待遷移 ID；未提供的 source pending 將被阻擋。

**切換不會改寫交付包內容。** API 二進位可使用相同已驗證來源；SPC 目前前端內含 API／Portal URL，須由相同來源版本分別 build 測試與正式前端。正式來源 .env.production 目前含測試值，必須明確核對實際編譯設定；不可只改 manifest 的環境名稱就發布同一前端ZIP。runtime config 改造不在本次範圍。

每個 manifest 固定一個環境與系統，不能將測試 manifest 配 Production 開關。需對應環境已存在的 appsettings、web.config、DB 與 SSO設定；包內永遠排除這些環境設定。

## 預檢、更新與回復

```powershell
# 純本機核對；不連線或寫入目標電腦
.\tools\release\Invoke-Release.ps1 -System SPC -Environment Test `
  -Mode Plan -ManifestPath <測試交付目錄>\manifest.json -WhatIf

# 將必要交付與工具安全暫存，再在目標主機只讀預檢IIS／DB
.\tools\release\Invoke-Release.ps1 -System SPC -Environment Test `
  -Mode Plan -ManifestPath <測試交付目錄>\manifest.json

# 確認該次更新與DB授權後執行，會顯示目標主機／環境／release ID
.\tools\release\Invoke-Release.ps1 -System SPC -Environment Test `
  -Mode Apply -ManifestPath <測試交付目錄>\manifest.json `
  -ApplicationAuthorized -DatabaseAuthorized

# 正式使用另一份正式manifest，並明確切換Production
.\tools\release\Invoke-Release.ps1 -System SPC -Environment Production `
  -Mode Apply -ManifestPath <正式交付目錄>\manifest.json `
  -ApplicationAuthorized -DatabaseAuthorized
```

Portal 使用相同命令加 `-System Portal` 與 Portal交付包。回復使用同一環境及原 manifest，`-Mode Rollback`，兩個授權開關仍須提供。同批 Apply 失敗自動回復已包含在該次發布授權範圍；不自動還原整庫或刪除新增檔。

Plan 會建立僅供 Administrators／SYSTEM 存取的 `C:\ProgramData\PMR-Release-Incoming\<Environment>\<releaseId>-<fingerprint>`，只傳 manifest、ZIP、已審查SQL與固定三份執行來源；先本地hash驗證，再接收端hash驗證，全部通過才執行。Plan 不建立網站／SQL備份、不停止服務、不套用migration、不覆寫網站。incoming及transfer紀錄會保留，不自動刪除。

Apply／Rollback 用共用 server engine，保留原備份、SQL VERIFYONLY、實際 pending ID 比對、固定路徑、設定保留與失敗回復。備份／證據根目錄為 `C:\ProgramData\PMR-Releases\<Environment>\<releaseId>`；同機操作由獨佔鎖序列化，不會切換到另一環境。

舊 Invoke-ProductionRelease.ps1 保持僅允許Production，作為相容入口；原始非正式演練工具仍阻擋Production。跨主機SPC／Portal依序執行，沒有分散式交易，不自動將另一套已成功系統降版。

## 本次驗證界線

已完成四組實際 Plan（Test SPC／Portal、Production SPC／Portal），包括本機32→64位元與兩台正式WinRM；所有結果都核對到正確DB。自動化另驗證環境混用拒絕、hash竄改、傳輸失敗關閉連線及WhatIf無連線／寫入。

**未執行 Apply／Rollback，未停止服務、遷移或更新站台。** 真正更新及失敗演練仍依該次授權執行，不能將Plan通過當成完整更新／登入業務驗收。
