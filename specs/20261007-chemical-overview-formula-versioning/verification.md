# 線別分析項目總覽公式版本記錄補強驗證

功能 ID：SPC-CHEM-OVERVIEW-FORMULA-VERSION-20261007  
日期：2026-10-07  
狀態：規劃完成，待執行

## 驗證

- Happy Path：已規劃總覽頁修改單筆公式後需建立同一份藥液公式版本記錄。
- Boundary Case：已規劃無變更不建版、批次多筆各自建版、部分失敗只成功者建版。
- Invalid Input：已規劃無效公式 JSON、非 CHEM 項目、未授權儲存/回復。
- Regression Risk：已規劃保護管制項目設定頁既有版本記錄/回復，以及 F 表版本記錄。

## 結果

- AC-001：`TODO.md` 新增 `SPC-CHEM-OVERVIEW-VERSION-TASK-001`。
- AC-002：小工作排序在 IIS 測試站環境問題之後、Portal 生日調整之前。
- AC-003：本次僅規劃與工作池更新，未修改 SPC 程式、資料庫或 IIS。
- AC-004：本次不建置、不測試、不發布。
