# I18N-TASK-07A 設備頁面三語
- 版本1，2026-10-26，DONE，必要驗證與測試發布完成。[授權](../20261026-i18n-continuation/spec.md)／[工作池](../20261022-i18n-task01/tasks.md)。07依模組拆A設備、B粒子、C校驗、D設定與使用者、E其餘查詢／導覽，不擴張功能。
- EquipmentStatusView／EquipmentPointsView／EquipmentMonitorView固定UI、狀態顯示與相對時間三語；保留來源／設備／channel／名稱／單位／raw訊息、數值及日期格式。
- 不改30秒／10秒更新timer、freshness判定、排序、原query、用途RUNNING/ALARM、activeWhen、pin及mapping DTO。locale變更不重讀／重建timer、不寫入、不清篩選與草稿。
- 主系統SPC，Chameleon等僅既有資料來源，無相依系統／DB／後端變更；只SPC測試前端。rollback核對test後資產先index最後還原完整前端備份，保留設定。
## BDD／矩陣
- AC1 Happy：Given 已選來源／設備／篩選及編輯草稿，When 三語切換，Then 顯示更新，原值／DOM／query／timer保持，無額外API。
- AC2 Boundary：Given 0/null、online/stale/unavailable、running/stopped/unknown與相對時間邊界，When 顯示，Then 原判定／排序／數值不變，只翻譯文字。
- AC3 Invalid：Given API錯誤或來源原文含HTML/i18n，When 顯示／儲存失敗，Then 原文純文字與草稿保留。
- AC4 Regression：Given 點位mapping，When 儲存任一語言，Then 原端點與逐欄DTO／原enum不變；監看只GET，timer依原頻率。
- AC5 三語四尺寸及必要模板／字典／瀏覽器／build／發布驗證；設備點位表格手機可橫捲，不更動業務。
## 任務
- [x] 規格／工作池／目的。
- [x] 實作固定顯示與draft契約。
- [x] 必要驗證與測試站發布。
- [x] 文件／提交上傳，接續07B。
驗證詳見[紀錄](verification.md)；譯文draft待TASK-20真人審稿。
