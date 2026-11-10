# 功能規格：既有藥液日報載入保存判定
- 功能 ID：`SPC-CHEM-DAILY-JUDGMENT-1110-TASK-001`
- 主系統：SPC API；相依：Portal藥液輸入頁
- 狀態：測試站完成／待登入畫面驗收

## 需求
- 日報API除分析值外，須回傳該量測已保存的初驗／最終判定、判定來源與缺證原因，不得套用目前主檔臆造歷史判定。
- Portal載入既有日報時，保存判定為OK／NG則直接還原；Unknown顯示「無法判定」及原因；只有可重新預覽且原始滴定值完整時才保留重新預覽入口。
- 不修改既有量測、公式、F表、規格或資料庫schema。

## BDD
- Given 既有量測有保存規格或Trace判定，When 載入當日日報，Then 分析值與保存的OK／NG同時顯示，不顯示待預覽。
- Given 既有量測缺少有效歷史證據，When 載入，Then 顯示無法判定與固定原因，不以目前上下限推算。
- Given 新輸入或使用者變更滴定值，When 預覽，Then 仍依日期公式／F的後端預覽結果更新判定。

## 風險與回復
- Happy Path：保存OK／NG還原；Boundary：複驗優先；Invalid：Unknown保持保守；Regression：新輸入預覽不變。
- 回復：回復SPC daily DTO及Portal日報載入欄位；無資料回復需求。
