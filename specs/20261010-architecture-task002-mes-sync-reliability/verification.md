# ARCH-TASK-002 驗證紀錄

## 自動化驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter MesSyncMessageBatchProcessorTests --no-restore -p:UseSharedCompilation=false`
  - 結果：3 passed。
  - 覆蓋：未支援 MessageType 不標示 `Processed`、無效 JSON 標示 `Failed`、註冊測試 handler 時有效訊息可標示 `Processed`。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`
  - 結果：0 warnings / 0 errors。
  - 備註：首次與 test 並行執行時 Debug output 發生 `CS2012` 檔案鎖，單獨重跑 build 通過。

## 發布驗證

- SPC 測試站 backend 已發布。
- 備份：`release\test\backend.backup-architecture-task002-20261010-211822`。
- 發布備註：首次 robocopy 因 `MesSpc.Api.dll` 被測試站使用而中止；建立 `app_offline.htm` 等待釋放後重跑部署成功，並已移除 `app_offline.htm`。
- Smoke test：
  - `http://172.16.110.27:8084/api/health`：200，`{"status":"ok","version":"0.1.59","environment":"test"}`。
  - `http://172.16.110.27:8084/api/version`：200，`{"version":"0.1.59","environment":"test"}`。
  - `http://172.16.110.27:8084/`：200，`text/html`。
