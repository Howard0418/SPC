# SPC 管制圖量測點備註驗證

功能 ID：SPC-CHART-POINT-REMARKS-20261007  
日期：2026-10-09
狀態：已實作並發布 SPC 測試站

## 驗證

- Happy Path：已新增 `PUT/GET /api/v1/spc/point-remarks` 與前端右鍵新增/編輯備註；重新載入圖表會重新查詢備註並顯示於 tooltip/點位明細。
- Boundary Case：備註長度限制 500 字；空白備註與清空按鈕採 `IsActive=false` 軟停用，不做實體刪除。
- Invalid Input：找不到 PPC、非本 PPC 的 VariableMeasurement/AttributeMeasurement、缺少點位識別或超過 500 字皆會拒絕。
- Regression Risk：點位備註使用獨立 `SpcPointRemarks`，不修改 `SpcPointExclusions`、原始量測值或 SPC 計算欄位。

## 結果

- AC-001：`SpcPointRemark` entity、`SpcPointRemarks` DbSet/EF mapping 與 migration `20261008183026_AddSpcPointRemarks` 已新增。
- AC-002：新增 `GET/PUT/DELETE /api/v1/spc/point-remarks`；清空與刪除 API 皆為軟停用，不移除資料列。
- AC-003：管制圖與趨勢圖右鍵選單已加入「新增/編輯備註」，並在 tooltip/點位明細顯示備註。
- AC-004：備註不影響既有排除/隱藏狀態；`SpcPointRemarksControllerTests` 驗證新增備註不建立 `SpcPointExclusions`。
- AC-005：使用測試名字與測試備註，未加入真實姓名或機敏資料。
- AC-006：`dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter "SpcPointRemarksControllerTests|SpcPointExclusionsControllerTests" --no-restore -p:UseSharedCompilation=false`：8 passed。
- AC-007：`dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- AC-008：`npm run build`：通過，僅既有 chunk size warning。
- AC-009：已發布 SPC 測試站 backend/frontend 檔案；備份 `release/test/backend.backup-spc-point-remarks-20261009-023652`、`release/test/frontend.backup-spc-point-remarks-20261009-023652`。
- AC-010：HTTP smoke：`http://172.16.110.27:8084/api/version` 200/test；`/` 200；新前端資產 `assets/index-96H3lPaN.js` 200；未登入 `GET /api/v1/spc/point-remarks?ppcId=1` 回 401。
