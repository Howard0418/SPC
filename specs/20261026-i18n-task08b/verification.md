# 08B 驗證與發布
- 2026-10-26 DONE。13新鍵／39列draft，累計2225鍵三語；Auth全部原錯誤回應與Viewer guard僅附加code/messageKey/空args。逐段diff確認簽章／時效／nonce／交易／帳號比對／provision／JWT claims與guard條件完全未改。
- 先紅後綠：9新測試原5失敗（舊回應缺metadata）／4原guard放行通過；實作後API31（含新9、Auth nonce replay、既有授權4與08A契約）、隔離SQLite身分12通過；無正式／測試業務資料寫入。
- 102前端單元含13鍵ledger hash／protected terms／空args descriptor通過。build:test與Release publish通過，累計2225鍵；既有chunk與table提示保留。
- 本機與發布各15 API adapter情境，含三語安全提示、known／field／invalid／unknown與明示locale通過；API全攔截，無真SSO／AD操作。401 redirect與JWT空401/403維持既有行為，08A shell17已驗，本次未再改client。
- 發布前唯讀SQL 172.16.110.16 / PMR_SPC_TEST，pending=[]，無migration/model變更；核IIS SpcSingleTest/與pool同名、backend D:\SPC\release\test\backend、AppEnvironment=test。
- 成對備份 .codex-tmp/i18n-task08b/backup 前後端逐檔hash；runtime94／前端4檔、設定保護4檔不變，只測試pool stop/start。實際version/health HTTP200 API0.1.74/test/ok，Web0.1.93四檔SHA256等於dist，14同源API請求通過。正式與Portal未改。
- 回復：確認同test路徑與pool後停該pool，以backup/backend及backup/frontend成對回復，保留設定／資料，資產先index最後，再啟pool核version/health。無DB回復需要。
- 譯文draft未冒稱真人審查；剩16件接續08C，附件掃毒另受阻1、Runtime／TransFiles暫緩2。
