# Full System Test Plan (FPC Smart Factory SPC)

## 1. 系統功能清單
- **品質主檔管理**：料號、製程、機台、檢驗基準、配方定義。
- **追溯主檔管理**：廠區、產線、槽體、槽位、藥水。
- **資料採集與匯入**：計量型/計數型 Excel/CSV 匯入、人工手動輸入。
- **SPC 統計運算引擎**：Xbar-R, X-MR, P, NP, C, U 管制圖計算，西方電氣異常規則判斷，Cp/Cpk/Pp/Ppk 能力指標計算。
- **高階戰情看板 (Dashboard)**：首頁柏拉圖、異常走勢、Cpk 警示排行榜。
- **異常處置閉環 (Alerts Workflow)**：異常簽核、真因分析、對策執行。
- **生產履歷與產品系譜 (Genealogy & Traceability)**：批號追溯、工單關聯、槽位歷史紀錄。
- **MES 介接整合**：非同步事件佇列處理 (Sync Messages)。

## 2. 測試範圍
- **前端 (Frontend)**：Vue 3 + Vite, ECharts 圖表渲染、表單驗證、權限卡控、RWD 顯示。
- **後端 (Backend)**：.NET 8 Web API, SPC 引擎算法準確度、EF Core 資料庫操作、非同步佇列效能。
- **資料庫 (Database)**：SQL Server 綱要約束 (Foreign Keys)、索引效能、大數據量查詢。
- **系統整合 (Integration)**：API 回應時間、MES Staging Table 寫入與消化。

## 3. 測試策略
- **單元測試 (Unit Test)**：針對 `SpcEngine.cs`, `FormulaEngineService.cs` 進行 100% 覆蓋率測試，確保統計公式與常數（如 A2, D3, D4）絕對正確。
- **API 整合測試 (Integration Test)**：透過 `WebApplicationFactory` 搭配 SQLite in-memory 或 Testcontainers 測試 End-to-End API 邏輯（包含 Excel 匯入解析）。
- **前端元件測試 (Component Test)**：使用 Vitest 測試 Vue 元件（如圖表綁定資料是否正確）。
- **端到端自動化測試 (E2E Test)**：使用 Playwright 模擬使用者真實操作（如上傳檔案、點擊單據、切換頁籤）。
- **人工探索性測試 (Manual Exploratory)**：針對 UI/UX 動線與產品系譜圖的連動進行人工驗證。

## 4. 測試資料設計
為避免污染正式環境，所有測試資料將採用明確前綴，並且在 E2E 測試結束後進行 Rollback 刪除：
- **工單/批號**：`TEST_WO_001`, `TEST_LOT_001`, `TEST_SUBLOT_001A`
- **料號/製程**：`TEST_PART_A`, `TEST_PROC_ETCH`
- **機台/產線**：`TEST_LINE_1`, `TEST_TANK_1`, `TEST_MACH_01`
- **操作員**：`TEST_OP_001`
- **檢驗項目**：`TEST_PT_ETCH`, `TEST_DEFECT_RATE`

## 5. 測試環境需求
- **後端**：.NET 8 SDK
- **前端**：Node.js 20+
- **資料庫**：SQL Server (LocalDB 或隔離的測試資料庫 `PMR_SPC_TEST`)
- **瀏覽器環境**：Chromium, Firefox, WebKit (透過 Playwright 驅動)

## 6. 測試風險
- **SPC 計算精度誤差**：浮點數計算精度可能導致小數點後 3~4 位的捨入誤差，需與 Minitab 產出的黃金基準資料對標。
- **大量資料效能瓶頸**：`SpcCalculationResults` 查詢與 ECharts 渲染在資料點超過 5000 點時可能出現卡頓。
- **MES 同步延遲**：測試環境若無真實 MES，需自行 Mock 寫入 `MesSyncMessages` 表格模擬高併發。

## 7. 測試優先順序
1. **P1 (Highest)**：SPC 統計公式準確度 (Xbar-R, Cpk, WECO)。若算錯，整個系統失去信任。
2. **P1 (Highest)**：資料匯入 (Excel/CSV) 與防呆檢核。
3. **P2 (High)**：Traceability 槽位歷史關聯與 Product Genealogy 樹狀圖。
4. **P2 (High)**：MES 同步與異常處置簽核 (Alert Workflow)。
5. **P3 (Medium)**：Dashboard 渲染、權限管理與前端 RWD。

## 8. 不可破壞既有功能清單
在進行新功能開發或測試時，必須進行回歸測試以確保下列既有功能不受損：
- 既有的品質主檔資料（已存在 DB 內的 Seed Data）。
- 計量型 SPC (Xbar-R) 舊有歷史圖表產出邏輯。
- SMTP 寄信功能設定與連線機制。
