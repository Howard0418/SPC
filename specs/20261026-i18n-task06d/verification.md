# 06D 驗證與發布
- 2026-10-26 DONE。117新鍵／351列draft；累計1506鍵三語，警示總覽／處置固定UI、成功訊息具名參數、模態LocalePicker及手機堆疊導覽。
- AC1/AC5：三入口×三語×四尺寸36情境，本機／發布均通過，切換保留篩選、DOM、查詢次數與raw文字；英文手機截圖檢視，表格原水平捲動保留。
- AC2/AC3：零actualValue、空列表、API409原文與草稿、含HTML/i18n字串純文字、狀態及計數保持；75單元（模板編譯／ledger hash／placeholder／literal參數含0）與build:test通過。既有chunk大小提示保留。
- AC4：三語處置失敗／重送DTO一致，Closed後ACK順序；三語模擬原預填與actualValue=0／OOS；三語郵件query literal及簽收；三語手機草稿／切換／取消不寫入。上述12情境加顯示36，共48個不同情境本機分批通過，發布48全通過。初版測試定位sr-only radio與巢狀locator失敗，改點可見label後三語重驗通過，非程式業務缺陷。
- 所有業務API攔截，未真寄信、未改資料；無未處理JS錯誤。原enum、DTO、姓名／根因／對策／message與預填email保持，成功後原load清除訊息的行為保留。
- 只發布SpcSingleTest Web0.1.85/API沿用0.1.72；IIS實際backend D:\SPC\release\test\backend與AppEnvironment=test核對，完整備份67檔、payload4、保護設定4，不重啟pool。
- 真匿名version/health HTTP200，environment=test/API0.1.72/status=ok；4個發布檔SHA256等於dist；14同源API請求通過。
- 回復：核對同一test目錄，將 .codex-tmp/i18n-task06d/backup-frontend 資產先還原、index最後，保留web.config/appsettings。不改DB／後端／正式。
- 譯文仍draft待TASK-20真人審稿。剩15件接續07；外部受阻1、暫緩2另列。
