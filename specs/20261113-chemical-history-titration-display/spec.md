# 藥液查詢顯示保存滴定值

- 功能 ID：SPC-CHEM-HISTORY-TITRATION-1113-TASK-001
- 狀態：測試站完成／正式未發布

## 需求

藥液量測查詢頁的初驗／複驗滴定值，必須與新增資料頁載入同一筆日報時一致。資料來源依序為量測所連結 Receipt 的 `originalInput.Primary`，舊資料缺 Receipt 時才回退同批次、同 PPC 或同槽位＋品質特性的 `UploadDetails.TitrationValue`；不可從分析值反推或跨線別取值。

## 驗收

1. Receipt 的 `originalInput.Primary` 為大寫或小寫、字串或數值時均可正確回傳。
2. Portal 查詢表格顯示 API 回傳的初驗滴定值；無證據才顯示 `-`。
3. 11/09 16:00 N1 七筆 Receipt 初驗值可查得 `9、12.7、6.7、0.205、5.5、14.6、0.1`；P200 對應 `12.7 → 151.638`。
4. 不修改歷史量測、分析值、公式、F 表或 schema。
