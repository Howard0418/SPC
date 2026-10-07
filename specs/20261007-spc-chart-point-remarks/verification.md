# SPC 管制圖量測點備註驗證

功能 ID：SPC-CHART-POINT-REMARKS-20261007  
日期：2026-10-07  
狀態：規劃完成，待執行

## 驗證

- Happy Path：已規劃管制圖點位右鍵新增備註，重新查詢仍顯示。
- Boundary Case：已規劃備註清空、長文字、換行、多種點位 scope。
- Invalid Input：已規劃找不到點位、非本 PPC 點位、未授權、超過長度。
- Regression Risk：已規劃保護點位排除/隱藏功能與 SPC 計算結果。

## 結果

- AC-001：`TODO.md` 新增 `SPC-CHART-POINT-REMARK-TASK-001`。
- AC-002：小工作排序在公式版本參照文件補強之後、Portal 生日調整之前。
- AC-003：本次僅規劃與工作池更新，未修改 SPC 程式、資料庫或 IIS。
- AC-004：本次不建置、不測試、不發布。
