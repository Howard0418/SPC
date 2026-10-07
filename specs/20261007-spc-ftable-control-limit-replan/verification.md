# SPC F 表版本記錄與管制界線重算規劃驗證

功能 ID：SPC-FTABLE-CL-REPLAN-20261007  
日期：2026-10-07  
狀態：`SPC-CL-RECALC-TASK-001` 已完成；`SPC-FTABLE-VERSION-TASK-001` 已完成

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

## SPC-FTABLE-VERSION-TASK-001 驗證結果
- Requirement：F 表套用新版或回復舊版時，需建立可查詢、可追溯、可回復的版本記錄。
- Spec/BDD：通過「F 表變更建立版本紀錄」與「F 表版本回復」驗收情境。
- Implementation：新增 `ChemicalFTableVersionHistory`、版本查詢/明細/回復 API、F 表維護頁版本記錄與回復按鈕；套用與回復會保存前後儲存格 snapshot、操作類型、原因、修改人與回復來源。
- Happy Path：`SyncCellsAsync_ShouldCreateVersionHistory_WhenAppliedCellsChange` 通過。
- Boundary Case：無變更且未切換啟用版本時不新增重複歷程；查詢筆數限制在 1～100。
- Invalid Input：回復不存在版本或空白 snapshot 會回傳錯誤。
- Regression Risk：既有 F 表公式試算測試仍通過；前端 production build 通過。
- Test：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter FullyQualifiedName~ChemicalFTableServiceTests --no-restore` 通過 3/3；`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore` 通過；`npm run build` 通過。
- Publish：已發布 SPC 測試站 backend/frontend 至 `D:\SPC\release\test`，備份 `D:\SPC\release\test\backend.backup-ftable-history-20261007-163420`；`SpcSingleTest` app pool 已回收。
- Verify：`http://172.16.110.27:8084/api/version` 回 200/test，首頁回 200；未登入 `GET /api/v1/chemical-f-table/versions` 與回復 API 回 401；`release/test/backend/app_offline.htm` 不存在。未取得登入憑證，因此版本清單與回復畫面以自動化 service 測試驗證。

