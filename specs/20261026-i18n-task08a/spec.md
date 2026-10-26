# I18N-TASK-08A API三語資源與相容錯誤契約
- 版本1，2026-10-26，DONE，必要驗證與測試發布完成。[連續授權](../20261026-i18n-continuation/spec.md)／[共用契約](../../docs/i18n/contract.md)。08拆A資源與錯誤契約、B身分／權限提示、C業務錯誤與驗證、D郵件通知、E人讀報表，各批獨立驗證；不增加業務功能。
## 範圍與需求
- R1：新增單一來源API JSON模組，前端組合與後端embedded resource共用同檔；重複key／object-leaf衝突／三語key或參數漂移須失敗。只common/SPC資源，不含原資料／設定／機敏值。
- R2：既有ApiErrorResponse保留success/code/message/detail、HTTP狀態及production隱藏內部錯誤；增量messageKey/args。標準model validation保留原ProblemDetails／field keys／errors，附安全code/messageKey/args及fieldMessages，不把原輸入或內部Exception放args。
- R3：顯示語言只接受zh-TW、zh-CN、en-US，無效值固定zh-TW。明示X-SPC-UI-Locale用於request顯示，不從瀏覽器Accept-Language猜測，不改CurrentCulture／JSON數值／日期／匯入與公式。舊message保留繁中相容，新UI按已知messageKey翻譯；背景排程無明示語言維持既有繁中，後續D/E逐一驗證。
- R4：前端request附當前明示locale；getApiErrorMessage維持字串介面、辨識已知API key及合法具名args，未知／不合法descriptor回安全舊訊息；raw來源保持原文，不以譯文控制業務分支。錯誤儲存後的逐頁動態descriptor接線留C，不宣稱A覆蓋所有業務錯誤。
- 主系統SPC；Portal/KM僅後續採契約，本批不改。無DB/schema/角色/登入協定變更；測試Web0.1.92/API0.1.73，正式另授權。
## BDD／驗收矩陣
- AC1 Happy：Given 已知例外或無效model，When API回應／前端顯示，Then 舊欄位與狀態相同、新key/args安全，三語可解譯。
- AC2 Boundary：Given 0/null、重複參數、raw包含HTML/i18n符號或缺少欄位，Then 單次純文字插值，原number/null／field key保持，未重編譯參數。
- AC3 Invalid：Given 非canonical locale、缺key/重複key/參數漂移／不合法descriptor，Then 明確失敗或安全fallback；不得漏出SQL／token／內部例外。
- AC4 Regression：Given 不同使用者語言並行，When 發送請求，Then request語言不互相污染，不變全域Culture、HTTP狀態、授權判斷、JSON小數與日期、舊客戶端契約。
- AC5 backend單元及無DB隔離HTTP、frontend契約／必要瀏覽器、build通過；核IIS test實體路徑、測試資料庫目標與啟動策略，備份前後端／保護設定，發布後版本／health／hash驗證，再提交上傳接續B。
## 回復／限制
- 前後端成對備份與回復，先核test環境、資產先index最後，保留設定／DB檔／data protection；必要時只重啟測試pool，不接觸正式及D:\Sites\PmrPortal。
- API模組譯文draft，真人審查留TASK-20。後續B/C/D/E不因本批完成標DONE。
- [計畫](plan.md)／[任務](tasks.md)／[驗證](verification.md)。
