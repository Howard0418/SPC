# SPC-POINT-FILTER 驗證紀錄

## 2026-10-06 TASK-001 規劃與現況分析

- 狀態：完成。
- 本階段未修改產品程式、未新增 migration、未發布測試站。

### 已確認現況

- `UploadBatches.IsExcluded` 已存在，`SpcController.ToggleExcludeUploadBatch` 會切換整批上傳資料排除狀態。
- `SpcService.GetInteractiveChartAsync` 會依 excluded upload batch 將 `SpcDataPoint/Subgroup/AttributeDataPoint.IsExcluded` 設為 true。
- I-MR、Xbar-R、Xbar-S、Attribute chart 與 normality 目前已有排除 `isExcluded` 的基礎邏輯。
- 管制圖前端已有點位明細按鈕 `剔除此數據/恢復此數據`，但呼叫的是整批 batch exclude，不是單點 exclude。
- 趨勢圖會呈現 `isExcluded` 點位狀態，但沒有右鍵選單、沒有設定/恢復操作。
- 現有系統沒有 `ExcludedHidden` 狀態，也沒有「已排除點」清單供隱藏點恢復。

### 風險

- Xbar-R/Xbar-S 圖上一點可能代表多筆 raw samples；實作前需固定「排除圖上子組點」的識別方式。
- 既有整批排除與未來單點排除可能同時存在；第一版應讓整批排除優先，避免單點恢復誤解除整批排除。
- 隱藏點若沒有清單入口，使用者會無法恢復，因此恢復入口須先於或同時於隱藏功能完成。

### 驗證

- 文件內容檢查：完成。
- 程式建置：不適用，本階段未修改程式。
- 測試站發布：不適用，本階段未修改程式。

## 2026-10-06 TASK-002 資料模型與 API

- 狀態：完成。
- 修改：
  - 新增 `SpcPointExclusion` entity 與 `SpcPointExclusions` DbSet/EF mapping。
  - 新增 EF migration `20261006003525_AddSpcPointExclusions`，建立 `SpcPointExclusions` 表、索引與外鍵。
  - 新增 API：
    - `GET /api/v1/spc/point-exclusions`
    - `PUT /api/v1/spc/point-exclusions`
    - `DELETE /api/v1/spc/point-exclusions/{id}`
  - `PUT/DELETE` 限 `Admin,Editor`；未登入測試站呼叫回 401。

### 驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter SpcPointExclusionsControllerTests --no-restore -p:UseSharedCompilation=false`：4 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- `dotnet publish backend\MesSpc.Api\MesSpc.Api.csproj -c Release --no-restore -o release-staging\spc-point-exclusions-20261006\backend-publish -p:UseSharedCompilation=false`：成功。
- 測試站發布：已發布 `release/test/backend`；備份 `release/test/backend.backup-spc-point-exclusions-20261006`。
- Smoke：
  - `http://172.16.110.27:8081/api/version`：HTTP 200，`environment=test`。
  - 未登入 `PUT /api/v1/spc/point-exclusions`：HTTP 401。

### 限制

- 本階段只建立資料模型與 API，尚未接入管制圖/趨勢圖計算。
- 右鍵選單、已排除點清單與隱藏點恢復 UI 尚未實作，留待後續 TASK。
