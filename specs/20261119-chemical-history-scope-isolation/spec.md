# 藥液歷史與咬蝕量資料歸屬隔離

- 工作代號：`PORTAL-SPC-CHEM-HISTORY-SCOPE-1119-TASK-001`
- 主系統：Portal；相依系統：SPC API
- 資料庫影響：無 schema、無資料刪除或改寫
- 發布影響：SPC API、Portal API；先發布測試區，正式區另行授權
- Rollback：回復兩站 API 前一版本；既有量測資料不需回復

## Requirement

藥液量測查詢只能顯示 `CHEM` 管制範圍的量測。相同製程下屬於 `PROCESS` 的咬蝕量必須保留於原資料與咬蝕功能，不得混入藥液頁面。

正式資料採邏輯歸類，不搬移或複製量測：以 PPC `ControlScope` 作為唯一歸屬，`CHEM` 只供藥液功能，`PROCESS` 且來源為 `EtchReport` 只供咬蝕功能。Portal 與 SPC API 必須同時套用相同範圍，避免任一層漏濾。

## 正式區全量盤點（2026-11-19）

- 正式有效量測共 18,460 筆：`CHEM` 4,797 筆、`PROCESS` 13,663 筆。
- 13,663 筆 `PROCESS` 全部來自 175 個 `EtchReport` 批次，涵蓋 PT1／PT2／QE1／QE2、54 個日期；非咬蝕來源 0 筆。
- 找不到 PPC 的孤兒量測 0 筆；資料分類本身正確，不需刪除、搬移或改值。
- 正式 Portal 舊代理未傳 `controlScope=CHEM`，正式 SPC 舊量測端點亦未套用範圍，因此藥液頁誤顯示 18,460 筆合併結果。

## BDD

- Given 同一製程同時有 CHEM 與 PROCESS 的變數量測，When Portal 查詢藥液歷史，Then API 只回傳 CHEM PPC 的量測。
- Given 既有咬蝕量歷史資料，When 套用本次修正，Then 不刪除、不改值、不改來源，且未指定範圍的既有 SPC 呼叫仍維持原行為。
- Given 正式資料已依 PPC 分為 CHEM 與 PROCESS，When Portal 藥液頁跨日期與製程查詢，Then 只回傳 4,797 筆 CHEM 集合，13,663 筆 EtchReport 仍由咬蝕功能讀取。
- Given 無效日期或分頁參數，When 查詢，Then 維持既有錯誤回應。

## 測試規劃

- Happy Path：同製程混合 CHEM／PROCESS，只回 CHEM。
- Boundary：未傳 `controlScope` 時維持全量查詢。
- Invalid Input：既有日期與分頁驗證不退步。
- Regression Risk：咬蝕趨勢、藥液歷史分頁、初複驗呈現。

## 發布狀態

- 測試區既有修正與自動化已完成。
- 正式 SPC API 與 Portal API 尚未發布本隔離契約；須另行取得正式發布授權，且 API／Web 版本依固定規則同步遞增與驗證。
