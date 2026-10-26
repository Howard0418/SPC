# 08C4A 驗證與發布
- 2026-10-26 三語化本批DONE；既有校驗驗證差異另保留待辦，不宣稱完整校驗回歸全綠。86鍵／258列draft，累計2500鍵三語；14控制器原message/error/body/code/HTTP/success/data不變，僅附固定／狀態metadata；目錄公開HTTP status可為參數，原inner／Webhook／路徑不進args。服務／逐列訊息留C4B/C、通知本文留D、逐頁接線留C5。
- 五個新測試先缺metadata紅燈，實作後本批API與既有Settings／ManualMeasurements／ReportSchedule共14通過；設備拒絕不Save、原error欄位與匿名校驗401保持。109前端單元、86鍵ledger hash／參數／protected terms通過；browser本機36／發布36三語，包括error欄位descriptor、原文參數／header／未知回退，API全攔截。
- 完整校驗124項：122通過、2失敗。先修復舊測試缺既有AuthorizationAccess建構式參數、兩個隔離host漏註冊服務，未改任何原斷言。兩項為：原始docs/QA-2-003-02A 儀器設備校驗管制一覽表-2026年-品保 - 更新.xlsx缺少；unknown合法Editor JWT且無人員主檔預期403、現行共用授權實際200。
- 以本批前HEAD 4c66c77c的git archive建立 .codex-tmp/i18n-task08c4a/baseline，只帶相同三個測試接線修正，未帶本批產品變更；重跑相關6情境同樣2失敗／4通過，證實非本批回歸。權限規則不在三語化中變更，缺少原範本不偽造／不跳過原驗收。另filtered排除DisplayName含unknown與缺範本後117項通過（該filter也排除其他Unknown狀態測試，不把117冒稱122）；完整124結果如上。新獨立驗證待辦CAL-VERIFY-001/002保留。
- 靜態比對14控制器：移除新增using、metadata／顯示wrapper後與HEAD全文完全相同，原權限、SQL／寫入、寄信／Webhook、response原文與現有code保持；本次未呼叫真郵件／Chat／目錄或清除資料。
- build:test與Release publish通過；既有chunk/table與測試host obsolete提示保留。唯讀SQL 172.16.110.16 / PMR_SPC_TEST，pending=[]，無model/migration；核IIS SpcSingleTest/與同名pool、backend D:\SPC\release\test\backend及AppEnvironment=test。
- .codex-tmp/i18n-task08c4a/backup 完整前後端逐檔SHA256核對，runtime94／前端4檔與保護設定4檔不變，只該test pool stop/start。真version/health HTTP200 API0.1.79/test/ok，Web0.1.98四檔SHA256等於dist，14同源API通過。正式／Portal／schema未改。
- 回復：核同test目錄/pool，停該pool，以backup/backend與backup/frontend成對回復，保護設定與資料、資產先index最後，啟pool後核version/health。無DB回復需求。
- C4分A/B/C後本批完成剩18件，接續C4B；譯文draft仍待20真人審稿。既有CAL-VERIFY-001/002另列，原附件掃毒受阻1／Runtime與TransFiles暫緩2保持。
