# 技術計畫
- 功能 ID：SPC-CHEM-EFFECTIVE-VALUE-1113-TASK-001
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- Portal：`VariableMeasurementEntry.cshtml` 顯示初驗、複驗及只讀最終有效值；`tests/chemical-effective-ui.cjs` 驗證瀏覽器行為。
- SPC：既有 `SpcService`、`ChemicalHistoryQueryService` 維持同一保存值契約；擴充 API／圖表回歸測試。
- 資料庫：不變更。發布：Portal 測試 IIS；SPC 程式無變更時不重發佈，但仍驗證圖表來源的既有契約。

## 實作方式與需求對應

1. 將入口欄位改稱「初驗分析值」，在同一列顯示「最終（查詢／圖表）」及初驗／複驗來源。
2. 在既有 `updateBatchRowState` 以欄位保存的文字與精度更新最終值，不額外呼叫公式預覽。
3. 保留 API `effectiveValue`、SPC `actualValue` 與歷史保存證據規則，不回算歷史。

## API、檔案格式及資料模型

不新增 API 或資料欄位。既有讀取契約：`effectiveValue = recheckValue ?? measuredValue`。

## 相容性與風險

- 只增加唯讀畫面說明，不改變保存 payload。
- 必須以空字串判斷是否有複驗，避免將 `0` 誤判為無複驗。
- 回復方式：回復 Portal 單一 view 變更；資料與 SPC API 不受影響。

## 驗證安排

- Portal Node UI 測試覆蓋 AC-001～AC-003。
- SPC 單元測試覆蓋複驗優先與 0 邊界。
- 編譯 Portal API/Web、SPC API；發布 Portal 測試站後依規則驗證首頁資產、Portal API、SPC API health/version 與實際 API 基底。

## 發布、備份及回復

只發布 Portal 測試 IIS，不發布正式站；發布腳本自動產生既有備份／版本。若健康檢查失敗，保留失敗證據並依既有發布回復流程處理。
