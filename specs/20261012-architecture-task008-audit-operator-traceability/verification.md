# ARCH-TASK-008 驗證紀錄

## 自動化驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter "UserAccessTests|SpcPointRemarksControllerTests" --no-restore -p:UseSharedCompilation=false`
  - 結果：通過，15 passed。
  - 覆蓋：`AuditActorResolver` 優先使用 `Identity.Name`；`preferred_username` fallback；無可用 claim 回 `System`；SPC 點位備註 `UpdatedBy` 可由 `preferred_username` 寫入；既有點位備註行為回歸。

## 建置

- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`
  - 結果：通過，0 errors。
  - 備註：`NU1900` 套件弱點資料來源 `https://api.nuget.org/v3/index.json` 無法載入，屬外部網路/弱點資料查詢警告；第一次與測試平行執行時因同時寫入 Debug DLL 產生檔案鎖，單獨重跑 build 後通過。

## 發布

- 發布：已發布 SPC 測試站 backend 至 `D:\SPC\release\test\backend`。
- 備份：`D:\SPC\release\test\backend.backup-architecture-task008-20261012-0828`；備份時排除受保護的 `private-data` 目錄，未讀取或複製資料保護金鑰。
- 套用證據：`D:\SPC\release\test\backend\_release-evidence-20261012-082508.json`。
- 發布備註：第一次套用遇 `Microsoft.Data.SqlClient.dll` 被 IIS 程序鎖住；已建立 `app_offline.htm` 等待釋放後重跑成功，完成後已移除 `app_offline.htm`。
- Smoke：
  - `http://172.16.110.27:8084/api/health`：200，`status=ok`、`environment=test`。
  - `http://172.16.110.27:8084/api/version`：200，`environment=test`。
  - `http://172.16.110.27:8084/`：200。
- 正式站：未發布。
