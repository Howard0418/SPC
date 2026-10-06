# SPC-POINT-FILTER 實作計畫

## 最小修改方向

採「新增單一圖點排除狀態」而非改原始量測資料。後端計算先合併排除狀態，再回傳圖表資料。前端只負責右鍵選單、狀態顯示、已排除點清單與呼叫 API。

## 受影響範圍

- 後端：
  - `backend/MesSpc.Api/Domain/Entities/Models.cs`
  - `backend/MesSpc.Api/Infrastructure/Data/AppDbContext.cs`
  - `backend/MesSpc.Api/Controllers/SpcController.cs`
  - `backend/MesSpc.Api/Services/SpcService.cs`
  - `backend/MesSpc.Api/SpcEngine/Models/SpcModels.cs`
  - EF migration 與 snapshot
- 前端：
  - `frontend/mes-spc-web/src/views/SpcChartView.vue`
  - `frontend/mes-spc-web/src/views/TrendChartView.vue`
- 測試：
  - `tests/MesSpc.Api.Tests`
  - `frontend/mes-spc-web/tests`

## 建議資料模型

新增表：`SpcPointExclusions`

欄位建議：

- `Id`
- `PointScope`：`VariableMeasurement`、`AttributeMeasurement`、`Subgroup`
- `PartProcessCharacteristicId`
- `VariableMeasurementId` nullable
- `AttributeMeasurementId` nullable
- `MeasurementBatchId` nullable
- `PointKey`：用於子組/聚合點，例如 `ppcId + measuredAt + samplingPhase + samplingStage + sideCode`
- `State`：`ExcludedVisible` 或 `ExcludedHidden`
- `Reason`
- `CreatedBy`
- `CreatedAt`
- `UpdatedBy`
- `UpdatedAt`
- `IsActive`

索引建議：

- `PointScope + VariableMeasurementId + IsActive`
- `PointScope + AttributeMeasurementId + IsActive`
- `PartProcessCharacteristicId + PointKey + IsActive`

## API 設計

- `GET /api/v1/spc/point-exclusions`
  - 依目前查詢條件取得已排除點清單。
- `PUT /api/v1/spc/point-exclusions`
  - 設定單一點狀態：`ExcludedVisible` 或 `ExcludedHidden`。
- `DELETE /api/v1/spc/point-exclusions/{id}`
  - 恢復單一點為 `Normal`。

## 計算整合

- `SpcService.GetInteractiveChartAsync` 查詢量測資料後，讀取目前查詢範圍內的 active exclusions。
- 建立 `SpcDataPoint/Subgroup/AttributeDataPoint` 時套用：
  - `IsExcluded = State != Normal`
  - `ExclusionState`
  - `ExclusionId`
  - `ExcludedBy`
  - `ExcludedAt`
- 計算使用既有 `isExcluded` 排除邏輯。
- 回傳時仍包含 `ExcludedVisible` 點；`ExcludedHidden` 點須在 raw data/排除清單可見，但圖表 series 不渲染。

## 前端設計

- 管制圖：
  - 點位右鍵顯示 context menu。
  - 選項：顯示但不列入計算、隱藏且不列入計算、恢復列入計算。
  - 工具列新增「已排除點 N」。
- 趨勢圖：
  - 同管制圖。
  - 直方圖與常態檢定使用後端排除後結果。
- 隱藏點恢復：
  - 從「已排除點」面板或 raw data/明細恢復。

## 相容性

- 保留既有 UploadBatch 整批排除，不在第一版移除。
- 新單點排除優先於整批狀態；若整批已排除，單點恢復是否可覆蓋整批需另行確認。第一版建議：整批排除仍優先，單點恢復不覆蓋整批。

## 驗證策略

- 後端單元/整合測試：
  - 單一點 ExcludedVisible / ExcludedHidden 均不列入計算。
  - Hidden 清單可查且可恢復。
  - Xbar 子組排除後平均/標準差/能力指標改變。
  - 趨勢圖 normality 只用未排除點。
- 前端測試：
  - 右鍵選單可開啟並呼叫 API。
  - 已排除點清單可恢復隱藏點。
  - 隱藏點不出現在圖上。

## 發布

- 程式變更完成後需建置後端與前端。
- 測試通過後發布 SPC 測試站 backend/frontend。
- 正式站另行授權。
