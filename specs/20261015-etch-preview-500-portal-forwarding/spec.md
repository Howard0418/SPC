# 小型變更：Portal 保留咬蝕預覽診斷回應
- 功能 ID：SPC-ETCH-PREVIEW-500-PORTAL-FORWARDING-20261015
- 版本：2
- 狀態：DONE（2026-10-19 自動化錯誤路徑驗收完成）
- 主系統：SPC；相依系統：Portal、TransFiles；資料庫影響：無；發布影響：Portal 測試站 API；Rollback：還原本次 Portal API 備份。

## 問題與範圍
TransFiles 已成功登入 Portal，但透過 Portal 呼叫 SPC 預覽時，124 份日報只顯示 `HTTP 500: SPC預覽拒絕`。SPC 已提供安全錯誤代碼與 Trace ID，但 Portal 代理端未將其明確保留給 TransFiles，無法診斷根因。

本次只調整 Portal 預覽代理的錯誤轉送，保留 SPC 的 HTTP status、`errorCode`、`traceId` 及安全訊息。不修改 Excel、主檔、計算、資料庫、確認匯入或正式站。

## 驗收
- AC-001：SPC 預覽回 500/503 並含安全診斷欄位時，Portal 回應保留相同 status、錯誤代碼與 Trace ID。
- AC-002：TransFiles 可在預覽阻擋日誌中看到安全錯誤代碼與 Trace ID。
- AC-003：400/409 的既有可讀拒絕行為不變，預覽不寫入資料。

## BDD
```gherkin
Scenario: SPC 預覽發生資料庫例外
  Given Portal 已登入且轉送 SPC 預覽請求
  When SPC 回傳 503 與 ETCH_PREVIEW_DATABASE_FAILURE、Trace ID
  Then Portal 保留相同狀態與安全診斷欄位
  And TransFiles 日誌可顯示該診斷資訊
  And 不會確認或寫入匯入資料
```

## 2026-10-19 T-003 驗證計畫補充
- 驗收方式：以本機注入 HTTPError 500/503 的安全 JSON 回應，使用實際 TransFiles EtchImportClient.request/preview、GUI import_complete_reports 的失敗分支及 write_preview_failure_log，檢查錯誤代碼與 Trace ID 出現在顯示訊息及 LOG。
- 不刻意破壞測試站或製造資料庫故障；不執行登入、不讀帳密、不送 HTTP、不呼叫 confirm。GUI 的 root.after/messagebox 用替身執行與捕捉訊息，不建立視窗。
- Happy Path：500/503 安全診斷可見；Boundary：第一筆失敗後不送第二筆且不可確認；Invalid Input：非 JSON 回應保留安全 fallback；Regression：400/409 可讀拒絕與機敏片段遮罩維持。
- 這是原始碼自動化驗收，非打包 EXE 人工故障測試；後者列為驗證限制，不以成功匯入畫面充當錯誤路徑證據。
