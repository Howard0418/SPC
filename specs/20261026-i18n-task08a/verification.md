# 08A 驗證與發布
- 2026-10-26 DONE。API單一來源模組12鍵／36列draft，合併runtime2212鍵三語；後端embedded resource與前端組合共用原JSON，三語key／named args／重複與shape衝突嚴格驗證。
- 核心測試先紅後綠：缺少資源composer／後端localization類別；追加舊validation message保留測試先失敗，再改TryAdd通過。前端101單元、後端25單元與無DB Kestrel HTTP通過（含既有error factory／authorization regression）。12並行locale HTTP、1.25 JSON數值與ambient Culture不變，SQL內部例外不洩漏。
- 本機與發布後各12 API adapter瀏覽器情境通過：三語known／field／invalid／unknown descriptor，每請求明示locale、raw HTML／i18n符號純文字不重編譯。所有API攔截，無業務寫入。已儲存字串不宣稱隨後切語言即時變更，逐頁descriptor接線留08C。
- 共用shell17通過。首次兩項失敗原因是舊測試以繁中placeholder定位，在切換後找不到同一欄位；改用已存在testid，保留DOM實例／草稿／無額外請求／角色與路徑原斷言後全數通過。無產品改動。
- build:test與Release dotnet publish通過；既有chunk大小與equipment table HTML提示保留。新增欄位不替換原HTTP、code、message、errors／field keys與安全detail策略；無權限／公式／匯入格式改動。
- 發布前唯讀核對SQL 172.16.110.16 / PMR_SPC_TEST，pending migration=[]，未啟動product host進行查核；本次無migration/model修改。IIS SpcSingleTest/實體backend D:\SPC\release\test\backend，pool SpcSingleTest與AppEnvironment=test。
- 完整前後端備份於 .codex-tmp/i18n-task08a/backup，逐檔SHA256核對；只該測試pool stop/start，runtime94檔與前端4檔、保護設定4檔hash不變。正式／Portal未變更。真匿名version/health HTTP200 API0.1.73/test/ok，Web0.1.92四檔SHA256等於dist，14同源API請求通過。
- 回復：先確認同一test路徑與pool，停止該pool，使用backup/backend與backup/frontend成對回復，保留現有appsettings/web.config與資料、資產先index最後，重啟同pool並核version/health。無資料庫回復需求。
- 譯文仍draft，真人語意審查留20；其餘17件接續08B，另附件掃毒受阻1／Runtime與TransFiles暫緩2。
