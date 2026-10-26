# 08C1 驗證與發布
- 2026-10-26 DONE，26鍵／78列draft、累計2251鍵三語。PPC私有驗證改typed descriptor，全部原分支／欄位正規化／null成功／HTTP與message保持；groupName為具名原文，只單次純文字插值，複製唯讀args防止外部修改。
- 測試先行：ApiUiError三參數overload缺少紅燈後實作；PPC／maintenance／API資源26測試通過，包含LSL由0.19999999999999998改0.2編輯排除自己成功，另一筆真正相同key時409且原資料不變、無效新增不寫入、群組原文與缺參數拒絕、既有公式歷史／界限／WE規則回歸。無真實DB業務寫入。
- 103前端單元通過，26鍵ledger hash／protected tokens／參數單次插值；build:test與Release publish通過，既有chunk/table提示保留。原數值／日期／公式／匯入與成功DTO未改。
- 本機18／發布18 API adapter情境三語通過，含群組名稱HTML／i18n符號保持純文字；所有業務API攔截。逐頁已存錯誤即時切語言留C5，不冒稱本批涵蓋。
- 唯讀核SQL 172.16.110.16 / PMR_SPC_TEST，pending=[]，無model/migration；核IIS SpcSingleTest/、同名pool、backend D:\SPC\release\test\backend與AppEnvironment=test。
- .codex-tmp/i18n-task08c1/backup 完整前後端逐檔SHA256備份；runtime94檔、前端4檔，保護設定4檔不變，只重啟測試pool。真匿名version/health HTTP200 API0.1.75/test/ok，Web0.1.94四檔SHA256等於dist，14同源API請求通過；正式與Portal未改。
- 回復先核同test路徑/pool，停該pool，backup/backend及backup/frontend成對回復，保留設定／資料、資產先index最後，啟pool後核version/health；無DB回復需求。
- C依規模拆5批後本批完成剩19件，接續C2；draft真人審查留20，另附件掃毒受阻1／Runtime與TransFiles暫緩2。
