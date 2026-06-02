# Full System Test Implementation Plan

在進入程式碼撰寫與修改前，本文件定義了自動化測試的實作藍圖，確保 FPC 專屬特性與 SPC 核心引擎的準確度。

## 1. 要新增哪些測試專案
基於目前的 `.NET 8` 與 `Vue 3` 架構，我們預計補強以下三個層級的測試專案：
1. **`MesSpc.Api.UnitTests`**：專門針對後端核心服務 (如 `SpcEngine`) 進行高密度運算驗證。
2. **`MesSpc.Api.IntegrationTests`**：利用 `Microsoft.AspNetCore.Mvc.Testing` 搭配 SQLite In-Memory 測試 API 端點與資料庫讀寫。
3. **`mes-spc-web-e2e`**：基於 Playwright 的前端端到端測試專案，模擬使用者點擊與圖表渲染。

## 2. 要新增哪些測試檔案
- **Unit Tests (`MesSpc.Api.UnitTests`)**:
  - `SpcEngineTests.cs`: 涵蓋 Xbar-R, P/NP/C/U Chart 計算公式與 WECO 規則檢定。
  - `FormulaEngineTests.cs`: 涵蓋特殊自訂公式與 Cpk 運算邏輯。
- **Integration Tests (`MesSpc.Api.IntegrationTests`)**:
  - `UploadsControllerTests.cs`: 模擬上傳 CSV/Excel (包含異常值與缺值)。
  - `TraceabilityTests.cs`: 測試 `LotSlotHistory` 寫入與 Genealogy 查詢結果。
  - `MesSyncProcessorTests.cs`: 測試佇列訊息解析。
- **E2E Tests (`mes-spc-web-e2e`)**:
  - `dashboard-pareto.spec.ts`: 驗證柏拉圖渲染與篩選連動。
  - `traceability-tree.spec.ts`: 驗證系譜圖展開。
  - `alerts-workflow.spec.ts`: 模擬異常簽核流程。

## 3. 要使用哪些測試框架
- **後端 (C#)**: `xUnit`, `Moq`, `FluentAssertions` (提供高可讀性的斷言語法)。
- **API (C#)**: `WebApplicationFactory<Program>`, `Respawn` (用於重置測試資料庫)。
- **前端 (Vue)**: `Vitest`, `Vue Test Utils` (元件測試)。
- **E2E**: `Playwright` (支援跨瀏覽器、並行執行、Visual Regression Test)。

## 4. 要建立哪些測試資料
建立專屬的 Seeder (`TestSeeder.cs`)，僅在測試環境啟動。所有資料加上 `TEST_` 前綴：
- **基礎資料**：`TEST_PT_ETCH` (檢驗項目)、`TEST_CHEM_CU` (藥水)。
- **黃金樣本 (Golden Data)**：一組從 Minitab 匯出且已知 UCL/LCL、Cp/Cpk 標準答案的陣列，供 `SpcEngineTests` 對標。
- **邊界資料**：極端小數 (如 0.0001)、負數不良數 (供 Exception 測試)。

## 5. 測試執行順序
為確保依賴關係正常，自動化腳本執行順序如下：
1. `Unit Tests` (無資料庫依賴，極速執行，擋下公式錯誤)。
2. `Database Migration Check` (確保 Schema 正確)。
3. `Integration Tests` (包含 API 權限與 Controller 邏輯)。
4. `Frontend Component Tests` (確保 UI 元件行為)。
5. `E2E Tests` (起完整環境，時間最長，作為最後一關)。

## 6. 如何清除測試資料
1. **Integration Test**：每個 Test Method 結束後，透過 [Respawn](https://github.com/jbogard/Respawn) 清除所有 Table 狀態（或在 SQLite 中直接 Rollback Transaction）。
2. **E2E Test**：在 `global.teardown.ts` 中透過 API 觸發隱藏的 Cleanup Endpoint (`/api/test-utils/clean-prefix?prefix=TEST_`)，一舉掃掉所有標記為 TEST_ 的測試紀錄。

## 7. 如何避免影響正式資料
- **環境隔離**：定義明確的 `ASPNETCORE_ENVIRONMENT=Testing`。
- **連線字串置換**：Integration Test 強制切換為 `DataSource=:memory:` 或專屬的 `PMR_SPC_TEST_DB`。
- **前綴卡控**：若萬一混入正式機，DB Trigger 或後端腳本可針對 `TEST_` 前綴進行定期清道夫作業。

## 8. CI/CD 如何執行測試
- **Pipeline Tool**: GitHub Actions 或 GitLab CI。
- **觸發時機**: 每次 Pull Request 建立時觸發 (On-Push)。
- **執行流程**:
  1. Checkout Code。
  2. Setup .NET & Node.js。
  3. `dotnet test` (包含 Unit & Integration)。
  4. `npm run test:unit`。
  5. `npx playwright install --with-deps` -> `npx playwright test`。
  6. 若有失敗，上傳 Playwright Traces/Screenshots 作為 Artifact 供工程師 Debug。
