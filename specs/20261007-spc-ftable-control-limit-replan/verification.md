# SPC F 表版本記錄與管制界線重算規劃驗證

功能 ID：SPC-FTABLE-CL-REPLAN-20261007  
日期：2026-10-07  
狀態：規劃完成

## 驗證
- Happy Path：已將標準差方法切換後重算管制界線、F 表版本記錄拆成兩個可執行小工作。
- Boundary Case：規格已列入手動固定界線、分段管制線、樣本數不足與 F 表無變更。
- Invalid Input：規格已列入無效公式、缺少 F 表儲存格、非法回復、未授權操作。
- Regression Risk：規格已要求保護既有藥液公式版本紀錄、F 表引用計算與 SPC 圖表界線來源。

## 結果
- AC-001：`TODO.md` 新增 `SPC-CL-RECALC-TASK-001` 並排在 F 表版本記錄之前。
- AC-002：`TODO.md` 新增 `SPC-FTABLE-VERSION-TASK-001`。
- AC-003：測試方向已寫入規格。
- AC-004：本次僅文件與工作池更新，未修改程式、資料庫、IIS，未發布。

