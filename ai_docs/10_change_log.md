# 10 Change Log

## [2026-06-24] - 查詢時間範圍上限與預設值限制 (日期卡關)
- **後端安全防護與效能優化 (SpcController.cs)**:
  - 限制 `/chart` 與 `/summary` 統計查詢端點的時間間隔最高不可超過 93 天（約 3 個月），防範大量量測數據的掃描。
  - 當查詢起迄日為空時，預設自動查詢最近 3 個月的數據，防止全表無日期過濾的重度資料庫查詢。
- **前端日期選取與阻擋 (SpcChartView.vue)**:
  - 預設載入「量測起日」為 3 個月前，「量測迄日」為今日。
  - 當使用者挑選日期區間大於 3 個月（93 天）時，由前端主動進行阻擋並回報「查詢時間範圍最多不可超過 3 個月」警告訊息，不向後端發送無效請求。

## [2026-06-23] - 新增直方圖常態分佈曲線疊加與常態性檢定
- **後端常態性檢定與曲線生成 (SpcEngine & Services)**:
  - 實作 Jarque-Bera 常態性檢定，包括偏態 (Skewness)、峰態 (Kurtosis) 及卡方生存函數計算 p-value。
  - 實作與前端組寬對齊的平滑常態 PDF 曲線產生器，點數 100 點，並透過 $ScaledPdf = PDF \times n \times binWidth$ 高度轉換重合公式，確保其能與計數直方圖無縫貼合。
- **前端直方圖與常態曲線疊加 (SpcChartView.vue)**:
  - 採用 ECharts 雙 X 軸方案，解決 category 軸柱狀圖與 value 軸曲線無法依數值精準對齊的問題。
  - 新增直方圖頂部常態性檢定專屬指標卡，展示偏態、峰態、p-value 與常態判定（顯著水準 $\alpha = 0.05$）。
- **測試防護網**:
  - 新增 `NormalityTest.cs`，包含正常數據、極端偏離常態數據與零變異防呆邊界情況驗證。

## [2026-06-23] - 製程工站槽位與 SPC 管制項目同步問題修復
- **槽位自動刪除與同步機制**:
  - 在 `MasterDataV2Controller.cs` 的 `SyncMachineTanksAsync` 中，實作了槽位（Tanks）與前端 UI 設定的雙向同步。前端移除槽位時，資料庫中對應的槽位記錄一併刪除，並具備外鍵約束保護。
  - 機台建立時，強制呼叫 `SyncMachineTanksAsync` 建立對應的 `ProductionLine`，防止產生無產線對應的孤立機台。
  - 機台代碼更新時，主動同步變更既有的 `ProductionLine.LineCode`，確保關聯的既有槽位對照不因機台改名而遺失。
- **匯入關聯補齊**:
  - 修正 `UploadService.cs`，在 Excel/CSV 自動匯入建立機台時，同步補建 `ProductionLines` 關聯資料，確保大小寫不一致或新機台能正常加載槽位。
- **前端載入時序與防呆優化**:
  - 於 `PartProcessCharacteristicsView.vue` 將槽位過濾改為以 API 動態向後端請求 `/api/machines/{machineId}/tanks`，根治前後端代碼大小寫或空格不一致引起的配對失敗。
  - 重構 `openEditModal` 編輯載入時序，改為先非同步載入槽位清單後再回填表單欄位，徹底防止競爭條件（Race Condition）導致編輯時已選取槽位被重置為空。

## [2026-05-18] - SPC 企業級品質管理系統全面升級 (Phase 1 ~ Phase 4 Complete)
- **資料庫與主檔架構重構 (Phase 1)**: 擴充 EF Core 模型，加入企業層級架構 (`Plant`, `Factory`, `Process`, `Machine`, `PartProcessCharacteristic`) 與中介暫存表，並透過 `SeedData.cs` 自動寫入標準量測項目與西方電氣規則。
- **SPC 運算引擎與西方電氣規則擴充 (Phase 2)**: 實作完整的 Western Electric Rules (Rule 1~4) 自動檢驗引擎，並擴展統計常數表 ($n=2\sim 25$) 與製程能力指數 ($C_p, C_{pk}, P_p, P_{pk}, \hat{\sigma}_{within}, \sigma_{overall}$) 即時運算。
- **兩階段資料匯入引擎優化 (Phase 3)**: 重構 CSV 與 Excel 上傳解析器，完整兼容中英文表頭對應（如 `料號` / `PartNo`，`測量值` / `MeasuredValue` 等），並建立暫存校驗與正式轉入工作流。
- **前端高階戰情室與互動管制圖重構 (Phase 4)**:
  - 升級 `App.vue` 企業級側邊欄與全域黑暗模式切換。
  - 重構 `DashboardView.vue` 戰情中心，加入即時警報走勢圖與 Cpk 後段班排行榜。
  - 重構 `SpcChartView.vue` 為頂級即時互動式 SPC 管制圖戰情室，支援六大管制圖、規格線、統計界限與紅點異常標記。
  - 修復 Vue 3 非同步 DOM 掛載與 ECharts 畫布初始化時序問題 (`nextTick`)，確保動態切換檢驗基準時無縫重繪管制圖。
  - 於 `SeedData.cs` 寫入真實抽樣檢驗歷史數據（25 筆計量型與 15 筆計數型），並完善 `UploadBatch` 關聯，實現開箱即用的完整展示。
  - 升級 `VariableUploadView.vue`, `AttributeUploadView.vue`, `UploadPreviewView.vue` 支援拖曳上傳與 Stage 2 檢核確認。

## [2026-05-15] - Formula Engine & Attribute Charts (P2)
- **Formula Engine 重構**: 將 `FormulaEngineService` 替換為 `NCalcSync` 函式庫，全面支援動態字串表達式解析。
- **內建變數綁定**: 實作動態綁定 `AVG`, `STDEV`, `MAX`, `MIN`, `RANGE`, `SUM`, `COUNT`, `USL`, `LSL` 等保留字，供表達式直接取用。
- **Attribute 管制圖**: 新增 `AttributeChartCalculator` 模組，完整支援計數型 P, NP, C, U 管制圖的界限計算。
- **單元測試**: 為 `FormulaEngineService` 與 `AttributeChartCalculator` 加入 xUnit 測試並全數通過。

## [2026-05-15] - SPC Unit Testing (P4)
- **測試防護網**: 建立 `MesSpc.Api.Tests` xUnit 測試專案。
- **單元測試**: 為 `ImrChartCalculator`, `XbarRChartCalculator` 與 `NelsonRulesValidator` 撰寫完整的覆蓋測試，涵蓋極端案例與常規計算。
- **配置優化**: 透過 `<InternalsVisibleTo>` 暴露內部模型供動態斷言，確保 API 設計乾淨。

## [2026-05-15] - SPC Engine Refactoring (P2)
- **架構解耦**: 將 SPC 核心運算從 `SpcService` 抽離至獨立的 `SpcEngine` 模組，解除 Entity Framework 相依性。
- **領域模型**: 建立 `SpcDataPoint` 與 `Subgroup` 模型，作為運算引擎的標準傳遞物件。
- **計算器模組**: 實作 `ImrChartCalculator` 與 `XbarRChartCalculator`。
- **規則引擎**: 實作 `NelsonRulesValidator`，支援 Nelson Rule 1 至 Rule 6 的自動判定，並將結果附加至回傳資料點。

## [2026-05-15] - Core Architecture Refactoring
- **核心架構重構**: 引入 `BaseEntity` 抽象類別，統一主鍵名為 `Id`。
- **自動稽核系統**: 實作 `AppDbContext` 自動填充 `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`。
- **資料安全與稽核**: 實作軟刪除 (`IsDeleted`) 與樂觀併發控制 (`RowVersion`)。
- **組織架構擴充**: 新增 `Plant`, `Factory`, `ProductionLine`, `Unit`, `Shift`, `Operator` 等主檔。
- **企業化資料庫**: 完成 SQL Server 遷移腳本 `V4_EnterpriseMasterDataAndAudit` 並成功應用。
- **文件化**: 更新 `ai_docs` 描述最新架構。

## [2026-05-14]
- 執行第一階段：專案盤點與現況分析。
- 執行第二階段：建立 `ai_docs` 基礎文件庫。
- 產出改造優先順序報告。
