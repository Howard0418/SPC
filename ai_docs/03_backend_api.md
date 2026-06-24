# 03 後端 API 規格說明 (Backend API)

## 基礎資訊
- **API 基底路由**：支援舊版 `/api/` 與標準 `/api/v1/` 雙路由模式。
- **認證機制**：可配置的 JWT Bearer Token 驗證，需於 HTTP Header 中附加 `Authorization: Bearer <Token>`。
- **文件工具**：整合 Swagger UI，開發時可於 `http://localhost:5243/swagger` 進行直接調試。

## 關鍵 API 端點定義

### 1. SPC 系統遷移與資料初始化 (Migration)
- **`POST /api/v2/migration/import-custom-spc`**：
  - **功能**：客製化 SPC 管制項目增量匯入端點。
  - **參數**：傳入上傳之 Excel 檔案（格式為 `IFormFile file`）。
  - **作業流程**：
    1. 解析 Excel 中的三個工作表（製程管制項目、藥液管制項目、產品管制項目）。
    2. 自動確保大分類（Group：`PROC`, `CHEM`, `PROD`）與中分類（Category：`VAR_PROC`, `VAR_CHEM`, `VAR_PROD`）存在。
    3. 自動關聯或建立缺失的 `Part`（料號，化學與製程項目預設為 `COMMON`）、`Process`（工站）、`Machine`（機台）與 `QualityCharacteristic`（特徵項目）。
    4. 自動新增或更新 `PartProcessCharacteristic`（規格上限/目標/下限、管制圖類型等），實現高可重複性 (Idempotent) 的 UPSERT 匯入。

### 2. 量測數據上傳 (Uploads)
- **`POST /api/v1/uploads/variable/excel`**：上傳計量型量測數據 Excel 進行第一階段格式與第二階段主檔校驗。
- **`POST /api/v1/uploads/attribute/excel`**：上傳計數型量測數據 Excel 進行校驗。
- **`GET /api/v1/uploads/{uploadBatchId}/preview`**：取得暫存批次的預覽資料與錯誤校驗報告。
- **`POST /api/v1/uploads/{uploadBatchId}/confirm`**：確認將校驗無誤的資料正式寫入量測歷史表，並觸發 `SpcEngine` 即時警報判定。

### 3. 主數據管理 (Master Data CRUD)
- 提供對 `Products`, `Parts`, `Processes`, `Machines` 以及 `ControlChartCategories` 的標準 CRUD 端點（同時支援雙路由）。
- **`GET /api/machines/{id}/tanks`**：取得指定機台對應產線下的所有槽位清單（`Tanks` 表）。
- **`POST /api/machines` 與 `PUT /api/machines/{id}`**：在請求中可包含 `Tanks` 陣列（含有 `Id`, `TankCode`, `TankName`, `IsActive`），後端會自動在 `SyncMachineTanksAsync` 中確保 `ProductionLine` 存在、同步更新產線代碼，並比對資料庫進行槽位的新增、更新或刪除（UI 中被移除的槽位會自資料庫中刪除，若有 SPC 管制項目關聯會被 DB 外鍵約束阻擋並回傳適當錯誤）。

### 4. SPC 數據查詢 (SPC Analysis)
- **`GET /api/v1/Spc/Chart`** 與 **`GET /api/v1/Spc/InteractiveChart`**：
  - 根據產品 ID、工站 ID、檢驗項目 ID 拉取最近 N 筆數據。
  - 呼叫後端 `SpcEngine` 計算管制界限、製程能力指標，並回傳標記有異常點與觸犯規則清單的資料點。
  - **常態分佈擴充**：針對計量型 (Variable) 管制項目，若有效點數 $\ge 3$，後端會自動計算 Jarque-Bera 常態性檢定結果（偏態、峰態、統計量與 $p$-value），並產生 100 點經 $ScaledPdf = PDF \times n \times binWidth$ 縮放的平滑常態曲線數據，供前端進行直方圖與曲線的重疊繪製。
