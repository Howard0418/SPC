# 功能規格：落塵／Particle 粒子監控 Long Format
- 功能 ID：20260930-particle-monitoring
- 版本：6
- 狀態：完成
- 涉及專案：SPC、TransFiles
- 授權依據：使用者逐項確認 T-001～T-015，授權完成規格、實作、驗證及 SPC 測試站發布。
- 現行需求基準：[需求索引](../../docs/requirements.md)
- 前案或相關規格：
  - [藥液與落塵監控擴充](../20260929-chemical-dust-monitoring/spec.md)

## 目的、現況與證據
使用者要求新增「落塵／Particle 粒子監控」功能，Excel 原始資料為橫向格式，但系統內需採 Long Format：一筆資料代表某時間、某位置、某粒徑的一個 Count 量測結果。

已確認需求：
- Location 為 R1～R9。
- ParticleSize 為 0.5、1、5、10 μm。
- 9 個位置 × 4 個粒徑需形成 36 條可獨立分析的監控序列。
- R8 + 0.5 μm 與 R9 + 0.5 μm 不可混成同一條時間序列。
- 不應建立 `R1_0.5`、`R1_1`、`R1_5`、`R1_10` 這類大量固定欄位或固定管制項目作為主要資料模型。
- Specification（USL/LSL）與 Statistical Control Limit（UCL/CL/LCL）必須分開，不可把 USL 當 UCL。

程式觀察：
- SPC 已有 AttributeMeasurement、DUST group、DUST_C、DUST_U、Attribute chart calculator、Excel upload preview/confirm 與 SPC chart 查詢框架。
- 前案已有落塵初步支援，但偏向將 `R#_粒徑` 編進管制項目；此方式不符合本規格 Long Format 原則，後續需受控調整。
- TransFiles 目前 `dust_reports.py` 會輸出 SPC Attribute Excel，`CharacteristicCode = {Location}_{ParticleSize}um`，例如 `R1_0.5um`。
- SPC `UploadService` 目前對 `DUST` scope 預設補建 `DUST_U` 管制項目，並以品質特性 / PPC 承接 `R#_粒徑`。
- SPC Web `VariableUploadView.vue` 另有舊落塵橫向 parser，將 R1～R12 視為 Process/Machine，且送 variable upload；此路徑與現行 TransFiles Attribute 路徑不一致。

## 範圍與非範圍
範圍：
- 設計 Particle Long Format 匯入、查詢、趨勢、位置比較與 SPC sequence 整合方式。
- 分析既有 SPC 架構可沿用部分與需新增部分。
- 規劃 DB schema、Excel 轉換、Chart Engine 接法與小工作拆分。

非範圍：
- 本階段不寫程式、不改 DB、不發布。
- 不重構藥液、咬蝕、儀器、校正等無關功能。
- 不在本規格硬指定必定使用 U-chart、C-chart 或 I-MR；需先依資料與引擎能力決定。
- 不實作 PM2.5、PM10、TSP、溫度、濕度、Sampling Volume、Sampling Duration、儀器、廠區、製程、機台、班別等未來欄位；僅確保設計不阻礙擴充。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | Excel 橫向資料匯入後需轉為 Long Format | AC-001 | 同一量測時間 R1～R9、0.5/1/5/10 μm 轉為最多 36 筆資料，每筆含 MeasurementTime、Location、ParticleSize、Count |
| R-002 | 匯入需保留原始追溯性 | AC-002 | 每筆資料可追溯 UploadBatch、來源檔、工作表、列、欄與原始值 |
| R-003 | 查詢需支援日期區間、Location、ParticleSize | AC-003 | 使用者可篩選 R8、0.5 μm、指定日期區間，只取得該獨立序列 |
| R-004 | 趨勢分析需支援單一 Location + ParticleSize | AC-004 | R8 + 0.5 μm 顯示時間 vs Count 趨勢圖，且不混入 R9 |
| R-005 | 位置比較需支援同時間、同粒徑比較 R1～R9 | AC-005 | 選定 0.5 μm 與量測時間後，以長條圖比較 R1～R9 Count |
| R-006 | SPC sequence 由 Location + ParticleSize 獨立形成 | AC-006 | R8 + 0.5 μm 可獨立產生 SPC 圖，不與其他位置或粒徑混合 |
| R-007 | 規格與管制界線需分離 | AC-007 | USL/LSL 與 UCL/CL/LCL 在資料模型、API、圖表上分別表示 |
| R-008 | 設計需保留未來擴充欄位空間 | AC-008 | DB/API 設計不需破壞性重做即可增加 SamplingVolume、Device、Shift 等欄位 |

## 例外與邊界
- 日期不可解析、Location 不在允許清單、ParticleSize 不在允許清單、Count 非數字或小於 0 時，需在預覽階段列錯誤。
- 同一來源檔或同一量測時間、Location、ParticleSize 重複時，需定義略過、覆蓋或衝突拒絕策略。
- 若 SamplingVolume 未提供，U-chart 分母不可臆測；可先將 UnitCount=1 視為暫行策略，但需清楚標示語意限制。
- 若既有 DUST 資料已用 `R#_粒徑` 管制項目方式建立，需另列轉換或並存策略，不可直接覆蓋。

## 假設與未決問題
- 假設第一版 Location 固定 R1～R9；前案 R1～R12 需確認是否保留相容或改為 R1～R9。
- 已決定：Particle data 使用全新專用表 `ParticleMeasurements`；不擴充 `AttributeMeasurement`，也不以 PPC 承接 Long Format 主資料。
- 已決定：SPC 第一版採 C-chart；U-chart 與 I-MR 的適用邊界依下方圖型策略辦理。
- 待確認：SamplingVolume、SamplingDuration 是否會出現在近期 Excel。
- 待確認：重複資料策略與匯入確認模式。

## 既有 DUST 相容盤點
- 已存在能力可保留：`DUST` control scope、`DUST_C`/`DUST_U` chart type、Attribute C/U calculator、DUST 維度辨識。
- 舊方案資料入口：TransFiles 解析 Excel 後產出 Attribute upload rows，將 Location + ParticleSize 合併為 `CharacteristicCode`，例如 `R8_0.5um`。
- 舊方案主檔效果：SPC 預覽補主檔會建立品質特性與 PPC，因此 36 條序列會變成 36 個檢驗項目 / 管制項目。
- 舊方案問題：可以製圖，但資料模型沒有一級欄位表示 Location、ParticleSize；查詢、位置比較、未來 PM2.5/PM10/TSP 或 SamplingVolume 擴充會受限。
- 建議相容策略：保留既有 DUST chart type 與已匯入 Attribute 歷史資料可查；新 Particle Long Format 走新資料表 / 新 API，不再新增 `R#_粒徑` PPC 作為主模型。若已有舊資料需轉換，另開 migration/backfill 任務，不在第一版直接覆蓋。

## Particle DB schema 決策
- 資料表：`ParticleMeasurements`，一列代表單一量測時間、單一 Location、單一 ParticleSize 的 Count。
- 主鍵：`Id`（`bigint` identity）。
- 必填欄位：`UploadBatchId`（`uniqueidentifier`）、`MeasurementTime`（`datetime2`）、`Location`（`nvarchar(16)`）、`ParticleSize`（`decimal(6,3)`，單位 μm）、`Count`（`bigint`，不得小於 0）、`SourceSheet`（`nvarchar(128)`）、`SourceRow`（`int`）、`SourceColumn`（`nvarchar(16)`）、`RawValue`（`nvarchar(256)`）。
- 選填擴充欄位：`SamplingVolume`（`decimal(18,6)`）、`SamplingVolumeUnit`（`nvarchar(32)`）、`SamplingDurationSeconds`（`decimal(18,3)`）、`DeviceCode`（`nvarchar(64)`）、`Remark`（`nvarchar(500)`）。
- 共通稽核欄位沿用 `BaseEntity<long>` 慣例；`UploadBatchId` 外鍵指向既有 `UploadBatches`，刪除採 Restrict。
- 查詢索引：`(Location, ParticleSize, MeasurementTime)` 支援獨立趨勢／SPC sequence；`(ParticleSize, MeasurementTime, Location)` 支援同粒徑的位置比較；`UploadBatchId` 支援上傳追溯與回查。
- 冪等唯一索引：`(UploadBatchId, SourceSheet, SourceRow, SourceColumn)`，避免同一預覽確認重複寫入。
- 第一版不對 `(MeasurementTime, Location, ParticleSize)` 建唯一索引：同時刻可能存在不同儀器或合法重測；跨批次重複由預覽顯示衝突，待匯入契約小工作決定處理策略。
- `Location` 與 `ParticleSize` 第一版由應用層白名單驗證（R1～R9、0.5／1／5／10），資料庫保留未來擴充值，不建立固定欄位或每組 PPC。

## Excel 橫向轉 Long Format 預覽契約
- 第一版唯一新入口為 TransFiles：TransFiles 解析原始橫向 Excel，送至 SPC 專用 Particle preview API；SPC Web 舊 DUST variable parser 與 Attribute upload 不作為新 Long Format 入口。
- 原始版型以工作表第 24 列辨識 Location、第 25 列辨識 ParticleSize、第 26 列起讀資料；量測時間沿用 B 欄。第一版僅接受 R1～R9 與 0.5／1／5／10 μm。
- 每個非空量測格轉成一筆 row：`measurementTime`、`location`、`particleSize`、`count`、`sourceSheet`、`sourceRow`、`sourceColumn`、`rawValue`；可附 `samplingVolume`、`samplingDurationSeconds`、`deviceCode`、`remark`。
- 同一來源資料列最多展開 36 筆。空白量測格略過；非空但無法解析的值不得略過，必須成為預覽錯誤並保留來源座標。
- TransFiles 呼叫 `POST /v1/uploads/particle/preview`，JSON request 含 `clientBatchId`、`originalFileName`、`fileHashSha256`、`sourceType=TransFiles` 與 `rows`。SPC 不信任用戶端驗證結果，仍逐筆驗證後寫入既有 `UploadBatch`／`UploadDetail`／`UploadError` staging，`UploadType=Particle`。
- preview response 含 `uploadBatchId`、`importStatus`、`totalRows`、`validRows`、`errorRows`、`duplicateRows`；明細查詢沿用 `GET /v1/uploads/{uploadBatchId}/preview`，每筆回傳正規化 row、來源座標、`isValid`、`isDuplicate` 與 errors。
- 驗證錯誤碼至少包含：`MEASUREMENT_TIME_INVALID`、`LOCATION_UNSUPPORTED`、`PARTICLE_SIZE_UNSUPPORTED`、`COUNT_INVALID`、`COUNT_NEGATIVE`、`SOURCE_COORDINATE_INVALID`、`DUPLICATE_SOURCE_CELL`、`DUPLICATE_MEASUREMENT`。
- 同批次來源座標重複屬硬錯誤。跨批次 `(MeasurementTime, Location, ParticleSize, DeviceCode)` 相同視為疑似重複；空白 `DeviceCode` 視為同一預設儀器範圍。
- 確認使用 `POST /v1/uploads/{uploadBatchId}/confirm?duplicateMode=reject|skip`；預設 `reject`，有跨批次疑似重複時整批不寫入。`skip` 僅略過疑似重複，寫入其餘有效 rows，回傳 inserted／skipped／errors 計數；第一版不提供覆蓋模式。
- 同一 `fileHashSha256` 已完成匯入時回 409；若僅有未確認 preview，可取代舊 preview。已 Imported 批次重送 confirm 必須冪等回傳原結果，不可重複寫入。

## 查詢 API 與圖表資料契約
- 所有時間參數使用含時區的 ISO 8601，SPC 正規化為 UTC 儲存與查詢，response 以 UTC `Z` 回傳；畫面再依使用者時區顯示。
- 原始量測：`GET /api/v1/particles/measurements`。必要參數 `from`、`to`；選填 `locations`、`particleSizes`、`deviceCode`、`uploadBatchId`、`page`、`pageSize`、`sort=asc|desc`。`pageSize` 預設 100、最大 500。
- 原始量測 response：`items`、`total`、`page`、`pageSize`；item 含 measurement id、時間、Location、ParticleSize、Count、採樣／儀器欄位及 batch／來源座標。排序固定以 `MeasurementTime`、`Id` 作穩定排序。
- 趨勢：`GET /api/v1/particles/trend`。必要參數 `from`、`to`、單一 `location`、單一 `particleSize`；選填 `deviceCode`。response 含 `seriesKey`、`unit=count`、`points[{measurementId,time,count}]`、`pointCount`、`specification{usl,lsl}`。
- 趨勢查詢不得混入其他 Location、ParticleSize 或 DeviceCode。第一版不做靜默聚合／降採樣；超過 10,000 點回 422 `TOO_MANY_POINTS`，要求縮小日期範圍，避免統計圖與原始資料不一致。
- 位置比較：`GET /api/v1/particles/location-comparison`。必要參數 `particleSize`、`measurementTime`；選填 `deviceCode`、`uploadBatchId`、`sourceSheet`、`sourceRow` 以鎖定同一量測事件。
- 比較 response 固定依 R1～R9 排序，回傳 `items[{location,count,measurementId,isMissing}]`；缺值保留 Location 並以 `count=null` 表示，不補 0。
- 若同一時間／粒徑存在多個合法量測事件且未提供足以鎖定事件的條件，回 409 `COMPARISON_AMBIGUOUS`，附 `candidates[{uploadBatchId,sourceSheet,sourceRow,deviceCode}]`；不得任意取第一筆或平均。
- `specification` 僅承載 USL／LSL，且可為 null；UCL／CL／LCL 僅由後續 SPC endpoint 回傳，趨勢與位置比較契約不得把規格線命名為管制界線。
- 參數錯誤統一回 400：日期範圍顛倒、Location／ParticleSize 不支援、page 範圍無效；查無資料回 200 空集合，不以 404 表示。

## 第一版 SPC 圖型策略
- Particle Monitoring 提供 C-chart／U-chart 選擇：C-chart 使用原始 Count；U-chart 使用 `Count / SamplingVolume`。sequence 均以 `Location + ParticleSize + DeviceCode` 分離，空白 DeviceCode 視為同一預設儀器。
- C-chart response 必須標示 `samplingBasis=constant-assumed` 與警示文字，明確表示目前未依採樣體積正規化；不得讓使用者誤認為是 U-chart。
- U-chart 只有查詢範圍內每個點都有大於 0、單位一致的 `SamplingVolume` 才可啟用；任一點缺少分母即回 422 `SAMPLING_VOLUME_REQUIRED`，不得以 1 代替。使用 Particle 專用 decimal 分母 calculator，不經 int 型 `AttributeDataPoint.UnitCount`。
- I-MR 不作為自動 fallback：它適合連續個別值，直接套用離散 particle count 會改變統計假設。未來若經製程工程確認，可作明確選用的替代分析，但不與 C-chart 自動切換。
- `GET /api/v1/particles/spc` 必要參數 `from`、`to`、單一 `location`、單一 `particleSize`，選填 `deviceCode`、`chartType=C|U`；預設 C。
- SPC response 含 `seriesKey`、`chartType`、`samplingBasis`、`warning`、`pointCount`、`controlStatus`、`statControlLimits{ucl,cl,lcl}`、`specification{usl,lsl}`、`points[{measurementId,time,count,ucl,cl,lcl,isOutOfControl,violatedRules}]`。
- 可計算點數少於 20 時，回 `controlStatus=insufficientData`，保留原始點與 specification，但 `statControlLimits=null` 且不執行管制規則判定；20 點以上才建立初始統計管制界線。
- 實作可沿用 `AttributeChartCalculator` 的 C-chart 公式與 Western Electric 規則，但需建立 Particle adapter／專用入口：禁止未檢查地將 `bigint Count` 轉成 int，並以 measurement id／穩定序號關聯結果，不得以 `MeasuredAt` 當唯一 key，因合法重測可有相同時間。
- USL／LSL 與 UCL／CL／LCL 維持不同 response 物件；超規格與失控為兩種不同旗標，畫面不可共用同一判定或圖例名稱。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-09-30 | 建立 Particle Monitoring Long Format 草稿 |
| 2 | 2026-10-01 | 使用者確認方向；決定專用 Long Format 資料表、欄位、索引與冪等鍵 |
| 3 | 2026-10-01 | 定義 TransFiles 橫向轉 Long Format、SPC preview／confirm 與重複處理契約 |
| 4 | 2026-10-01 | 定義原始量測、趨勢與位置比較 API、時區、分頁及歧義處理契約 |
| 5 | 2026-10-01 | 定義第一版採 C-chart、最低基準點數、採樣假設及引擎相容邊界 |
| 6 | 2026-10-01 | 使用者確認 Particle Monitoring 提供 C/U 圖型選擇；U 圖強制有效 SamplingVolume |
