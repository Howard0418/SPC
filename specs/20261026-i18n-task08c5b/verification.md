# 08C5B 驗證與發布
- 2026-10-26 DONE。共用索引轉接與訊息行組合先紅後綠，117前端單元通過。索引必須為範圍內整數且唯一，未知／非法描述回原字串，不就地修改原陣列。API回應message/title非字串時拒絕當本機UI描述，保留安全字串fallback。無新增譯鍵，2622鍵三語draft保持。
- 其餘23公開頁及6使用元件的已存API錯誤接getApiErrorDisplay/displayUiMessage；原生alert仍用字串getApiErrorMessage，系譜頁離線原UI fallback保留。校驗原優先message的錯誤保持，已知metadata優先。未接公開路由的舊頁不開新入口。
- 設備root/source/device、上傳preview／欄位tooltip／suggestion與history firstError、校驗errors/warnings索引／outcomes與製程relatedItems接明示metadata；未知code/key保留原文，原Status／顏色／計數／raw欄位／數值0／ErrorCode及陣列／FormData／選取未變。巢狀API描述先翻成純文字，再插入本機UI args。已保存資料與歷史diagnostic不推定翻譯。
- 本機31個逐頁與DTO情境分批通過，覆蓋切三語不增加請求／不卸載DOM、query／草稿／selected保持、HTML/i18n原名、非法message物件、索引與逐列結果、原生alert為字串；發布後完整31一次通過。所有業務API mock，含測試POST/DELETE均只回fixture，不真寫入／匯入／寄信。49既有API相容情境及shell17通過。
- 操作回歸193項：打包preview下190通過；另3圖表context測試依賴/src/Vue開發模組，誤用preview時無法解析undefined模組，改用原playwright.i18n-chart-actions.config.ts開發模式後3通過，未改原斷言／產品邏輯。涵蓋表單／失敗／DTO／公式排程／粒子／校驗／設備timer／警示／SMTP設定／查詢與圖表，無新業務退化。既有校驗兩項CAL-VERIFY-001/002保留，不宣稱後端全綠。
- build:test通過（既有chunk/table提示保持）；backend與API資源未修改，API0.1.81沿用，無schema／migration。唯讀172.16.110.16 / PMR_SPC_TEST pending=[]；核IIS SpcSingleTest/實體backend D:\SPC\release\test\backend及AppEnvironment=test。
- 前端完整備份 .codex-tmp/i18n-task08c5b/backup-frontend 106檔hash核對、發布4檔assets先index後、保護設定4檔不變，不重啟pool。實際API version/health200/0.1.81/test/ok，Web0.1.102四檔SHA256等於dist，同源14 API通過。正式／Portal未改。
- 回復核同test路徑，還原backup-frontend資產後index最後；backend／設定／資料不動，核原版本及health，無DB回復需求。
- 剩15件接D。尚無metadata的背景／郵件／報表固定顯示與舊服務提示，依D/E補齊；持久化業務訊息及原始diagnostic不自動翻譯。真人審稿留20，其他掃毒阻擋1／Runtime與TransFiles暫緩2／校驗基準差異2各自保留。
