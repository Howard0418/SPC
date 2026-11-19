# 藥液歷史與咬蝕量資料歸屬隔離

- 工作代號：`PORTAL-SPC-CHEM-HISTORY-SCOPE-1119-TASK-001`
- 主系統：Portal；相依系統：SPC API
- 資料庫影響：無 schema、無資料刪除或改寫
- 發布影響：SPC API、Portal API；先發布測試區，正式區另行授權
- Rollback：回復兩站 API 前一版本；既有量測資料不需回復

## Requirement

藥液量測查詢只能顯示 `CHEM` 管制範圍的量測。相同製程下屬於 `PROCESS` 的咬蝕量必須保留於原資料與咬蝕功能，不得混入藥液頁面。

## BDD

- Given 同一製程同時有 CHEM 與 PROCESS 的變數量測，When Portal 查詢藥液歷史，Then API 只回傳 CHEM PPC 的量測。
- Given 既有咬蝕量歷史資料，When 套用本次修正，Then 不刪除、不改值、不改來源，且未指定範圍的既有 SPC 呼叫仍維持原行為。
- Given 無效日期或分頁參數，When 查詢，Then 維持既有錯誤回應。

## 測試規劃

- Happy Path：同製程混合 CHEM／PROCESS，只回 CHEM。
- Boundary：未傳 `controlScope` 時維持全量查詢。
- Invalid Input：既有日期與分頁驗證不退步。
- Regression Risk：咬蝕趨勢、藥液歷史分頁、初複驗呈現。
