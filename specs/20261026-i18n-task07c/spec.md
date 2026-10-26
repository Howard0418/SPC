# I18N-TASK-07C 儀器校驗三語
- 版本1，2026-10-26，DONE，必要驗證與測試發布完成。[授權](../20261026-i18n-continuation/spec.md)／[工作池](../20261022-i18n-task01/tasks.md)。
- InstrumentCalibrationsView與InstrumentImportPanel固定介面、狀態顯示、驗證／成功提示與模態語言選單三語。raw API訊息／儀器名稱／部門／人員／證書檔名／Excel原欄位與來源／警示不翻譯。
- 保留usageStatus／Passed/Failed／Email/SynologyChat／校驗方式原value、日期字串與台北日期判定、到期計算、version／reason／requestId、DTO、憑證FormData與匯入hash／selectedRows／options、角色與匯入權限／限制、提醒0～365與12項卡控；locale不重讀或清草稿，不新增通知／寫入。
- 主系統SPC，無DB／後端／其他系統變更；只測試前端發布，正式另授權。所有業務驗證攔截API，不真寄信／匯入／寫入。
## BDD／測試矩陣
- AC1 Happy：Given 列表篩選／編輯／登記／歷史／設定或匯入草稿，When 切三語，Then 固定UI更新，原DOM／選擇／草稿／查詢保持。
- AC2 Boundary：Given 日期今日／過期／未來、0/null、Inactive/Retired、提醒0／365／12項、匯入Ready/Invalid/Existing，When 顯示或操作，Then 原判定／value／卡控保持。
- AC3 Invalid：Given raw含HTML/i18n、API409或無效輸入，When 操作，Then 原文純文字、保留草稿、固定驗證訊息切語言，不送無效資料。
- AC4 Regression：Given 任一語言編輯／登記／匯入／下載，When 提交，Then 原端點／DTO／日期／version／FormData欄位與原檔內容不變；取消不寫入、既有日期／捲動／匯入回歸通過。
- AC5 三語四尺寸／手機模態與原橫捲操作可達，模板／draft hash／參數／必要瀏覽器／build與測試發布後提交上傳。
## 任務
- [x] 規格與目的。
- [x] 固定顯示／draft契約與原handler語言選單。
- [x] 必要驗證／測試發布。
- [x] 文件／提交上傳接續07D。
驗證詳見[紀錄](verification.md)。Rollback核對test目錄後還原完整前端備份，資產先index最後、保留設定；draft真人審稿留TASK-20。
