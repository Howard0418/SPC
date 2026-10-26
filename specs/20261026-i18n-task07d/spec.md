# I18N-TASK-07D 設定與使用者三語
- 版本1，2026-10-26，DONE，必要驗證與測試發布完成。[授權](../20261026-i18n-continuation/spec.md)／[工作池](../20261022-i18n-task01/tasks.md)。
- SmtpSettingsView、SpcReportSettingsView、OperatorsView固定介面／確認／驗證／成功訊息、星期與角色顯示三語。API錯誤、原部門／姓名／郵件／權限catalog原文、匯出與機器資料不翻譯。
- 不改角色／permission／路由guard／Portal比對與匯入規則、raw值、API端點／DTO／數值、smtp port清理與ssl判定、password清空／保留機制、勾選收件人ID、報表週月排程／時間／期間／檔案／通知；locale不重讀、不清草稿、不寄信、不寫入。
- SPC單一系統，Portal僅既有directory來源，不改Portal／登入／schema；只測試前端，全部通知／寫入API攔截驗證，正式另授權。
## BDD／測試矩陣
- AC1 Happy：Given 已載入設定或使用者表單與收件人草稿，When 三語切換，Then 固定UI更新，DOM／原選擇／草稿／query保持、無額外API。
- AC2 Boundary：Given 0/null／空列表、星期0／月日／port含ssl與角色enum，When 顯示與操作，Then 原值／清理／權限與卡控保持。
- AC3 Invalid：Given API409或raw含HTML/i18n／無效欄位，When 操作，Then 原文純文字、固定錯誤隨locale更新、草稿保留。
- AC4 Regression：Given 三語儲存／測試通知／匯出或使用者操作，When 提交，Then 原端點／DTO／收件ID／enum／schedule與下載原檔保持；取消不寫入，機敏值不入字典／文件或輸出。
- AC5 三頁三語四尺寸／手機模態，必要模板／draft hash／參數／瀏覽器／build與測試發布驗證。
## 任務
- [x] 規格與目的。
- [x] 固定顯示／draft契約。
- [x] 必要驗證／測試發布。
- [x] 文件／提交上傳接續07E。
驗證詳見[紀錄](verification.md)。Rollback核對test目錄後還原完整前端備份，資產先index最後、保留設定。譯文draft真人審稿留TASK-20。
