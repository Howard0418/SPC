# 08C3B 驗證與發布
- 2026-10-26 DONE。79新鍵／237列draft，累計2414鍵三語；既有服務例外只Attach明示descriptor，保留Exception實例／型別／Message／ParamName／Inner與catch順序。controller只取私有marker，未知仍原message-only／success=false/data=null envelope，不由文字猜key，global500仍隱藏原輸入與inner。
- 核心先紅後綠：後端缺ApiUiException／4參數legacy helper、前端動態header解析缺少紅燈，實作後64必要後端測試通過（含真無DB Kestrel動態header、unknown fallback、原Exception與Inner、原日期／decimal訊息、全域500安全、粒子三種拒絕、既有化學公式／排程／Portal批次／F表／歷史／trace）。未執行真SQL併發或業務資料寫入。
- 首次含FormulaTraceCompatibility filter為59 pass/1 skip（需真Portal來源）；另執行原Invoke-FormulaTraceBaseline.ps1，讀真D:\PmrPortal evaluator與TransFiles來源，Portal公式15、TransFiles來源特徵10全通過／無skip，不啟動GUI／網路匯入。最後64必要測試不重複混入已補基準。
- 額外靜態比對9個修改服務：僅移除新using與明示Attach後，與原HEAD全文完全一致。原公式、日期／decimal parsing、精度、預覽hash／token、交易／SQL與throw／catch條件不變。原日報欄位合併錯誤留C4，已存訊息與巢狀畫面留C5，不冒稱完成。
- 原動態string body保留，X-SPC-Message-Args受限4096字元base64url UTF8 JSON；已知key/exact plain args才解譯，nested／錯UTF8／非法JSON／未知／過長fallback。108前端單元通過，ledger hash／參數／protected terms完整。browser本機33／發布33三語情境通過，含UTF8與HTML/i18n原文單次插值，全部業務API攔截。
- build:test與Release publish通過，既有chunk/table提示保留。發布前唯讀SQL 172.16.110.16 / PMR_SPC_TEST，pending=[]，無model/migration；核IIS SpcSingleTest/、同名pool、backend D:\SPC\release\test\backend與AppEnvironment=test。
- .codex-tmp/i18n-task08c3b/backup 完整前後端逐檔SHA256核對，runtime94與前端4檔、保護設定4檔不變，只重啟該test pool。實際version/health HTTP200 API0.1.78/test/ok，Web0.1.97四檔SHA256等於dist，14同源API請求通過。正式／Portal與schema未改。
- 回復：核同test路徑與pool，停該pool，以backup/backend和backup/frontend成對回復，保留設定／資料、資產先index最後，再啟pool核version/health。無DB回復需求。
- 剩17件接續C4，譯文draft仍留20真人審查；附件掃毒另受阻1／Runtime與TransFiles暫緩2。
