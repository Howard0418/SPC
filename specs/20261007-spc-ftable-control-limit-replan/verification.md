# SPC F 表版本記錄與管制界線重算規劃驗證

功能 ID：SPC-FTABLE-CL-REPLAN-20261007  
日期：2026-10-07  
狀態：`SPC-CL-RECALC-TASK-001` 已完成；F 表版本記錄待執行

## 驗證
- Happy Path：已將標準差方法切換後重算管制界線、F 表版本記錄拆成兩個可執行小工作。
- Boundary Case：規格已列入手動固定界線、分段管制線、樣本數不足與 F 表無變更。
- Invalid Input：規格已列入無效公式、缺少 F 表儲存格、非法回復、未授權操作。
- Regression Risk：規格已要求保護既有藥液公式版本紀錄、F 表引用計算與 SPC 圖表界線來源。

## 結果
- AC-001：`TODO.md` 新增 `SPC-CL-RECALC-TASK-001` 並排在 F 表版本記錄之前。
- AC-002：`TODO.md` 新增 `SPC-FTABLE-VERSION-TASK-001`。
- AC-003：測試方向已寫入規格。
- AC-004：本次僅文件與工作池更新，未修改程式、資料庫、IIS，未發布。

## SPC-CL-RECALC-TASK-001 驗證結果
- Requirement：切換「樣本標準差」與「系統標準公式」後，不可沿用舊固定 UCL/CL/LCL。
- Spec/BDD：沿用本規格 Scenario「切換標準差方法後重算管制界線」；本次實作後端保存防護。
- Implementation：`PartProcessCharacteristicsController.Update` 偵測 `FormulaConfigJson.XbarCalculationMethod` 改變時，清空 `UCL/CL/LCL` 並移除舊 `SpcCalculationResults`，讓後續圖表以新方法重新計算統計界線。
- Happy Path：`Update_Should_Clear_FixedControlLimits_When_XbarCalculationMethodChanges` 通過，舊固定界線清空。
- Boundary Case：固定界線由後端清空，避免前端送回舊值覆蓋新方法；分段管制線未修改，仍依原日期區間優先。
- Invalid Input：既有 `CleanFormulaConfig` 與 `ValidateScopeAsync` 保留 JSON 驗證；無效 JSON 仍拒絕。
- Regression Risk：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore` 通過；新增單測通過。整個 `PartProcessCharacteristicMaintenanceTests` 類別有 3 個既有測試斷言落差，失敗點為舊 BadRequest 格式與規則群行為，不是本次新增測試。
- Test：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter FullyQualifiedName~Update_Should_Clear_FixedControlLimits_When_XbarCalculationMethodChanges --no-restore` 通過。
- Publish：已發布 SPC 測試站 backend 至 `D:\SPC\release\test\backend`，備份 `D:\SPC\release\test\backend.backup-cl-recalc-20261007-161715`；`SpcSingleTest` app pool 已回收。
- Verify：`http://172.16.110.27:8084/api/version` 回 200/test；未登入 `PUT /api/v1/part-process-characteristics/1` 回 401；`release/test/backend/app_offline.htm` 不存在。

