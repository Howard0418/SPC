# 07D 驗證與發布
- 2026-10-26 DONE。173新鍵／519列draft、累計1979鍵三語。SMTP／報表／使用者固定UI、星期／角色與比對狀態顯示、原生確認／錯誤／成功提示三語；保留API原文與raw資料。
- AC1/AC5：三頁三語四尺寸36、手機人員草稿／取消3，本機與發布通過，語言切換保留DOM／設定／收件ID／草稿、不增加API；英文手機三頁截圖檢視。
- AC2/AC3：91單元通過，含模板／動態固定文字／ledger hash／具名參數／literal email與raw／0／SMTP port及SSL邊界。110s與587ssl、0／空／非數字退回25保持；空白密碼及hasPassword提示保持；409人員草稿保留。星期日0／月日28／sendTime原數值不變，星期文字即時更新。
- AC4：三語SMTP儲存／測試信／測試警報逐欄port、ssl、空密碼、recipient IDs與settings保持；三語報表排程、原始下載bytes／檔名／原月份期間、weekly/monthly兩種send-now DTO保持；三語人員Editor／pagePermissions及失敗重送保持；三語Portal比對勾選與同步只送portalUserId，不改權限。加12不同情境，共本機分批51／發布51通過；所有通知／業務寫入全攔截。
- 初版測試誤指定非既有submit testid及假定時鐘毫秒不流逝，修正測試定位／原期間斷言後相關案例重驗通過，未改業務邏輯；發布51完整通過。無未處理JS錯誤。
- build:test通過，1979鍵×3，既有chunk大小提示保留。只SpcSingleTest Web0.1.89/API0.1.72；IIS實際backend D:SPCelease	estackend及AppEnvironment=test核對，完整前端備份76檔、payload4／保護設定4、不重啟pool。
- 真匿名version/health HTTP200，environment=test/API0.1.72/status=ok；發布四檔SHA256等於dist，14同源API請求通過。
- 回復：核對test目錄，使用 .codex-tmp/i18n-task07d/backup-frontend 備份、資產先index最後，保留web.config/appsettings；無DB／後端／Portal／正式變更，無機敏值入字典或文件。
- draft真人審查留TASK-20。剩15件接續07E，另受阻1／暫緩2。
