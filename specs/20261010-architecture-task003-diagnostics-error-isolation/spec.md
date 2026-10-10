# TASK-003 診斷端點與錯誤資訊隔離

功能 ID：ARCH-TASK-003-DIAGNOSTICS-ERROR-ISOLATION-20261010
版本：1
狀態：DONE
主系統：SPC
相依系統：SPC API、IIS 測試站
資料庫影響：無
發布影響：已發布 SPC 測試站 backend；正式站未發布

## 目的

收斂 SPC API 的診斷端點與未處理例外回應，讓測試/正式環境可做基本健康檢查，同時避免 Production 對外回傳 exception message、inner exception、SQL 或堆疊細節。

## 現況與證據

- `Program.cs` 已有 `GET /api/version`，回傳版本與 `AppEnvironment`。
- `Program.cs` 已有全域 `UseExceptionHandler`，但目前一般未處理例外會回傳 `message = ex.Message` 與 `detail = ex.InnerException?.Message`，Production 有外洩內部錯誤內容風險。
- 既有 `TestDataController`、`MigrationController` 已限制 Development，但一般錯誤回應未統一隔離。
- `TODO.md` 將 `TASK-003` 列為架構改善待確認；使用者已確認可先做架構改善。

## 範圍

- 新增錯誤回應工廠，依環境產生安全錯誤 payload。
- 調整全域 exception handler：
  - Production/Test 等非 Development：不回傳 exception message/detail。
  - Development：保留 message/detail 方便診斷。
  - `DbUpdateException` 仍回 409，但非 Development 不回傳資料庫 inner detail。
- 新增匿名 `GET /api/health`，回傳最小健康資訊。
- 建立單元測試保護錯誤資訊隔離。
- 同步 `TODO.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md` 與驗證紀錄。

## 非範圍

- 不改資料庫 schema。
- 不新增需登入的完整診斷後台。
- 不檢查或回傳 SQL connection string、JWT key、SMTP、Portal SSO key 等機敏設定。
- 不修改各 controller 既有業務錯誤格式。
- 不發布正式站。

## 需求與驗收

- R-001：Production/Test 未處理例外不得外洩 exception message、inner exception、SQL 或 stack trace。
  - AC-001：單元測試證明非 Development 一般例外 payload 只含通用訊息與錯誤代碼，不含原始 exception message/detail。
- R-002：Development 仍可診斷未處理例外。
  - AC-002：單元測試證明 Development payload 保留 message/detail。
- R-003：資料庫更新例外保留可讀分類，但不在非 Development 回傳 inner detail。
  - AC-003：單元測試證明非 Development `DbUpdateException` 回 409 payload 不含 inner exception。
- R-004：系統提供最小匿名 health endpoint。
  - AC-004：`GET /api/health` 回 200，包含 `status`、`environment`、`version`，不包含 connection string、key、secret、token。
- R-005：測試通過後發布 SPC 測試站，正式站不發布。
  - AC-005：後端測試/build 通過，測試站 smoke 記錄明確。

## BDD

### Scenario: Production 不外洩未處理例外

Given API 在非 Development 環境發生未處理例外
When 全域 exception handler 建立錯誤回應
Then 回應只包含通用錯誤訊息與錯誤代碼
And 回應不包含原始 exception message 或 inner exception detail

### Scenario: Development 保留診斷資訊

Given API 在 Development 環境發生未處理例外
When 全域 exception handler 建立錯誤回應
Then 回應可包含 exception message 與 inner exception detail

### Scenario: Health endpoint 不暴露機敏設定

Given 使用者未登入
When 呼叫 `/api/health`
Then API 回傳 200 與最小健康資訊
And 不回傳 connection string、JWT key、SMTP、token 或 secret
