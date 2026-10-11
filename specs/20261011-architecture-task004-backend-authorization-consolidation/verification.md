# ARCH-TASK-004 驗證紀錄

## 自動化驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter "AuthorizationAccessTests|UserAccessTests" --no-restore -p:UseSharedCompilation=false`
  - 結果：11 passed。
  - 覆蓋：Editor + `calibration.manage` 可管理、Viewer 不可管理、停用操作者不可管理、Operator 無權限 JSON 時可使用 claims permission、既有角色正規化與 Viewer 寫入防護維持通過。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`
  - 結果：0 warnings / 0 errors。
  - 備註：首次與 test 並行執行時 Debug output 發生 `CS2012` 檔案鎖，單獨重跑 build 通過。

## 發布驗證

- SPC 測試站 backend 已發布。
- 備份：`release\test\backend.backup-architecture-task004-20261011-103654`。
- 發布備註：使用 `app_offline.htm` 釋放測試站 backend DLL 後部署，部署完成已移除暫停檔；robocopy backup/deploy 回傳 1/3，屬成功等級。
- Smoke test：
  - `http://172.16.110.27:8084/api/health`：200，`{"status":"ok","version":"0.1.59","environment":"test"}`。
  - `http://172.16.110.27:8084/api/version`：200，`{"version":"0.1.59","environment":"test"}`。
  - `http://172.16.110.27:8084/`：200，`text/html`。
