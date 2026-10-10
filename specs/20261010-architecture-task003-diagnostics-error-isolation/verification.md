# 驗證紀錄

功能 ID：ARCH-TASK-003-DIAGNOSTICS-ERROR-ISOLATION-20261010
日期：2026-10-10
狀態：DONE
發布狀態：已發布 SPC 測試站 backend；正式站未發布

| 驗收 ID | 方式 | 結果 | 狀態 |
| --- | --- | --- | --- |
| AC-001 | 非 Development 一般例外單元測試 | `ApiErrorResponseFactoryTests` 通過；非 Development payload 不含原始 exception message/detail。 | 通過 |
| AC-002 | Development 一般例外單元測試 | `ApiErrorResponseFactoryTests` 通過；Development payload 保留 message/detail。 | 通過 |
| AC-003 | 非 Development DbUpdateException 單元測試 | `ApiErrorResponseFactoryTests` 通過；非 Development DB inner detail 不回傳。 | 通過 |
| AC-004 | `/api/health` 測試站 smoke | `http://172.16.110.27:8084/api/health` 200，回 `{"status":"ok","version":"0.1.59","environment":"test"}`，未含 connection string/key/secret/token。 | 通過 |
| AC-005 | 後端測試/build 與測試站發布 | 針對性測試 3 passed；後端 build 0 warnings / 0 errors；已發布測試站 backend，`/api/version` 200/test、首頁 200。 | 通過 |

## 執行紀錄

- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 首次並行執行測試/build 時因同一 Debug DLL 檔案鎖導致測試編譯失敗；單獨重跑後通過，非功能失敗。
- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter ApiErrorResponseFactoryTests --no-restore -p:UseSharedCompilation=false`：3 passed。
- `dotnet publish backend\MesSpc.Api\MesSpc.Api.csproj -c Release -o release-staging\architecture-task003-backend-20261010-211129 --no-restore -p:UseSharedCompilation=false`：通過。
- 發布備份：`release\test\backend.backup-architecture-task003-20261010-211209`。備份與部署使用 `robocopy`，排除 `private-data`、`logs`，並保留既有 `appsettings*.json`、`web.config`。
- `Stop-WebAppPool`/`Start-WebAppPool` 在本 PowerShell 環境因 WebAdministration COM 類別未註冊不可用；未能透過 App Pool 指令停啟，但 `robocopy` 完成，HTTP smoke 通過。
