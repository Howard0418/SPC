# CAL-VERIFY-001 驗證紀錄

日期：2026-10-31

## 根因

- `AuthorizationAccess.ResolveAsync` 對查無 Operator 的已驗證 JWT 回傳 `IsActive=true`。
- `CalibrationRules.CanManage` 對 Editor 且權限 JSON 空白採全部頁面預設，導致未知 Editor JWT 可通過校驗管理判斷。
- 正常 Portal SSO 會先取得或建立 SPC Operator 再簽發 JWT；資料庫不存在的 Editor 不屬正常登入路徑，應 fail closed。

## 自動驗證

- 先紅：新增未知 Editor 單元測試，修正前失敗，實際為 `true`。
- `AuthorizationAccessTests`：5 passed。
- `CalibrationImportHttpTests.BothWriteEndpointsRejectUnauthorizedUsers`：5 passed，未登入401；Viewer、明確撤銷、停用、未知Editor均403。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore`：0 warning／0 error。

## 發布驗證

- 測試 API：0.1.93／test。
- `/`、`/spc`、`/api/health`、`/api/version`：HTTP 200。
- 發布前備份：`D:\SPC\release-staging\cal-verify-001-20261031\backup`；`appsettings.json` 保留測試資料庫設定。
- 正式站、Portal、資料庫 schema與業務資料均未修改。

## 回復

停止 `SpcSingleTest` 應用程式集區，以備份覆蓋測試 backend後重新啟動；再驗證 health/version與首頁。
