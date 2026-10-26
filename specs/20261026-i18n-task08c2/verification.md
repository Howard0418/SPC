# 08C2 驗證與發布
- 2026-10-26 DONE。24鍵／72列draft，累計2275鍵三語；其餘主檔原錯誤物件附加metadata，原string response透過明示X-SPC-Message-Key無參數header，result實例／body／HTTP／text/plain content negotiation不變。client僅對string body解析已知argument-free key，typed object優先、非法header回raw，不依譯文比對業務。
- 核心先紅後綠：後端缺WithLegacyMessage、前端缺resolveLegacyApiDescriptor紅燈後實作。後端30必要測試通過，包含原字串result／body、未知／需參數key拒絕、無DB Kestrel真HTTP text/plain／header、實際製程缺名稱不寫入、重複group不另建與scope/GroupType原文、使用中製程刪除拒絕與關聯清單順序、PPC／維護與Auth guard回歸。
- 原relatedItems完整保留，僅加relatedItemMessages，機台／管制項目識別與計數明示具名args，count InvariantCulture；僅原投影多帶原名稱／代碼，原查詢filter／刪除與成功結果不變。巢狀清單逐頁畫面接線留C5，不宣稱已全部顯示。
- 105前端單元含24鍵ledger hash／protected terms／原文參數通過，build:test與Release publish通過，既有chunk/table提示保留。瀏覽器本機24／發布24三語known/invalid/unknown、安全、PPC原文、legacy header／badHeader回退通過，業務API全部攔截。
- 發布前唯讀SQL 172.16.110.16 / PMR_SPC_TEST，pending=[]，無model/migration；核IIS SpcSingleTest/、同名pool、backend D:\SPC\release\test\backend與AppEnvironment=test。
- .codex-tmp/i18n-task08c2/backup 完整前後端逐檔SHA256核對，runtime94與前端4檔、保護設定4檔不變，只重啟該test pool。實際version/health HTTP200 API0.1.76/test/ok，Web0.1.95四檔SHA256等於dist，14同源API請求通過。正式／Portal與資料庫結構未改。
- 回復：核同test路徑與pool，停該pool，以backup/backend和backup/frontend成對回復，保留設定／資料、資產先index最後，再啟pool核version/health。無DB回復需求。
- 剩18件接續C3，譯文draft仍留20真人審查；附件掃毒另受阻1／Runtime與TransFiles暫緩2。
