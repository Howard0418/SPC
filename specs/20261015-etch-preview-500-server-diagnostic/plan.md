# 技術計畫
- 功能 ID：SPC-ETCH-PREVIEW-500-SERVER-DIAGNOSTIC-20261015
- 規格版本：3
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- SPC：`EtchTestImportController` 攔截資料庫與未預期例外，記錄不含量測值的日期、線別、動作與 Trace ID。
- SPC 測試：新增錯誤回應組裝器的單元測試。
- TransFiles：不修改；既有日誌會顯示 SPC 回傳文字。

## 實作方式與需求對應
- 抽出無狀態錯誤回應組裝器，資料庫例外回 503／`ETCH_PREVIEW_DATABASE_FAILURE`，其他例外回 500／`ETCH_PREVIEW_SERVER_FAILURE`。
- Controller 先保留 400、409 分支，再依例外類別回安全訊息並以 `LogError` 留下完整例外與 Trace ID。

## API、檔案格式及資料模型
- API 回應新增 `errorCode`、`traceId`；不變更請求格式或資料表。

## 相容性與風險
- TransFiles 僅將非 2xx 視為預覽阻擋，新增訊息可直接顯示，具相容性。
- 預覽不開啟確認匯入專用交易或應用程式鎖，也不建立 UploadBatch 或量測資料；確認匯入仍保留寫入保護。

## 驗證安排
- AC-001、AC-002：組裝器單元測試。
- AC-003：既有 `EtchTests` 預覽測試。
- 通過後 build、publish，刷新 SPC 測試站 backend；再以一筆代表日報預覽進行人工驗證。

## 發布、備份及回復
- 僅發布 SPC 測試站 backend，先由既有 release 工具備份；正式站不發布。
- 回復：還原此次 backend 備份並回收 `SpcSingleTest` AppPool。
