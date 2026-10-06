# 藥液分析公式版本記錄與回復驗證紀錄

## 2026-10-06 TASK-001/TASK-002 規格與現況

- 狀態：完成。
- 本階段未修改產品程式、未建置、未發布測試站。

### 已確認現況

- 藥液分析公式目前儲存在 `PartProcessCharacteristics.ChemicalAnalysisConfigJson`。
- 前端單筆編輯由 `PartProcessCharacteristicsView.vue` 組出 `chemicalAnalysisConfigJson`，透過 `PUT /part-process-characteristics/{id}` 覆蓋目前設定。
- 目前有 F 表版本資料表與服務，但用途是 F 表基準與公式引用，不是每個藥液分析公式的異動歷史。
- 目前缺少公式修改日期、修改人、前後版本與回復 API。

### 產出

- [spec.md](spec.md)
- [plan.md](plan.md)
- [tasks.md](tasks.md)

### 待後續驗證

- 後端公式版本新增/修改/回復測試。
- 權限測試。
- 前端版本清單與回復操作。
- 測試站實際修改一筆公式並回復。

## 2026-10-06 TASK-003 後端資料模型與 Migration

- 狀態：完成。
- 修改：新增 `ChemicalAnalysisFormulaVersion` entity、`ChemicalAnalysisFormulaVersions` DbSet、EF mapping、唯一版本索引與 migration。
- 範圍：只建立資料承載結構；尚未接 API、版本服務、前端或測試站發布。

### 驗證

- 先建測試：初次執行 `ChemicalAnalysisFormulaVersionModelTests` 因 model 尚未建立而編譯失敗，符合 red test 預期。
- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter ChemicalAnalysisFormulaVersionModelTests --no-restore -p:UseSharedCompilation=false`：1 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 備註：一次 build 與 test 並行時遇到 DLL 檔案鎖定，單獨重跑 build 後通過。

## 2026-10-06 TASK-004 公式版本服務

- 狀態：完成。
- 修改：新增 `ChemicalAnalysisFormulaVersionService`，負責公式內容比對、建立下一版版本紀錄、拒絕非 CHEM 主檔，以及回復指定版本並新增 Restore 紀錄。
- 範圍：本階段只建立可共用的後端服務；尚未接 `PUT /part-process-characteristics/{id}`、查詢/回復 API 或前端。

### 驗證

- 先建測試：初次執行 `ChemicalAnalysisFormulaVersionServiceTests` 因 service 尚未建立而編譯失敗，符合 red test 預期。
- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter ChemicalAnalysisFormulaVersionServiceTests --no-restore -p:UseSharedCompilation=false`：4 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 備註：一次 build 與 test 並行時遇到 DLL 檔案鎖定，單獨重跑 test 後通過。

## 2026-10-06 TASK-005 整合既有 PPC 更新流程

- 狀態：完成。
- 修改：`PUT /part-process-characteristics/{id}` 在保存主檔後，呼叫 `ChemicalAnalysisFormulaVersionService.RecordChangeAsync`；CHEM 公式內容有變更時新增版本紀錄，未變更時不新增。
- 範圍：不新增 API、不改前端；仍沿用既有主檔儲存流程與 display mode 驗證。

### 驗證

- 先建測試：初次執行 `Update_Should_Record_ChemicalFormulaVersion_When_ChemicalConfigChanges` 因 controller 尚未注入版本服務而編譯失敗，符合 red test 預期。
- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter "Update_Should_Record_ChemicalFormulaVersion_When_ChemicalConfigChanges|ChemicalAnalysisFormulaVersionServiceTests" --no-restore -p:UseSharedCompilation=false`：5 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 備註：曾嘗試併跑舊測試 `Update_Should_Save_Unit_And_ItemSpecificRules`，該測試卡在既有 `UpdateRules` 已固定 WE 規則的舊期待，非本次版本紀錄變更；本階段未修改該舊測試。

## 2026-10-06 TASK-006 版本查詢與回復 API

- 狀態：完成。
- 修改：新增 `ChemicalAnalysisFormulaVersionsController`。
- API：
  - `GET /api/v1/part-process-characteristics/{id}/chemical-analysis-formula-versions`
  - `POST /api/v1/part-process-characteristics/{id}/chemical-analysis-formula-versions/{versionId}/restore`
- 範圍：查詢版本清單、回復指定版本；回復限制 `Admin,Editor`。本階段不改前端、不發布測試站。

### 驗證

- 先建測試：初次執行 `ChemicalAnalysisFormulaVersionsControllerTests` 因 controller 尚未建立而編譯失敗，符合 red test 預期。
- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter "ChemicalAnalysisFormulaVersionsControllerTests|ChemicalAnalysisFormulaVersionServiceTests" --no-restore -p:UseSharedCompilation=false`：7 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 備註：一次 build 與 test 並行時遇到 DLL 檔案鎖定，單獨重跑後通過。

## 2026-10-06 TASK-007 後端測試補齊

- 狀態：完成。
- 修改：補齊藥液公式版本相關測試，涵蓋資料模型、服務、既有 PUT 整合、無變更不新增、非 CHEM、回復 API 與回復端點授權角色。
- 範圍：只補測試與文件，不改產品功能行為。

### 驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter "ChemicalAnalysisFormulaVersion" --no-restore -p:UseSharedCompilation=false`：10 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。

## 2026-10-06 TASK-008 前端版本紀錄入口

- 狀態：完成。
- 修改：藥液管制項目編輯 modal 的「藥液分析公式」區塊新增「版本紀錄」按鈕，可載入並顯示目前項目的公式版本清單。
- 範圍：只讀版本紀錄；尚未提供回復按鈕或回復確認，留待 TASK-009。

### 驗證

- `npm run build -- --mode testhost`：通過。
- 本階段未發布測試站。
