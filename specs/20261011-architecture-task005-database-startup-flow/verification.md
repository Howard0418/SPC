# ARCH-TASK-005 驗證紀錄

## 自動化驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter DatabaseStartupPlannerTests --no-restore -p:UseSharedCompilation=false`
  - 結果：8 passed。
  - 覆蓋：sqlite 大小寫與空白正規化、sqlite 使用 `EnsureCreated` 且不跑 SQL Server baseline、null/空白/sqlserver/未知 provider 預設 SQL Server migration 與 baseline。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`
  - 結果：0 warnings / 0 errors。
  - 備註：首次與 test 並行執行時 Debug output 發生 `CS2012` 檔案鎖，單獨重跑 build 通過。

## 發布驗證

- SPC 測試站 backend 已發布。
- 備份：`release\test\backend.backup-architecture-task005-20261011-203422`。
- 發布備註：使用 `app_offline.htm` 釋放測試站 backend DLL 後部署，部署完成已移除暫停檔；robocopy backup/deploy 回傳 1/3，屬成功等級。
- Smoke test：
  - `http://172.16.110.27:8084/api/health`：200，`{"status":"ok","version":"0.1.59","environment":"test"}`。
  - `http://172.16.110.27:8084/api/version`：200，`{"version":"0.1.59","environment":"test"}`。
  - `http://172.16.110.27:8084/`：200，`text/html`。
