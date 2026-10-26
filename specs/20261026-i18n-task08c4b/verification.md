# 08C4B 驗證與發布
- 2026-10-26 本批三語化DONE。55新鍵／165列draft，累計2555鍵三語；校驗／證書／匯入結構／提醒／Webhook與粒子輸入例外明示Attach，原型別／Message／Status／Code／ParamName／Inner不變。controller只加key/args，原code=ex.Code或INVALID_REQUEST保持，不採generic descriptor code覆寫。範本header預期欄名為原機器格式，不翻譯接受值。逐列errors/warnings／彙整例外留C4C，逐頁畫面留C5。
- 核心先紅後綠：三項新測試原缺key／設備record metadata失敗後實作；21必要API測試通過，含原CalibrationException Status/Code、空檔、設備來源不可用／時間缺漏與同一快取物件／HTTP呼叫數、既有例外／設備拒絕／粒子匯入與設定回歸。設備來源隔離3測試通過。
- 校驗完整124項為122通過、2失敗，仍是C4A已以本批前HEAD重現的缺原始範本／unknown Editor JWT授權預期差異，沒有新增失敗。原斷言保留，未改權限，不宣稱全綠。既有InvalidFile HTTP測試追加驗證code=VALIDATION、原message與新key，於122通過之列；郵件／Chat皆假服務，不送真通知。CAL-VERIFY-001/002繼續待辦。
- 靜態比對10個服務／controller：移除明示metadata後與原HEAD全文完全一致，包含CalibrationRules授權部分。Chameleon DTO僅加MessageKey/空Args、原Message/Status不改；cache key/TTL/數值/HTTP分支及時間維持，新增隔離測試覆蓋原快取與請求數。無真Chameleon／SQL業務寫入。
- 110前端單元通過，55鍵ledger hash／placeholder與protected terms完整；本機39／發布39三語API adapter情境通過，包含原VERSION_CONFLICT與translated display，raw HTML/i18n參數安全。所有業務API攔截。
- build:test與Release publish通過；既有chunk/table與測試host obsolete提示保留。唯讀SQL 172.16.110.16 / PMR_SPC_TEST，pending=[]，無model/migration；核IIS SpcSingleTest/與同名pool、backend D:\SPC\release\test\backend及AppEnvironment=test。
- .codex-tmp/i18n-task08c4b/backup 完整前後端逐檔SHA256核對，runtime94／前端4檔、保護設定4檔不變，只該test pool stop/start。真version/health HTTP200 API0.1.80/test/ok，Web0.1.99四檔SHA256等於dist，14同源API通過。正式／Portal／schema未改。
- 回復：核同test目錄/pool，停該pool，以backup/backend與backup/frontend成對回復，保護設定與資料、資產先index最後，啟pool後核version/health。無DB回復需求。
- 剩17件接續C4C，譯文draft待20真人審稿，兩項既有校驗差異另待辦；原附件掃毒受阻1／Runtime與TransFiles暫緩2保持。
