# 技術計畫

功能 ID：ARCH-TASK-003-DIAGNOSTICS-ERROR-ISOLATION-20261010

## 受影響檔案

- `backend/MesSpc.Api/Program.cs`
- `backend/MesSpc.Api/Services/Security/ApiErrorResponseFactory.cs`
- `tests/MesSpc.Api.Tests/ApiErrorResponseFactoryTests.cs`
- `specs/20261010-architecture-task003-diagnostics-error-isolation/*`
- `TODO.md`
- `docs/requirements.md`
- `CHANGELOG_CUSTOM.md`
- `ai_docs/10_change_log.md`

## 實作方式

1. 新增 `ApiErrorResponseFactory`，集中產生一般例外與 DB 例外 payload。
2. `Program.cs` 的 `UseExceptionHandler` 改用該工廠，不直接輸出 `ex.Message`/inner detail 到非 Development。
3. 新增 `/api/health` anonymous endpoint，僅回傳狀態、環境與版本。
4. 新增單元測試，先保護 payload 規則。
5. 執行後端針對性測試與 build；通過後發布 SPC 測試站 backend 並做 smoke。

## 相容性

- HTTP status 維持：一般未處理例外 500、DB update 409。
- Development 保留診斷細節。
- 既有 controller 自行回傳的 BadRequest/Conflict 格式不在本次收斂範圍。

## Rollback

- 還原 `Program.cs`、刪除新增工廠與測試。
- 若已發布測試站，還原發布前 backend 備份。
