# 10 Change Log

## [2026-06-25] - 作業人員角色權限與匯入量測者帳號整合
- **修改目的**:
  - 將既有作業人員主檔擴充為可登入的系統使用者，提供 `Viewer`（僅檢視與查詢）及 `Editor`（完整操作）兩種角色。
  - 計量型與計數型資料確認匯入時，將檔案內量測者自動加入系統使用者管理並預設為 `Editor`。
  - 權限限制同時落實於後端 API 與前端操作介面，避免只隱藏按鈕卻仍可直接呼叫寫入 API。
  - 清理資料時僅移除具有測試標記的資料，保留既有維護主檔與正式使用者。
- **相容性原則**:
  - 既有作業人員資料升級後預設保留 `Editor` 權限。
  - 保留既有 demo 管理帳號作為過渡登入方式。
  - 所有資料庫結構變更均透過 EF Core Migration 執行。

## [2026-06-24] - 新增管制界線試算與分段管控 (Trial Calculate & Segmented Control Limits)
- **後端分段界線模型與資料庫升級 (AppDbContext & Program.cs)**:
  - 新增 `ControlLimitSegment` 實體與 `ControlLimitSegments` 資料表，用於儲存不同時間區間的自訂統計管制界線。
  - 在 `Program.cs` 啟動階段加入 SQL 自動建表邏輯，避免直接執行 Migration 時遭遇 Windows 沙箱權限阻擋。
- **點級動態界線異常判定與圖表運算 (SpcEngine & Rules)**:
  - 升級 `WesternElectricRulesValidator` 與 `NelsonRulesValidator` 的異常檢定引擎，支援「點級動態界線解析」。各點所處時間對應的分段管制界線（`UCL`/`CL`/`LCL`）會作為檢定基準，若無分段則回退至全局設定。
  - 升級 `AttributeChartCalculator`、`ImrChartCalculator`、`XbarRChartCalculator`、`XbarSChartCalculator`，在輸出數據點時將 active UCL/CL/LCL 動態對應至各個量測點，並輸出 `uclStat`/`clStat`/`lclStat` 供前端繪製。
- **後端試算與分段 CRUD API (Controllers & Services)**:
  - 新增 `ControlLimitSegmentsController`，提供分段管制界線的 CRUD 端點，並在建立或更新時實作時間區間重疊的防呆校驗。
  - 在 `SpcController` 新增 `trial-calculate` 試算端點，允許輸入日期區間，拉取該區間內的量測數據並呼叫 SPC 引擎計算出統計界界線值。
- **安全清理測試資料 (DatabaseSeeder.cs)**:
  - 實作 `ClearTransactionalDataAsync()` 方法，只清除量測值、批次、警報等交易性測試數據，安全保留料號、工站、檢驗特性等主配置維護檔 (維護主檔)。
  - Expose `/api/testdata/clear-transactions` 路由至 `TestDataController`。
- **前端介面分段管理與 stepped 曲線渲染 (frontend)**:
  - `PartProcessCharacteristicsView.vue`: 新增「分段管制線與界線試算」功能按鈕。點擊後開啟 Modal，左側可進行歷史數據試算，一鍵帶入右側；右側支援分段上限、中心線、下限、有效日期區間及備註的 CRUD 管理，並整合後端區間重疊錯誤回報。
  - `SpcChartView.vue`: 配合動態界線，將 ECharts 中心線 (CL) 改為如同 UCL/LCL 的 stepped line (階梯折線) 渲染，與點級分段相符。

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
