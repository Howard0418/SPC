# 02 資料庫設計 (Database Design)

## 核心設計模式
所有資料表實體均繼承自 `BaseEntity`，保證企業級品質系統所需的稽核追蹤與安全性：
- **Id** (int PK)：自增主鍵。
- **CreatedAt / CreatedBy**：建立時間與人員。
- **UpdatedAt / UpdatedBy**：最後更新時間與人員。
- **IsDeleted** (bool)：軟刪除標記，系統在 `OnModelCreating` 中設定了 Global Query Filter，查詢時會自動過濾已刪除資料。
- **RowVersion** (byte[])：時間戳記，用於防範多人併發衝突。

## 關鍵實體模組與資料表對應

### 1. 組織架構主檔 (Org Structure)
- **Plant**：工廠
- **Factory**：車間
- **ProductionLine**：生產線，代碼 `LineCode` 預設與機台的 `MachineCode` 對應。
- **Tank** (槽位)：與 `ProductionLine` 關聯，定義在產線/機台下的各個物理槽位，並在 `TankCode` 自動附加機台代碼前綴 (例如 `MC-01-T01`)。
- **Unit**：設備單元
- **Operator**（系統使用者／量測者）：
  - `OperatorCode`、`OperatorName`、部門與 Email。
  - `Username`：可空白；空白表示僅作為量測者主檔，不能登入。
  - `PasswordHash`：PBKDF2-SHA256 雜湊，API 永不回傳。
  - `Role`：`Viewer` 或 `Editor`；既有資料及匯入建立資料預設為 `Editor`。

### 2. 生產與品質主檔 (Master Data)
- **Part** (產品料號)：存儲產品資料 (如 `PartNo` 必須唯一)。對於製程與藥液等非產品專屬項目，系統會將其關聯至 `"COMMON"` 共用產品。
- **Process** (工站製程)：如 `ProcessCode` 必須唯一。
- **Machine** (生產機台)：存儲各機台與工作站主檔。
- **QualityCharacteristic** (檢測特性)：定義被測量的物理量 (如膜厚、電壓等)。

### 3. 管制圖設定主檔 (SPC Configuration)
- **ControlChartGroup** (管制圖大分類)：如 `GroupCode` 必須唯一，包含 `PROC` (製程)、`CHEM` (藥液)、`PROD` (產品)。
- **ControlChartCategory** (管制圖中分類)：與大分類關聯，如 `CategoryCode` 必須唯一，包含 `VAR_PROC` (計量_製程) 等。
- **ControlChartType** (管制圖小分類/圖表類型)：管制圖計算配置，包括 `ChartTypeCode` 全局唯一索引，包含 `XBAR_R` (平均數-全距圖)、`XBAR_S` (平均數-標準差圖)、`I_MR` (單值-移動全距圖)。
- **PartProcessCharacteristic** (管制特性設定主檔)：
  - 連接產品、工站、特徵與圖表類型。
  - 儲存產品規格界限：`Lsl` (規格下限)、`Target` (目標值)、`Usl` (規格上限)。
  - 儲存統計管制界限：`Lcl`、`Cl`、`Ucl`（若採用計算界限則動態運算）。
- **ControlLimitSegment** (分段管制界線設定檔)：
  - 儲存特定時間區間內的自訂/試算統計管制界限。
  - 欄位包含：`PartProcessCharacteristicId` (關聯主檔)、`StartDate` (生效起日)、`EndDate` (失效止日，選填)、`UCL`、`CL`、`LCL`、`Note` (備註)。

### 4. 交易資料與檢驗數據 (Transactional Data)
- **MeasurementBatch**：每批次上傳/登錄之總表。
- **VariableMeasurement**：計量型實際測量數值紀錄。
- **AttributeMeasurement**：計數型不良數/總數紀錄。
- **UploadBatch** / **UploadDetail**：供兩階段上傳校驗用的暫存資料表。

## 資料庫優化與索引
- **IX_ControlChartTypes_ChartTypeCode**：唯一索引，防止全系統重複登錄圖表代碼。
- **IX_ControlChartCategories_ChartGroupId_CategoryCode**：複合唯一索引，限制同群組下不可有重複類別。
- **IX_Operators_Username**：可空白的唯一索引，避免登入帳號重複。
- **複合索引**：針對 `PartProcessCharacteristic` 的 FK 關聯與數據表的 `MeasuredAt` 時間欄位建立複合索引，提升 SPC 圖表拉取歷史數據時的效能。
