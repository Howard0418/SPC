# 技術計畫
- 功能 ID：20260914-calibration-date-edit
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
SPC InstrumentCalibrationService、InstrumentCalibrationsView；新增 CalibrationDateEditTests、CalibrationDateEditHttpTests、前端日期編輯測試。Portal/TransFiles 不變。

## 實作方式與需求對應
R-001～005：先測試重現舊 guard，再只移除 LastCalibrationDate 變更禁止規則，繼續使用共用 ValidateInputAsync、版本與交易、InstrumentUpdated audit。只改上次不改 CycleId/LatestResult/NextCalibrationDate，不取消相同到期日工作；改下次沿用現有取消邏輯。
R-006：日期欄位增加可辨識 label 與主檔更正提示，不增加新操作流程。

## API、檔案格式及資料模型
維持 PUT /api/v1/instruments/{id} 的 InstrumentInput 契約。LastCalibrationDate / NextCalibrationDate 為 DateOnly?；無 schema/migration，歷史及證書資料結構不變。

## 相容性與風險
主檔日期可與既有校正歷史不同；以異動稽核追溯更正，歷史仍代表當時登記內容。既有無下次日期不通知與改期原因规则維持。

## 驗證安排
先建立服務測試：首次補填、改既有日期/清空、已有歷史、稽核、日期合法性、版本、其他欄位及 Pending/Failed/Sent。
HTTP 使用既有隔離 WebApplicationFactory/JWT/SQLite 與假郵件；前端使用只攔截本機 API 的 Playwright。跑全校正測試及相關 UI 回歸，Release build/publish、testhost build；必要項目未跑不得標通過。

## 發布、備份及回復
授權僅 SPC 測試站 D:/SPC/release/test/backend、frontend；核對 IIS SpcApi/SpcWeb 實際目錄及绑定、PMR_SPC_TEST 与 migration 無待套用。先獨立 staging，再備份舊程式，保留 appsettings*.json、web.config 與既有資料；app_offline 停 API 後覆蓋程式、前端 assets 在 index 前，部署後比對 hash/HTTP。失敗從備份回復被覆蓋檔案。正式站/Portal/真信不操作。
