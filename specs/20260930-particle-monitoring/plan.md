# 技術計畫
- 功能 ID：20260930-particle-monitoring
- 規格版本：6
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- SPC backend：Particle Long Format 資料模型、匯入 API、查詢 API、SPC sequence 轉換。
- SPC frontend：匯入預覽、查詢篩選、趨勢圖、位置比較圖、SPC 圖入口。
- TransFiles：若沿用轉檔工具，負責 Excel 橫向格式解析與送入 SPC 預覽。

## 實作方式與需求對應
- 優先沿用既有 UploadBatch、Preview/Confirm、UploadError、SPC chart result 結構。
- 不沿用 `R#_粒徑` 作為固定管制項目的資料模型。
- Particle sequence 以 Location + ParticleSize 組成。
- SPC 提供 C/U 圖型選擇；Particle rows 經專用 adapter 接入既有規則，並保留 bigint、decimal SamplingVolume 與同時間重測保護。

## API、檔案格式及資料模型
- 新增專用表 `ParticleMeasurements`，不修改 `AttributeMeasurements` 結構。
- 核心欄位：`UploadBatchId`、`MeasurementTime`、`Location`、`ParticleSize`、`Count`。
- 來源追溯：`SourceSheet`、`SourceRow`、`SourceColumn`、`RawValue`。
- 預留欄位：`SamplingVolume`、`SamplingDurationSeconds`、`DeviceCode`、`Remark`。
- 索引：
  - `(Location, ParticleSize, MeasurementTime)`：趨勢與 SPC sequence。
  - `(ParticleSize, MeasurementTime, Location)`：同時間位置比較。
  - `UploadBatchId`：批次追溯。
  - `(UploadBatchId, SourceSheet, SourceRow, SourceColumn)` unique：同批次來源儲存格冪等。
- 不建立 `(MeasurementTime, Location, ParticleSize)` 唯一索引；合法重測與不同儀器資料不可在 DB 層被誤判為重複。

## 匯入預覽與確認流程
1. TransFiles 讀取原始 Excel，第 24／25 列建立 Location／ParticleSize 欄位映射，第 26 列起將每個非空量測格展開為 Long Format row。
2. TransFiles 將原始檔名、SHA-256、client batch ID 與含來源座標的 rows 送至 `POST /v1/uploads/particle/preview`。
3. SPC 正規化時間、Location、ParticleSize、Count，建立 `UploadType=Particle` staging，回傳有效、錯誤與疑似重複計數。
4. 使用者由 preview 明細確認；任何格式錯誤均阻擋整批 confirm。疑似重複預設以 `reject` 阻擋，可明確選 `skip` 後只匯入非重複 rows。
5. confirm 在單一 transaction 寫入 `ParticleMeasurements` 並將 batch 標記 Imported；重送同一 confirm 不重複寫入。

第一版不提供 update／overwrite；若資料錯誤，應排除原批次或另開受控修正流程，避免匯入時靜默改寫歷史量測。

## 查詢與圖表投影
- `measurements` endpoint 使用 `(Location, ParticleSize, MeasurementTime)` 或 `(ParticleSize, MeasurementTime, Location)` 索引依篩選條件查詢，並以 `MeasurementTime + Id` 穩定排序及分頁。
- `trend` endpoint 必須先套用單一 Location／ParticleSize／DeviceCode，再投影時間與 Count；不在後端做會改變原始點位的聚合或補值。
- `location-comparison` 以 `UploadBatchId + SourceSheet + SourceRow + MeasurementTime + ParticleSize` 表示一次橫向來源事件；R1～R9 缺值回 null，前端可顯示缺測但不可畫成 0。
- 同時間存在多個候選事件時回 409 與候選清單，前端需讓使用者選擇後重查。
- 規格線資料與後續 SPC 管制界線使用不同 response 欄位，避免 USL／LSL 與 UCL／CL／LCL 混用。

## SPC engine 接法
- Particle SPC endpoint 以 `chartType=C|U` 選擇圖型；同一 `Location + ParticleSize + DeviceCode` 依 `MeasurementTime + Id` 排序後投影至 Particle 專用 adapter。
- adapter 沿用現有 C-chart 的 `cBar ± 3*sqrt(cBar)`、LCL 不小於 0 與 Western Electric 規則，但保留 bigint 輸入檢查及 measurement id，不直接複製既有 Attribute staging／PPC 模型。
- 少於 20 個有效基準點不估算管制線、不跑規則，只回原始點與資料不足狀態。
- 相同 MeasurementTime 的合法重測以 Id 維持穩定順序；結果關聯使用 measurement id，避免現有 calculator 以時間建 dictionary 時的重複 key 問題。
- U-chart 使用 decimal SamplingVolume 與明確單位；缺分母或混用單位即拒絕。I-MR 僅保留未來人工選用評估，不自動 fallback。
- 實作前先建立 calculator／adapter 單元測試，至少涵蓋序列隔離、20 點門檻、bigint 邊界、相同時間重測、LCL=0、規格線與管制線分離。

## 相容性與風險
- 前案 DUST 匯入與主檔可能已建立 `R#_粒徑` 項目；需盤點測試庫/正式庫後決定相容策略。
- 若直接塞入 AttributeMeasurement，Location/ParticleSize 會缺少一級欄位，不利查詢與擴充。
- U-chart 分母若固定 1，統計語意需標示，避免誤認為已依採樣體積正規化。
- 既有 SPC Web `VariableUploadView` 的 DUST parser 與 TransFiles DUST parser 不一致；新功能需指定唯一入口，避免兩條匯入流程產出不同資料模型。
- 相容建議：保留 DUST group/chart type；凍結舊 `R#_粒徑` 自動補主檔路徑或只保留舊資料查詢；新 Long Format 不再依賴 PPC 作為序列主鍵。

## 驗證安排
- 匯入解析測試：一筆時間 × R1～R9 × 4 粒徑轉 36 筆。
- 驗證錯誤測試：日期、位置、粒徑、Count。
- 查詢測試：R8 + 0.5 μm 不含 R9。
- 圖表測試：趨勢與位置比較資料集正確。
- SPC 測試：同一 Location + ParticleSize 形成獨立 sequence。

## 發布、備份及回復
- 本小工作僅文件草稿，發布不適用。
- 後續實作若含 DB migration，需先測試站備份與回復計畫。
