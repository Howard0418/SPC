# PORTAL-SPC-CHEM-LEGACY-EXCLUSION-CLEANUP-1119-TASK-001

## 目的與範圍

- 清理 Portal 舊版「不列入」checkbox 所建立的藥液量測排除資料。
- 僅限 `PortalChemicalTrace-v1` 批次、`VariableMeasurement`、固定原因代碼及 `ExcludedHidden` 的啟用紀錄。
- 以 `IsActive=0` 軟停用，不實體刪除；保留人工在 SPC 右鍵建立的排除及新版 `ExcludedVisible` 固定備註。
- 本次只處理測試資料；正式資料須另行授權。

## BDD 驗收

- Given 舊版 Portal 藥液量測存在固定原因且為隱藏排除，When 執行清理，Then 該排除改為停用且量測資料本身保留。
- Given 排除原因為「管制圖右鍵設定」或非 Portal 批次，When 執行清理，Then 不得異動。
- Given 已清理資料，When 需要回復，Then 可依保存的 ID 將 `IsActive` 改回 1。

## 驗證

- 執行前後依狀態、原因及來源批次統計。
- 驗證量測筆數不變、人工排除仍啟用。
- 無 schema、網站程式或版本變更，不觸發網站發布。
