# I18N-TASK-08C2 其餘主檔業務驗證三語
- 2026-10-26 DONE，必要驗證與測試發布完成，依[連續授權](../20261026-i18n-continuation/spec.md)，[分批](../20261026-i18n-task08c1/spec.md)。主系統SPC，不改DB／角色／claims／公式，僅MasterDataV2Controller非PPC錯誤與顯示資源。
- R1 原物件錯誤附加code/key/args，保留message與附加usage等欄位。動態製程／帳號／工號／scope／GroupType保留原文具名args，不以譯文控制分支。
- R2 原string response body必須原樣保持，使用明示X-SPC-Message-Key header附加已知無參數key，前端只在string body時依白名單key解譯，非法／未知header維持原文；不依訊息比對猜key，不更動HTTP／content negotiation或既有result型別。已有typed object descriptor優先。
- BDD Happy：Given合法主檔，When編輯儲存，Then成功與資料結果相同；Given現有拒絕條件，Then原HTTP/message/usage相同且可三語解譯。
- BDD Boundary：Given已有相同啟用group／帳號工號、WE固定規則及有使用中的製程，Then原防呆保持；原名稱含HTML／i18n符號只純文字插值。
- BDD Invalid：Given無必填欄位／錯分頁／未知header，Then原400/404與raw fallback不變，不寫入；嚴格known API key及空args限制。
- BDD Regression：原PPC與權限／身分回歸保持；新header只顯示，不可變更auth、環境guard或Culture，既有機器回應string body原樣。核心先測試；frontend、無DB HTTP／InMemory必要驗證，build與測試發布。
- 測試Web0.1.95/API0.1.76，發布先核IIS/PMR_SPC_TEST與pending migration，成對備份，只test pool；回復前後端成對、保護設定／資料，無schema或正式更新。譯文draft待20真人審查。
- [任務](tasks.md)／[驗證](verification.md)。
