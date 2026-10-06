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

## 2026-10-06 TASK-006 管制圖計算套用單點排除

- 狀態：完成。
- 修改：
  - `SpcService.GetInteractiveChartAsync` 讀取 active `SpcPointExclusions`。
  - 變量型管制圖 raw point 若有 `VariableMeasurement` 單點排除，會標記 `IsExcluded = true`。
  - Xbar-R / Xbar-S 子組若任一 raw measurement 被單點排除，該子組點會標記 `IsExcluded = true`。
  - 保留既有 `UploadBatches.IsExcluded` 整批排除，單點排除只做 OR 合併，不覆蓋整批排除。

### 驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter SpcPointExclusionCalculationTests --no-restore -p:UseSharedCompilation=false`：1 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- `dotnet publish backend\MesSpc.Api\MesSpc.Api.csproj -c Release --no-restore -o release-staging\spc-point-exclusion-calc-20261006\backend-publish -p:UseSharedCompilation=false`：成功。
- 測試站發布：已發布 `release/test/backend`；備份 `release/test/backend.backup-spc-point-exclusion-calc-20261006`。
- Smoke：
  - `http://172.16.110.27:8081/api/version`：HTTP 200，`environment=test`。
  - `release/test/backend/app_offline.htm`：不存在。

### 限制

- 本階段只接管制圖後端變量資料與子組計算。
- 趨勢圖、直方圖、常態檢定、前端右鍵選單、`ExcludedHidden` 圖上隱藏效果與已排除點清單仍留待後續 TASK。

## 2026-10-06 TASK-007 趨勢圖/直方圖/常態檢定套用單點排除

- 狀態：完成。
- 修改：
  - 趨勢圖/直方圖共用的 `PopulateNormalityAndCurve` 已由 TASK-006 回傳的 `IsExcluded` raw points 套用單點排除。
  - 補齊 Attribute chart 的 `AttributeMeasurement` 單點排除讀取與 raw point 標記。
  - `AttributeDataPoint` 補帶 `AttributeMeasurementId`，供後續前端右鍵與恢復清單定位點位。

### 驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter SpcPointExclusionCalculationTests --no-restore -p:UseSharedCompilation=false`：2 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- `dotnet publish backend\MesSpc.Api\MesSpc.Api.csproj -c Release --no-restore -o release-staging\spc-point-exclusion-trend-20261006\backend-publish -p:UseSharedCompilation=false`：成功。
- 測試站發布：已發布 `release/test/backend`；備份 `release/test/backend.backup-spc-point-exclusion-trend-20261006`。
- Smoke：
  - `http://172.16.110.27:8081/api/version`：HTTP 200，`environment=test`。
  - `release/test/backend/app_offline.htm`：不存在。

### 限制

- 本階段只處理後端口徑，不改前端右鍵選單與 `ExcludedHidden` 圖上隱藏渲染。

## 2026-10-06 TASK-008 管制圖右鍵單點排除

- 狀態：完成。
- 修改：
  - 管制圖點位支援右鍵選單。
  - 選單可設定「顯示但不列入計算」、「隱藏且不列入計算」、「恢復列入計算」。
  - 點位排除改呼叫 `PUT/DELETE /api/v1/spc/point-exclusions`，不再以右鍵操作整批 UploadBatch。
  - 圖例補上已排除點位樣式；已排除點沿用灰色叉號樣式。
  - Attribute chart `chartData.points` 補帶 `attributeMeasurementId`，供右鍵選單定位單點。

### 驗證

- `npm run build -- --mode testhost`：通過。
- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter SpcPointExclusionCalculationTests --no-restore -p:UseSharedCompilation=false`：2 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 測試站發布：已發布 `release/test/frontend`；備份 `release/test/frontend.backup-spc-point-context-menu-20261006`。
- 測試站發布：已發布 `release/test/backend`；備份 `release/test/backend.backup-spc-point-context-menu-20261006`。
- Smoke：
  - `http://172.16.110.27:8083/`：HTTP 200，`text/html`。
  - `http://172.16.110.27:8083/assets/index-93uPHtFu.js`：HTTP 200，`application/javascript`。
  - `http://172.16.110.27:8081/api/version`：HTTP 200，`environment=test`。
  - `release/test/backend/app_offline.htm`：不存在。

### 限制

- 本階段先完成管制圖右鍵操作與 ExcludedVisible 樣式。
- `ExcludedHidden` 已可寫入後端並不列入計算，但圖上隱藏點恢復清單留待 TASK-009 完成。

## 2026-10-06 TASK-009 管制圖已排除點清單

- 狀態：完成。
- 修改：
  - 管制圖工具列新增「已排除點 N」按鈕。
  - 展開後可查看目前 PPC 的 active 排除點。
  - 清單支援逐筆「恢復列入計算」，可恢復 `ExcludedHidden` 點。
  - 查圖成功後同步載入排除清單；右鍵排除或恢復後重載圖表與清單。

### 驗證

- `npm run build -- --mode testhost`：通過。
- 測試站發布：已發布 `release/test/frontend`；備份 `release/test/frontend.backup-spc-excluded-list-20261006`。
- Smoke：
  - `http://172.16.110.27:8083/`：HTTP 200，`text/html`。
  - `http://172.16.110.27:8083/assets/index-Z4tE0lCl.js`：HTTP 200，`application/javascript`。

### 限制

- 本階段只處理管制圖排除點清單；趨勢圖清單留待 TASK-011。

## 2026-10-06 TASK-010 趨勢圖右鍵單點排除

- 狀態：完成。
- 修改：
  - 趨勢圖點位支援右鍵選單。
  - 選單可設定「顯示但不列入計算」、「隱藏且不列入計算」、「恢復列入計算」。
  - 趨勢圖點位排除改呼叫 `PUT/DELETE /api/v1/spc/point-exclusions`。
  - 趨勢圖圖例補上已排除點位樣式；已排除點沿用灰色叉號樣式。

### 驗證

- `npm run build -- --mode testhost`：通過。
- 測試站發布：已發布 `release/test/frontend`；備份 `release/test/frontend.backup-trend-point-context-menu-20261006`。
- Smoke：
  - `http://172.16.110.27:8083/`：HTTP 200，`text/html`。
  - `http://172.16.110.27:8083/assets/index-KEeRhtLO.js`：HTTP 200，`application/javascript`。

### 限制

- 本階段先完成趨勢圖右鍵操作與 ExcludedVisible 樣式。
- 趨勢圖已排除點清單與 `ExcludedHidden` 隱藏點恢復入口留待 TASK-011。

## 2026-10-06 TASK-011 趨勢圖已排除點清單

- 狀態：完成。
- 修改：
  - 趨勢圖工具列新增「已排除點 N」按鈕。
  - 展開後可查看目前 PPC 的 active 排除點。
  - 清單支援逐筆「恢復列入計算」，可恢復 `ExcludedHidden` 點。
  - `ExcludedHidden` 點會從趨勢線上隱藏，但仍保留於清單供恢復。

### 驗證

- `npm run build -- --mode testhost`：通過。
- 測試站發布：已發布 `release/test/frontend`；備份 `release/test/frontend.backup-trend-excluded-list-20261006`。
- Smoke：
  - `http://172.16.110.27:8083/`：HTTP 200，`text/html`。
  - `http://172.16.110.27:8083/assets/index-DKfYYFj2.js`：HTTP 200，`application/javascript`。

### 限制

- 本階段只處理趨勢圖前端清單與恢復；未改後端 API、資料表或統計公式。

## 2026-10-06 TASK-012 後端回歸測試

- 狀態：完成。
- 本階段未修改產品程式、未發布測試站。

### 驗證

- `dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter "SpcPointExclusionCalculationTests|NormalityTest|XbarR_GoldenValues_ShouldUseN3Constants|XbarR_ShouldKeepChemicalShiftMetadataOnChartPoints" --no-restore -p:UseSharedCompilation=false`：8 passed。
- `dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。

### 備註

- 第一次 build 與 test 並行時遇到 `MesSpc.Api.dll` 檔案鎖定，單獨重跑 build 後通過；判定為並行程序鎖定，不是編譯錯誤。

## 2026-10-06 TASK-013 前端 build / UI 測試

- 狀態：完成；含已知既有 Playwright 測試落差。
- 本階段未修改產品程式、未發布測試站。

### 驗證

- `npm run build:test`：通過，產出 `assets/index-DKfYYFj2.js` 與 `assets/index-CVlQs-Gh.css`。
- 趨勢圖 UI 靜態檢查：
  - `trend-excluded-points-toggle` 與 `trend-excluded-points-panel` 存在。
  - `restoreExcludedPoint`、`ExcludedHidden` 隱藏判斷與 `point-exclusions` API 呼叫存在。
- 管制圖 UI 靜態檢查：
  - `excluded-points-toggle` 與 `excluded-points-panel` 存在。
  - `restoreExcludedPoint` 與 `point-exclusions` API 呼叫存在。

### 已知測試落差

- `npx playwright test tests/spc-ui.spec.ts tests/spc-summary.spec.ts --project=chromium`：5 failed。
- 失敗點為既有測試與目前 UI/mock 不一致：
  - 版本文字 `Enterprise SPC Edition` 找不到。
  - `ModuleGuide` 內容初始狀態為 hidden。
  - 舊管制圖 mock 等不到 canvas。
  - summary 測試等不到既有「重新計算」按鈕或 select。
- 以上失敗未指向本次新增的已排除點清單 testid；本階段先記錄為既有 Playwright 測試需後續校正。

## 2026-10-06 TASK-014 測試站總 smoke test

- 狀態：完成。
- 本階段未修改產品程式；正式站未發布。
- SPC 測試站目前已使用前序小工作發布的 frontend/backend。

### 驗證

- `http://172.16.110.27:8083/`：HTTP 200，`text/html`。
- `http://172.16.110.27:8083/assets/index-DKfYYFj2.js`：HTTP 200，`application/javascript`。
- `http://172.16.110.27:8081/api/version`：HTTP 200，`environment=test`。
- `release/test/backend/app_offline.htm`：不存在。
- `release/test/frontend/index.html`：存在。
- `release/test/frontend/assets/index-DKfYYFj2.js`：存在。
