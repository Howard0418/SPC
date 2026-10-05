# 03 後端 API 規格說明 (Backend API)

- 2026-09-16：[儀器規格欄位](../specs/20260916-calibration-details/spec.md)：instruments GET/POST/PUT 新增可空 measurementSpecification、precision、remarks、calibrationStandard、acceptanceCriteria；最多 2000 字、備註與校正 reason 分開。

- 2026-09-15：[校正 Chat 通知規格](../specs/20260915-calibration-chat/spec.md)。GET/PUT `/api/v1/instrument-calibrations/settings` 新增 notificationChannel（Email/SynologyChat）、chatWebhookConfigured；PUT chatWebhookUrl 空白保留，加密值不回傳。POST `/api/v1/instrument-calibrations/test-chat` 接受可選 chatWebhookUrl（留空用已存值），Editor＋calibration.manage，回傳 success/data.state/errorCode/message；測試不寫通知工作。

> 2026-09-12 新增：[儀器校正 Excel 匯入 API 與格式](import-formats/instrument-calibration.md)；[測試校正通知](../specs/20260912-calibration-test-email/spec.md) `POST /api/v1/instrument-calibrations/test-email`（Editor＋`calibration.manage`，【測試】主旨，不寫入通知表）。

## 基礎資訊
- **API 基底路由**：支援舊版 `/api/` 與標準 `/api/v1/` 雙路由模式。
- **認證機制**：JWT Bearer Token 驗證，需於 HTTP Header 中附加 `Authorization: Bearer <Token>`。
- **角色權限**：
  - `Viewer`：僅允許 GET／HEAD／OPTIONS 查詢。
  - `Editor`：允許完整 CRUD、匯入、確認匯入、異常處置與系統設定。
  - 後端會攔截 Viewer 的非讀取請求並回傳 HTTP 403，前端隱藏操作頁不能取代此安全檢查。
- **登入來源**：優先由 `Operators` 系統使用者主檔驗證帳號與 PBKDF2 密碼；既有 demo 帳號保留為過渡 Editor 管理帳號。
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
- **`POST /api/v1/spc/trial-calculate`**：
  - **功能**：根據指定的 SPC 管制項目及日期範圍，試算歷史量測資料的統計管制上限 (UCL)、中心線 (CL) 及下限 (LCL)。
  - **參數**：`partProcessCharacteristicId` (SPC項目PK), `startDate`, `endDate`。
  - **回傳**：`ucl`, `cl`, `lcl`, `sampleCount` (樣本數), `note`。

### 5. 分段管制線管理 (Control Limit Segments)
- **`GET /api/v1/control-limit-segments`**：取得指定 SPC 項目的所有分段設定（按時間排序）。
- **`POST /api/v1/control-limit-segments`**：建立新的分段管制界線。系統會自動驗證日期範圍是否重疊，若重疊會回傳 `400 BadRequest`。
- **`PUT /api/v1/control-limit-segments/{id}`**：修改現有的分段界線或有效期間。
- **`DELETE /api/v1/control-limit-segments/{id}`**：刪除特定分段界線。

### 6. 安全清理測試資料 (Safe Clean Test Data)
- **`DELETE /api/testdata/clear-transactions`**：
  - **功能**：僅清理交易性、量測性及上傳暫存等測試資料，完全保留所有維護主檔（如料號、工站、檢驗特性、產線及SPC配置等）。
  - **限制**：為安全起見，僅在 Development (開發環境) 下可執行。
  - **清理資料表**：包含 `LotSlotHistories`、`SlotParameters`、`LotSplitHistories`、`LotMasters`、`VariableMeasurements`、`AttributeMeasurements`、`SpcCalculationResults`、`UploadErrors`、`UploadDetails`、`UploadBatches`、`AlertEvents`、`MeasurementValues`、`MeasurementBatches`、`StationOperationSessions`、`WorkOrders`、`MesSyncMessages`。
  - **回傳**：各資料表清理的筆數統計。
- **`DELETE /api/testdata/clear-tagged`**：
  - 只清除 `TEST_`、`E2E_`、`E2E-` 標記的測試交易、上傳批次、警報及測試使用者。
  - 僅 Development 環境可執行，且啟用認證時仍須 Editor Token。
  - 不清除正式料號、製程、機台、品質特性、SPC 設定或正式使用者。

### 7. 系統使用者管理
- **`GET /api/operators`**：Viewer／Editor 均可查詢，回傳帳號、角色及 `HasPassword`，不回傳密碼雜湊。
- **`POST /api/operators`、`PUT /api/operators/{id}`、`DELETE /api/operators/{id}`**：僅 Editor。
- 角色只接受 `Viewer`／`Editor`；未知值安全回退為 `Viewer`。既有資料則由 Migration 明確預設為 `Editor`，兼顧最小權限與相容性。


## 2026-09-17 藥液日報班別／階段契約
- `GET /api/v1/manual-measurements/daily` 保留 machineId/date/samplingPhase，新增可選 `samplingStage=GENERAL|OPEN|CLOSE`。SamplingPhase 的 OPEN/MIDDLE 仍為早班/中班；SamplingStage 的 OPEN/CLOSE 為開線/收線，只允許 N1/N2；非法值回 400 JSON message。
- Portal 代理 `GET /api/spc/measurements/daily` 同樣轉送 samplingStage。
- `POST /api/v1/uploads/variable?portalDaily=true` 每列新增字串 SamplingStage，缺省 GENERAL；預覽與確認驗證線別並依管制項目/台北日期/班別/階段 upsert。舊欄/值不改語意。
- [規格與相容性](../specs/20260917-chemical-shift-stage/spec.md)。


## 2026-09-17 Portal SSO 身分配對
`POST /api/v1/auth/portal-sso` 請求欄位不變。完整簽章 Username/OperatorCode 配對既有操作者，工號登入回傳既有 canonical AD 與同 ID；保留角色/頁面權限。歧義回 409，停用/軟刪除回 401。legacy 簽章僅允許既有同 AD，未簽欄位不採用，首次建檔回 409（不再降級另建）。[規格](../specs/20260917-sso-single-operator/spec.md)。

## 製程量測歷史分頁（2026-09-17）
GET /api/processes/{id}/measurements 新增 skip（預設0）、start/end（可選，包含端點），依 MeasuredAt/Id 降冪分頁；total為日期篩選後總筆數，rows/原預設take=200相容。負skip或反向日期回400。Portal代理每頁上限500，頁面分批讀取至total並偵測重複／總數變動。

## 藥液日期摘要（2026-09-17）
GET /api/v1/manual-measurements/calendar：machineId/year/month/samplingPhase/samplingStage，回days(date,hasData,hasHistory)。僅月份內有效資料，日報日期優先；精確班別階段且手動日報標hasData，未分類／非日報另標history。Portal同品保權限代理/api/spc/measurements/calendar。無資料寫入。

### 2026-09-17 舊班別對應修正
calendar與daily對OPEN早班同時納入GENERAL／空白班別；daily省略班別亦依早班解讀。PortalDaily寫回比對同義班別並沿用ID；衝突不任選。SourceType與SamplingStage既有界線保留。

## 2026-09-17 日報日期回退
GET /api/v1/manual-measurements/daily：缺PortalDailyDate的手動資料以MeasuredAt當日半開區間載入；已有日報日期優先。calendar同步標可載入。PortalDaily預覽/更新採同規則，更新沿用ID並補PortalDailyDate（before稽核保留原值）；重複項目拒絕。N1/N2舊CLOSE不重新分類。

## 2026-09-17 完整咬蝕日報同步
PUT /api/v1/etch-reports（Admin/Editor）接受reportDate、lineCode、lineSpeed、operatorName、points[side,repeatNo,opNo,beforeValue,afterValue]及可選provenance。完整25/50點/面、負值拒絕；交易＋同日同線SQL applock，SourceReference穩定業務鍵、PPC＋SampleNo更新原ID，UploadBatch/Detail留來源及前值。不使用PortalDailyDate每日單值唯一鍵，不影響藥液。相同內容回unchanged；其他來源同日期衝突409。Portal代理PUT /api/spc/etch-reports保留品保權限。

### 2026-09-18 N1/N2藥液圖表
GET /api/v1/spc/chart 的I-MR點位增加samplingStage；CHEM N1/N2同管制項目依日期→開線/收線/未分→班別→時間/ID排序，試算同序。點數/原始值保留，不按日班別折疊。
## Particle Monitoring 匯入 API（2026-10-01）

- `POST /api/v1/uploads/particle/preview`：接收 TransFiles 產生的 Long Format rows，建立 `UploadType=Particle` 預覽；逐筆驗證 ISO 8601 時間、R1～R9、0.5／1／5／10 μm、非負整數 Count 與來源座標，並回傳有效／錯誤／疑似重複筆數。
- `GET /api/v1/uploads/{uploadBatchId}/preview`：沿用既有 staging 預覽，回傳 batch、details 與 errors；Particle detail payload 含正規化 UTC 值與 `IsDuplicate`。
- `POST /api/v1/uploads/{uploadBatchId}/confirm?duplicateMode=reject|skip`：Particle batch 預設遇跨批疑似重複整批拒絕；`skip` 略過重複後交易寫入其餘有效資料。已 Imported batch 重送不重複寫入。
- 同一檔案 SHA-256 已完成匯入回 409 `DUPLICATE_FILE`；格式錯誤或疑似重複衝突分別回 409 `VALIDATION_ERRORS`／`DUPLICATE_MEASUREMENT`。

## Particle Monitoring 查詢 API（2026-10-01）

- `GET /api/v1/particles/measurements`：日期區間必填，可依 locations、particleSizes、deviceCode、uploadBatchId 篩選；回傳穩定排序的分頁原始資料，pageSize 最大 500。
- `GET /api/v1/particles/trend`：日期、單一 location 與 particleSize 必填；只回傳該獨立 sequence，超過 10,000 點回 422 `TOO_MANY_POINTS`。
- `GET /api/v1/particles/location-comparison`：依 measurementTime／particleSize 比較 R1～R9；缺測 count 為 null。同時間有多個來源事件時回 409 `COMPARISON_AMBIGUOUS` 與 batch／sheet／row／device 候選。
- `GET /api/v1/particles/spc`：日期、單一 location 與 particleSize 必填，`chartType=C|U`（預設 C），依 DeviceCode 隔離 sequence。少於 20 點回 `controlStatus=insufficientData`；C 圖標示 `samplingBasis=constant-assumed`，U 圖依 decimal SamplingVolume 正規化並回單位。U 圖缺分母／單位或混用單位分別回 422 `SAMPLING_VOLUME_REQUIRED`／`SAMPLING_VOLUME_UNIT_MISMATCH`。

