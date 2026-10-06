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
