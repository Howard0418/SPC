# SPC-ADJUST-1028-TASK-001 管制線公式儲存後重算重繪

## 需求

管制項目的管制圖公式設定儲存後，既有計算結果不可沿用舊公式；下次查詢／重繪必須讀取最新設定並重新計算 UCL、CL、LCL。既有量測資料不改寫，正式站不在本次範圍。

## BDD 驗收

- Given 管制項目已有公式與計算結果，When 只修改公式參數並儲存，Then 舊的 SPC 計算結果被清除，重新查詢會依新公式產生界限與圖線。
- Given 公式 JSON 僅格式化差異，When 儲存，Then 不應誤判為公式變更。
- Given 無效 JSON，When 儲存，Then 沿用既有驗證錯誤，不清除資料。

## 範圍與回復

只調整 PartProcessCharacteristics 更新時的 stale calculation invalidation 與單元測試；不修改 F 值、公式資料、量測資料或資料庫結構。回復方式為還原本次提交。
