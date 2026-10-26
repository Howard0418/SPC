# 08C3A 驗證與發布
- 2026-10-26 DONE。60新鍵／180列draft，累計2335鍵三語；11控制器固定message/body附加metadata，原英文或繁中body/message、HTTP／success/data、寫入／計算與enum原樣保留；成功提示只加descriptor。服務例外與private動態驗證留C3B，逐頁接線留C5。
- 核心先紅後綠：四個分段無效新增測試缺header先失敗，再實作後通過；34後端必要測試含日期／分段重疊不寫入、點位備註／排除、分段詳情、公式歷史與粒子C/U計算／查詢回歸。首次編譯發現粒子原有code與新增code重複，移除新增code保留原代碼後通過。
- 額外靜態比對11個修改控制器：只移除新using、metadata／legacy display wrapper後，與HEAD原程式全文完全一致；舊message/body／原code、分支、SQL與計算均不變。無真實業務寫入。
- 106前端單元含60鍵ledger hash、protected tokens與known descriptor通過，build:test與Release publish通過。既有chunk/table提示保留。瀏覽器本機27／發布27三語情境通過，包含分段重疊、原文參數、typed與舊header、未知／非法回退；業務API全部攔截。
- 發布前唯讀SQL 172.16.110.16 / PMR_SPC_TEST，pending=[]，無model/migration；核IIS SpcSingleTest/、同名pool、backend D:\SPC\release\test\backend與AppEnvironment=test。
- .codex-tmp/i18n-task08c3a/backup 完整前後端逐檔SHA256核對，runtime94與前端4檔、保護設定4檔不變，只重啟該test pool。實際version/health HTTP200 API0.1.77/test/ok，Web0.1.96四檔SHA256等於dist，14同源API請求通過。正式／Portal與schema未改。
- 回復：核同test路徑與pool，停該pool，以backup/backend和backup/frontend成對回復，保留設定／資料、資產先index最後，再啟pool核version/health。無DB回復需求。
- C3分A/B後本批完成剩18件，接續C3B；譯文draft仍留20真人審查，附件掃毒另受阻1／Runtime與TransFiles暫緩2。
