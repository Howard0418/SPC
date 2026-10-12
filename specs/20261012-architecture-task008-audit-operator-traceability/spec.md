# 小型變更：稽核與操作者追溯
- 功能 ID：ARCH-TASK-008-AUDIT-OPERATOR-TRACEABILITY-20261012
- 版本：1
- 狀態：已完成
- 專案／授權依據／需求基準：SPC 架構改善 TASK-008；依 `TODO.md` 架構改善排序與 `docs/requirements.md` 現行需求索引。

## 問題、預期行為與範圍
- 現況：SPC 多個寫入流程依 `User.Identity.Name`、`name` claim 或 request body 操作者欄位寫入 `CreatedBy` / `UpdatedBy`，缺少共用解析規則。
- 問題：不同 controller 若各自解析 ClaimsPrincipal，當 JWT/SSO claim 型態不同時，稽核欄位可能落成 `System` 或不同格式，降低後續追溯一致性。
- 預期：先建立共用操作者解析器，明確排序 `Identity.Name`、`ClaimTypes.Name`、`name`、`preferred_username`、`username`、`ClaimTypes.NameIdentifier`、`sub`，並修剪空白；SPC 點位備註/排除寫入稽核欄位時使用同一規則。
- 範圍：只調整 claim 解析與 SPC 點位備註/排除 `UpdatedBy`；不新增資料表、不修改 JWT 發行、不改登入流程、不補做完整 audit log。

## 需求與驗收
- R-001：系統需提供共用操作者解析規則，供後端寫入 `CreatedBy` / `UpdatedBy` 或 audit log 時使用。
- R-002：SPC 點位備註/排除新增、更新、清空、恢復時，`UpdatedBy` 必須使用共用解析規則。
- AC-001：Given 已驗證使用者只有 `preferred_username` claim，When 儲存 SPC 點位備註，Then `UpdatedBy` 寫入該 claim 值。
- AC-002：Given claim 值為空白或沒有可用 claim，When 解析操作者，Then 回傳預設值 `System`。
- AC-003：Given 使用者具備 `Identity.Name` 與其他 username claim，When 解析操作者，Then 優先使用 `Identity.Name`。

## 計畫與任務
- 受影響檔案：
  - `backend/MesSpc.Api/Services/Security/AuditActorResolver.cs`
  - `backend/MesSpc.Api/Controllers/SpcController.cs`
  - `tests/MesSpc.Api.Tests/UserAccessTests.cs`
  - `tests/MesSpc.Api.Tests/Controllers/SpcPointRemarksControllerTests.cs`
- [x] T-001：新增共用操作者解析器與單元測試。
- [x] T-002：調整 SPC 點位備註/排除使用共用解析規則，並補寫 controller 測試。
- [x] T-003：同步有效需求、變更紀錄、TODO 與驗證結果。

## 驗證
- Happy Path：`Identity.Name` 優先於其他 claim。
- Boundary Case：只有 `preferred_username` 時仍可追溯操作者。
- Invalid Input：空白 claim 或無 claim 回 `System`。
- Regression Risk：確認既有點位備註測試仍通過，後端 build 通過。
- 實際結果及證據：`UserAccessTests|SpcPointRemarksControllerTests` 15 passed；後端 build 0 errors。
- 發布狀態：已發布 SPC 測試站 backend；正式站未發布。
- 限制或未決問題：本次不建立跨模組 audit log schema，也不重寫校正模組既有 `CalibrationAuditLog`。
