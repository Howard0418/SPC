# 技術計畫
- 功能 ID：20260909-chemical-entry-and-chart
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- SPC Web：`frontend/mes-spc-web/src/views/SpcChartView.vue` 計算主圖與趨勢圖的 y 軸範圍。
- SPC API：`ManualMeasurementsV1Controller` 接受中班並刪除日報單筆；`UploadService` 接受中班並依班別去重；EF migration 不需調整結構，`SamplingPhase` 為既有可擴充字串欄位。
- Portal API：`SpcProxyController` 新增受權限保護的刪除代理。
- Portal Web：`VariableMeasurementEntry.cshtml` 加入中班選項、名稱呈現與刪除按鈕/確認/重新查詢。

## 實作方式與需求對應
- R-001：抽出純 JavaScript y 軸界線計算器，收集量測與可見界線，以範圍比例加上最小值邊距；主圖與趨勢圖共用。
- R-002：將 `MIDDLE` 加入 SPC 正規化；Portal N1／N2 下拉加上中班且以一致名稱顯示。
- R-003：SPC 刪除時以 ID 鎖定單筆，限制 `SourceType.Manual` 且 `PortalDailyDate` 不為空，交易刪除關聯計算、警示、量測並保留匯入稽核批次；Portal 僅轉發並顯示結果。

## API、檔案格式及資料模型
- 新增 `DELETE /api/v1/manual-measurements/items/{id}`，回傳被刪除 ID；不接受非 Portal 日報來源。
- 新增 `DELETE /api/spc/measurements/{id}` 作 Portal 品保權限代理。
- `SamplingPhase` 新增值 `MIDDLE`，不變更既有索引 `(PartProcessCharacteristicId, PortalDailyDate, SamplingPhase)`。

## 相容性與風險
- `OPEN`、`CLOSE`、`GENERAL` 資料保持不變；未知值仍回退 `GENERAL`。
- 刪除只影響選定單筆及其衍生資料，不刪除 UploadBatch/UploadDetail，避免破壞既有稽核資料。
- y 軸只在有可顯示數值時指定範圍，避免空圖呈現異常。

## 驗證安排
- AC-001：前端建置與針對 y 軸計算器的 Node 測試。
- AC-002／AC-003：`ManualMeasurementsV1ControllerTests` 驗證中班讀取、刪除關聯資料與非日報拒絕；`SpcProxyControllerTests` 驗證 DELETE 代理。
- Portal／SPC 各自建置，測試站發布後以 HTTP health 與登入後畫面/API 執行驗收。

## 發布、備份及回復
- 僅發布至測試環境：SPC API/Web 與 Portal API/Web 的既有 test 發布目錄；發布前保存各站目前部署版本，發布後回收對應 App Pool。
- 不發布正式環境；回復方式為還原測試發布目錄備份並回收 App Pool。資料 schema 不變，無資料庫 migration 或資料修補。# 技術計畫
功能 ID：20260909-chemical-entry-and-chart｜規格版本：1
- 圖表：共用座標範圍 helper 將資料、規格/管制線納入刻度，Node 測試涵蓋邊界。
- 班別：VariableMeasurement 新增 ShiftCode（EARLY/MIDDLE，預設 EARLY），EF migration 更新日報唯一鍵；UploadService 及日報/歷史 API 同步班別，Portal 下拉與 payload/query 同步。
- 刪除：SPC DELETE 單筆端點做認證、寫入權限、日期/來源/管制範圍檢查；交易內移除計算/量測、關閉警示及寫入 UploadDetail 稽核；Portal 品保代理及查詢按鈕串接。
- 核心變更先建立有意義的回歸測試，再實作。
- DB migration 必須驗證舊資料歸 EARLY 與新唯一鍵；不得回退 migration 造成早中班資料碰撞。
- 部署：先辨識測試 IIS 目錄與 DB，備份測試產物與必要 DB，產物於隔離 staging 建置；SPC API/schema → Portal API/Web → SPC Web；不執行同時發布正式站的現有腳本。
- 回復：保留新增欄位與班別資料，必要時還原測試二進位並限制中班使用；不直接 Down migration 丟失區分。
