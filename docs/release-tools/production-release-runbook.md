# SPC／Portal 正式版更新工具

版本 2.1｜2026-10-19｜規格：[RELEASE-PRODUCTION-TOOL-20261019](../../specs/20261019-production-release-tool/spec.md)

新增跨環境操作入口見[測試／正式切換與遠端發布](remote-release-runbook.md)。本頁保留正式機端操作及備份回復規則。

## 入口與範圍

使用 [Invoke-ProductionRelease.ps1](../../tools/release/Invoke-ProductionRelease.ps1)，支援 `Plan`（唯讀預檢）、`Apply`（備份後更新）、`Rollback`（回復同批應用）。在對應正式主機以系統管理員 Windows PowerShell 5.1 或 PowerShell 7 執行；需 WebAdministration、.NET Runtime、SQL 備份與 migration 權限。產包電腦另需 .NET SDK。

本次已完成隔離自動化驗證及真實交付包的離線產包檢查；新版工具尚未在正式機執行 Apply／Rollback，不把這次開發視為未來發布的永久授權。每次發布仍須取得該次應用、DB migration／啟動與回復範圍授權；同批失敗自動回復及恢復服務是該次 Apply 的必要步驟。

| 系統 | 正式主機 | IIS／AppPool | 實際目錄 | DB |
| --- | --- | --- | --- | --- |
| SPC | PMR-SPC-SRV.pmr.com.tw | SpcApi、SpcWeb | C:\inetpub\wwwroot\production\backend、frontend | PMR_SPC_2026 |
| Portal | PMR-WEB.pmr.com.tw | PmrPortalApi、PmrPortalWeb | C:\inetpub\wwwroot\publish\portal-api、portal-web | PMR_PORTAL_UAT |

工具固定白名單，不接受呼叫端自行指定正式路徑／備份根目錄。不使用 `D:\Sites\PmrPortal`；舊 ReleaseApplyPackage／ReleaseBackupRestore 的 Production 阻擋仍保留。IIS 主機、路徑、pool、來源 commit、環境、檔案或 ZIP SHA256 不符即拒絕。變更正式拓撲須另立小工作更新白名單及測試。

## 1. 準備與審查

先完成對應 build／test；Portal 必須用既有 `scripts/publish-all.ps1 -OutputRoot <隔離輸出目錄>` 產包，SPC 用既有 Release publish 與正式前端設定。不要把本機測試目錄直接當正式包；SPC 目前正式 HTTP API 拓撲不由本工具改造。

以工具從同一份來源匯出實際 EF migration catalogue。此工具只建立 DbContext 模型並呼叫 GetMigrations／GenerateScript，不執行應用程式啟動或打開資料庫連線，避免把檔名誤當實際待遷移項目。

```powershell
.\tools\release\Export-ProductionMigrationReview.ps1 `
  -System SPC -ApiProject .\backend\MesSpc.Api\MesSpc.Api.csproj `
  -OutputDirectory .\release-staging\<release-id>-review `
  -FromMigration <正式庫已套用且在來源存在的最後一筆-ID>
```

Portal 的 ApiProject 改為 `D:\PmrPortal\src\PmrPortal.Api\PmrPortal.Api.csproj`。若無待遷移，只輸出 catalogue，可省略 FromMigration。審查產生 SQL 並與正式 history 對照；不修改來源不存在的歷史 ID。不建立或變更正式 DB，直到明確授權、停寫與備份驗證完成。

## 2. 產生交付包

```powershell
.\tools\release\New-ProductionReleaseBundle.ps1 `
  -System SPC -ReleaseId <本次唯一-ID> -SourceCommit <40位來源commit> `
  -ApiPackage <已publish的backend目錄> -WebPackage <已build的frontend目錄> `
  -ApiVersion <新API版本> -WebVersion <新Web版本> `
  -PreviousApiVersion <目前正式API版本> -PreviousWebVersion <目前正式Web版本> `
  -MigrationCatalog <review目錄>\migration-catalog.json `
  -OutputDirectory <新的交付目錄>
```

若有 migration，增加 `-MigrationSql <已審查SQL> -MigrationId <本次全部待遷移ID陣列>`。無 migration 時不可夾帶 SQL。工具產生兩個 ZIP、完整逐檔 SHA256、manifest、新舊版 smoke 檢查。`manifest.json` 必須與原始 ZIP／SQL 一起交付，不得在套用後修改。

產包排除 appsettings*.json、web.config、app_offline、.env、uploads、private-data、persistent-data、keys、logs、憑證及資料庫檔。工具不搬移正式附件或改寫連線字串。ZIP 不允許路徑穿越、重複檔案、不在清單的檔案；單包解壓大小上限 2GB。產包輸入不可含 reparse point。

確認 manifest 的新／舊版本、正式 URL、預期狀態、source commit 與 catalogue 完整。SPC Web 版本由正式 build 與交付雜湊追溯；API／Portal Web 在套用前額外檢查 DLL FileVersion。可擴充 smoke 檢查包含管制圖資產、唯讀查詢與 API 401，但只允許固定正式主機的 GET 或匿名 401／403 POST，不允許任意資料寫入。

## 3. 預檢與更新

將整個交付目錄及三份執行來源（Invoke-ProductionRelease.ps1、Invoke-ReleaseOnServer.ps1、ProductionRelease.psm1；Export／New 留在產包機）交付到對應正式主機的獨立目錄。不要放在網站目錄。套用命令在該正式主機執行：

```powershell
.\Invoke-ProductionRelease.ps1 -Mode Plan -ManifestPath <交付目錄>\manifest.json

# 取得本次明確授權後執行；確認畫面會列出主機及release ID。
.\Invoke-ProductionRelease.ps1 -Mode Apply -ManifestPath <交付目錄>\manifest.json `
  -ApplicationAuthorized -DatabaseAuthorized
```

`-WhatIf` 只做唯讀核對。`-Confirm:$false` 可供已授權的排程使用；兩個 Authorized 開關仍必填，不能替代實際使用者授權。因應用程式啟動會自動 migration，無 SQL 更新的包仍須核准 DB／啟動範圍。

預檢會核對正式 JSON 層、DB_NAME、實際 EF history 與完整來源 catalogue；本次未授權或漏列的待遷移會阻擋。IIS site/pool/path 不符、共享 pool、既有 app_offline、環境變數改寫連線或環境、reparse point 均拒絕。預檢不停止服務、建立備份或遷移。

更新依序：取得主機獨佔鎖 → 保存原始服務狀態 → 停止該系統 IIS AppPool → appcmd IIS 備份 → 網站備份與 hash 驗證 → SQL COPY_ONLY/CHECKSUM 備份及 RESTORE VERIFYONLY → 已審查 EF SQL → history 再核對 → 逐檔套用 → DLL/檔案 hash、環境設定保留檢查 → 啟動 → 嚴格 smoke → 恢復原始 pool 狀態。新套件設定檔不會覆蓋正式值。

維護前須另外暫停 TransFiles 直寫及外部排程；工具只控制白名單 AppPool，無法停止其他電腦的資料庫寫入。同機不可併行發布。跨系統依序 SPC、Portal，每套有獨立 release ID 與備份；這不是跨主機原子交易，前者完成後後者失敗不自動將前者降版。對跨系統 DB schema 不相容的變更，須額外規劃整體停寫及更新順序。

## 4. 回復與證據

套用或 smoke 失敗會驗證同批網站備份並回復應用，再執行舊版 smoke；備份／migration 失敗時恢復原始 pool。回復驗證失敗會停止 pool、回報 recovery-failed-maintenance，不假報成功。

手動回復：

```powershell
.\Invoke-ProductionRelease.ps1 -Mode Rollback -ManifestPath <原交付目錄>\manifest.json `
  -ApplicationAuthorized -DatabaseAuthorized
```

只接受原 manifest 與原備份；現有檔案已被後續發布或人工改寫時拒絕，避免覆蓋較新版本。回復應用不會自動 Down／drop／截短欄位或還原整庫；SQL schema 可能已套用或部分套用，保留 DB evidence 給人工處理。應用只能在審查確認向後相容的 migration 下回復；破壞性 migration 不應使用此一般流程。整庫還原須另外核對備份後新增資料與停寫區間並取得授權。

新產生檔案與舊資產保留、不自動刪除；回復還原舊 entrypoint／deps／DLL，由舊程式選取舊資產。evidence 的 newFiles 列出新增項目供另行審查；不保證清除新版本檔案，不刪任何附件。

每次備份保留於 `C:\ProgramData\PMR-Releases\Production\<release-id>`，僅 SYSTEM 與 Administrators 可存取。內含 plan、pool-state、backup hashes、私有設定備份、database／iis 記錄與 evidence；SQL .bak 位於 SQL Server 的預設備份目錄。不得把這些備份或 evidence 推送 Git。失敗輸出只顯示階段／錯誤類別，不印設定、SQL 例外或回應內容。工具不建立 app_offline，不刪備份，不回復整台 IIS 全域設定（避免覆蓋其他站台）。

更新後同步規格、版本、測試與發布狀態；登入後 SSO、真實查詢／製圖須另記人工驗收，不以 HTTP 200 代替完整業務驗收。

## 隔離驗證

```powershell
pwsh -NoProfile -File .\tools\release\tests\ProductionRelease.Tests.ps1
```

測試使用獨立暫存檔案及 IIS／SQL／HTTP 替身，驗證 happy path、hash/路徑防護、備份與 migration 失敗、回復失敗、原池停止狀態及舊工具阻擋；不連線正式主機或正式 DB。
