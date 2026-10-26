# 07E1 驗證與發布
- 2026-10-26 DONE。94新鍵／282列draft、累計2073鍵三語；公開查詢／系譜／咬蝕入口固定UI、tooltip與提示三語，原識別與raw資料保持。
- AC1/AC5：三頁三語四尺寸36情境，本機／發布通過；保留DOM／篩選／查詢次數／日期與識別，系譜本機實際ECharts instance／series資料相同，tooltip固定label即時翻譯、raw HTML編碼；發布不存取dev模組，深度option斷言在本機完成。英文手機三頁截圖檢視；系譜drawer最大寬不超過容器、復用語言handler，關閉後可繼續查詢。
- AC2/AC3：無PPC固定錯誤切語言、API404 raw純文字、空清單保留查詢／翻譯訊息、QE2無啟用mapping卡片保留無連結；0/null與既有fallback不變（tree tooltip currentQty=0原以「-」顯示仍保持，事件量測0仍顯示0）。95單元通過，含模板／動態固定文字／draft hash／placeholder／raw與0參數。
- AC4：workOrderNo/lotNo/serialNo逐欄query、CPK匯出month與路徑、lot/workorder端點、drawer切語言與關閉保持；PT2 PPC=5與原特性code、start/end query及Portal href/rel不變，不操作Portal。39＋空資料1＝40個不同情境，本機／發布分批通過，無未處理JS錯誤。全部業務API攔截、無寫入。
- build:test通過、2073鍵×3，既有chunk提示保留；只SpcSingleTest Web0.1.90/API0.1.72，IIS實際backend D:\SPC\release\test\backend與AppEnvironment=test核對，完整前端備份78檔、payload4／保護設定4，不重啟pool。
- 真匿名version/health HTTP200，environment=test/API0.1.72/status=ok；發布四檔SHA256等於dist，14同源API請求通過。
- 回復：核對test目錄，使用 .codex-tmp/i18n-task07e1/backup-frontend 備份，資產先index最後、保留web.config/appsettings；無DB／後端／Portal／正式變更。
- 07E拆兩批後本項完成剩15件，接續07E2；draft真人審稿待20，另受阻1／暫緩2。未接路由舊檔保留，不宣稱已翻譯。
