# 07E2 驗證與發布
- 2026-10-26 DONE。127新鍵／381列draft、累計2200鍵三語；操作手冊及SSO固定標題／提示／錯誤／返回連結三語，SSO復用原LocalePicker handler，保留原術語／路徑／流程。
- AC1/AC5：手冊三語四尺寸12、缺token／無效格式三語四尺寸24、假claims與安全redirect5，共41不同情境本機／發布通過；切語言保留DOM／路徑、不增加API。97單元含模板／ledger hash／placeholder／protected tokens通過，build:test通過。全部譯文draft待真人語意審核，未冒稱人工完成。
- AC2/AC3：缺token與無效token失敗分支、hash清除、無session token保持；假JWT UTF8姓名／displayName／operatorCode／角色／permission claims映射保持；預設／相對路徑與兩種外部redirect安全fallback保持，sessionStorage redirect消費後清除。未連線真Portal／AD，所有API攔截。
- AC4：原token解析、setAuthSession、SSO grace、claims與redirect程式比對未改，只有兩個error顯示改key；LoginView空模板與launch轉址未修改。未接路由舊頁未新增入口，手冊未加入偏態峰態或方法下拉。
- 手機視覺QA發現原固定側欄壓縮SSO卡片，修正該路由手機堆疊／卡片寬度；六個手機情境及全部29個SSO情境本機重驗，加入view寬度>70% viewport斷言後最終發布41全通過。手機手冊與SSO截圖檢視。無未處理JS錯誤。
- 只SpcSingleTest Web0.1.91/API0.1.72；IIS實際backend D:\SPC\release\test\backend與AppEnvironment=test核對。初始完整前端備份81檔保留，手機修正版另備份83檔，兩次payload4／保護設定4，不重啟pool。
- 最終真匿名version/health HTTP200、environment=test/API0.1.72/status=ok，四檔發布SHA256等於dist，14同源API請求通過，既有chunk提示保留。
- 回復：核對test目錄，使用 .codex-tmp/i18n-task07e2/backup-frontend 回本項前版本；backup-before-mobile為首版91補充備份。資產先index最後、保留web.config/appsettings。無DB／後端／Portal／正式變更。
- 剩14件（08～21），接續08；draft真人審稿留20，另受阻1／暫緩2。
