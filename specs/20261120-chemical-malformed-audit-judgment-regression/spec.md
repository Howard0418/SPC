# SPC-CHEM-MALFORMED-AUDIT-JUDGMENT-1120-TASK-001

狀態：僅登錄，未開始實作。

## 問題

稽核整案回歸續驗時，`ChemicalSavedSpecificationTests.MalformedOrNonObjectAuditCannotCrashHistoryOrBecomeEvidence`的3組輸入可獨立重現：`UploadDetail.PayloadJson`為非物件JSON、`details`型別錯誤或JSON破損時，預期歷史判定為`Unknown`，實得`OK`。

## 範圍與風險

- 主系統：SPC API。
- 相依：`ChemicalHistoryQueryService`、保存規格證據、目前／最近規格fallback與歷史查詢DTO。
- 資料庫影響：預計無schema migration；先用記憶體fixture及唯讀查詢診斷，不補寫歷史資料。
- 發布影響：若需改API，必須先紅綠測試、遞增API版本並只發布SPC測試站；正式仍需另行授權。
- rollback：回復本件服務最小差異及版本，不需回復資料。

## BDD

- Given 歷史量測只有非物件、型別錯誤或破損的保存規格稽核payload，When 查詢歷史判定，Then 不得將該payload當成可信證據，且不得因解析異常中斷查詢。
- Given 沒有可驗證的當時保存規格，When 系統考慮目前或最近規格fallback，Then 必須依既有業務契約明確區分`Unknown`與可判定情境，不得靜默使用不相干的現行規格改寫歷史語意。
- Given 合法且唯一的當時規格證據，When 查詢歷史判定，Then 既有OK／NG、複驗與衝突證據行為保持不變。

## 後續驗證規劃

- Happy Path：合法唯一保存規格可正確判定OK／NG。
- Boundary Case：JSON陣列、`details`非陣列／非物件、空值與重複證據。
- Invalid Input：截斷JSON、偽造`action`、數值與字串型別錯誤。
- Regression Risk：目前／最近規格fallback、歷史修復服務、Portal保存、初驗／複驗與既有`Unknown`原因碼。

本次只登錄問題與驗收契約，不修程式、不寫入資料庫、不發布。
