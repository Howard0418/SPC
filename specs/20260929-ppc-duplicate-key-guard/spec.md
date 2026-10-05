# 小型變更：PPC 重複鍵儲存防護
- 功能 ID：20260929-ppc-duplicate-key-guard
- 版本：1
- 狀態：完成；SPC 測試 API 已發布
- 專案／授權依據／需求基準：使用者回報 SPC 管制項目編輯時發生 SQL 唯一索引重複錯誤；依 AGENTS.md 緊急修正流程。

## 問題、預期行為與範圍
問題：編輯 SPC 管制項目時，若送出的 ControlScope / ProcessId / MachineId / TankId / SlotId / CharacteristicId / Unit 與另一筆既有管制項目相同，後端直接送入 SQL Server，導致 `IX_PartProcessCharacteristics_ControlScope_ProcessId_MachineId_TankId_SlotId_CharacteristicId_Unit` 唯一索引錯誤。

預期行為：Create / Update 存檔前先偵測同鍵其他列。若重複，回傳 409 與可讀訊息；若只是更新同一筆原資料，不應誤判重複。

範圍：僅修改 SPC 後端 PPC 主檔 API 與單元測試；不改資料庫 schema、不修補既有資料、不發布正式站。

## 需求與驗收
- R-001：PPC 新增/編輯需在 SaveChanges 前攔截同鍵重複。
- AC-001：不同 Id 且同鍵時回 409；同 Id 自身更新不阻擋；前端可顯示後端訊息。

## 計畫與任務
- 受影響檔案：`backend/MesSpc.Api/Controllers/MasterDataV2Controller.cs`、`backend/MesSpc.Api.Tests/*`、本規格、`ai_docs/10_change_log.md`、`CHANGELOG_CUSTOM.md`。
- [x] T-001：實作 R-001。
- [x] T-002：驗證 AC-001。
- [x] T-003：同步有效需求與變更紀錄。

## 驗證
- 方式、環境：`dotnet test backend/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj`；`dotnet publish backend/MesSpc.Api/MesSpc.Api.csproj -c Release --no-restore`；SPC release/test backend。
- 實際結果及證據：測試 43 passed / 1 skipped；Release publish 成功；部署 DLL hash 與 staging 相同 `1ED9864006FEBA6FAC97676D5F1911EAEA2280EBB4913F09D7AB0A6D63FB47AD`；`http://172.16.110.27:8081/api/version` 回 `environment=test`。
- 發布狀態：已發布 SPC 測試 API；備份 `release-staging/ppc-duplicate-key-guard-20260929/backend-backup`。正式站未發布。
- 限制或未決問題：未以真實畫面重送同一筆編輯；本次以後端查重與測試覆蓋防止 SQL 唯一索引錯誤外洩。
