# I18N-TASK-06D 警示與處置顯示三語
- 版本1，2026-10-26，DONE，必要驗證與測試發布完成。依[連續授權](../20261026-i18n-continuation/spec.md)及[工作池](../20261022-i18n-task01/tasks.md)。
- SPC AlertsView與AlertsWorkflowView（含SpcAlertsView包裝入口）固定文字、placeholder、操作提示及成功訊息三語；原API錯誤與主檔／訊息／根因／對策／姓名原文保持。
- 不改角色、guard、簽收及Closed觸發ACK順序、原狀態字串、篩選／排序、查詢端點、POST／PUT payload、預填測試郵件與模擬訊息。切語言不送信、不新增寫入、不重載頁面，不丟草稿。
- 主系統SPC，無相依系統／DB／後端變更；僅測試前端發布，正式另授權。rollback為核對test後還原完整前端備份、資產先index最後、保留設定。
## BDD／測試矩陣
- AC1 Happy：Given 任一入口已載入與篩選，When 切三語，Then 固定UI更新，原值／計數／DOM／查詢保持。
- AC2 Boundary：Given 空列表／數值0／已簽收與Closed，When 顯示，Then 原狀態判定／計數與零值保持。
- AC3 Invalid：Given raw含HTML或API失敗，When 顯示或儲存，Then 原文純文字、保留草稿，固定訊息可切語言。
- AC4 Regression：Given 模擬／測試郵件／簽收或處置草稿，When 三語操作，Then 原端點、參數、DTO與Closed後ACK保持；取消不寫入。測試均攔截API，禁止真寄信。
- AC5 三語四尺寸，手機模態操作可達；編譯／字典與draft契約／必要瀏覽器測試／發布驗證。
## 計畫與任務
- [x] 規格與目的；保持業務基準。
- [x] 固定UI與成功訊息、原handler語言選單、draft ledger。
- [x] 必要測試／build與測試發布。
- [x] 文件同步、提交上傳並接續07。
驗證詳見[紀錄](verification.md)，譯文初稿真人審查留TASK-20。
