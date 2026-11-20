# SPC-CHEM-MALFORMED-AUDIT-JUDGMENT-1120-TASK-001

狀態：完成；測試站已發布，待使用者依驗證指引抽驗。

規格版本：3

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

初始登錄階段只建立問題與驗收契約；本版已獲使用者執行授權，依下列修正契約實作、測試並發布測試站。

## 修正契約

- 保存規格讀取須同時回傳「有效快照」與「出現無效保存證據的批次」，不得把無效證據靜默等同於完全沒有證據。
- 同一批次只要存在宣稱為`PortalChemicalSpecificationSavedV1`但不是物件、`details`不是非空陣列、內容無法反序列化或批次不一致，即視為證據衝突；歷史判定回`Unknown`、`SAVED_SPEC_EVIDENCE_INVALID`及`EvidenceConflict`，不得再套最近／目前規格。
- 完全沒有保存規格稽核的舊資料，仍依既有最近規格／目前規格fallback；合法唯一快照與重複快照既有行為保持。

## 版本紀錄

- v1：登錄問題與BDD。
- v2：開始實作，明確區分無效證據與無證據，固定原因碼及fallback邊界。
- v3：實作與聚焦測試完成；API `0.1.162`發布測試站，連線、版本、環境及資產驗證通過。[驗證](verification.md)
