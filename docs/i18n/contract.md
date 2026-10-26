# 共用字典契約 v1.0.0
適用SPC(Vue3)、Portal(Razor/既有Vue2島狀頁面)、KM(Razor)。本文定義跨框架資料契約；各runtime介接與相容性測試由後續Task完成。
## 1. 檔案與穩定key
- UTF-8無BOM、嚴格JSON：不允註解/尾逗號/重複成員/NaN/Infinity。根為非空object，內部為非空object，leaf必須非空string；不使用array、null、數字或bool當譯文。
- 採nested物件；攤平key例`common.actions.save`。segment規則`^[a-z][A-Za-z0-9]*$`，不能包含點、空白或以中文句子當key；禁`__proto__`/`prototype`/`constructor`。
- 頂層namespace：common、spc、portal、km；common只供確實相同語意/使用情境，不為避免重複而合併不同操作語意。
- 產品輸入可按module分檔，三語必須具有同樣模組/leaf key集合；同一locale匯入分檔後重複leaf或物件/leaf衝突必須失敗，不採後載覆蓋。
- 本Task三語單檔範例用來說明契約，不代表產品須整包載入三套模組。只打包本系統namespace與common，禁止透過dictionary載入其他系統資料。
- 新增key不使用畫面位置/行號命名。文意相同的小改可保留key但重設審核；語意或參數契約改變須新key並保留相容過渡，不直接刪除已使用key。
## 2. 語言代碼與fallback
- 只接受精確代碼zh-TW、zh-CN、en-US；預設/fallback固定zh-TW。zh-TW標示繁體中文、zh-CN標示简体中文、en-US標示English，語言選單顯示原生名稱。
- null、空字串、其他代碼（包括en、zh、en_US、zh-tw）不直接存入偏好，視為無效並用zh-TW。若未來要支援alias，須集中映射/測試，不能讓三站各自猜測。
- 語言來源/跨站持久化屬TASK-03/09，不在本契約建立Cookie/API/DB欄位；不能由瀏覽器English自動覆蓋預設或使用者已選語言。
- 每個key依序取所選locale→同key zh-TW。來源也缺時開發/CI失敗；runtime顯示`[key]`並記安全診斷，不自動回傳內部例外。正式建置不能帶缺來源key。
- fallback只為runtime穩健；release gate仍要求三語key/參數完整，不可以fallback掩蓋翻譯缺漏。暫存審稿可未翻譯但不得宣稱可發行。
## 3. 具名參數與可攜語法
- 唯一可攜插值語法是`{name}`，參數名`^[a-z][A-Za-z0-9]*$`；例`common.validation.required`以`field`傳入已翻譯的欄位標籤。
- 同key三語參數名稱與出現次數必須一致，以multiset比對而非只比集合；允許為語序調整移動位置。ledger/metadata登記每個參數及語意/型別。
- v1不使用位置參數`{0}`、JS template、ICU plural、Vue pipe/plural/linked-message、內嵌格式函數或.NET `String.Format`。禁止eval；缺參數/多參數在開發與測試失敗，不向使用者留下未替換token。
- 參數先由UI明確格式化為顯示字串，runtime adapter只插值文字，不修改原始模型。英文數量範例採`Records: {count}`避免假裝支援單複數規則；真正plural需求另版契約。
- v1 reference譯文不含literal`{`/`}`/`@`/`|`，避免三種message compiler歧義；若業務需要符號/Email/公式，透過具名參數傳入純文字。adapter不得二次編譯/遞迴解讀參數內容，literal符號支援需在TASK-04/10/15驗證。
- JSON字串內換行只用`\n`；翻譯不可自行變更需要單行的aria/title等surface限制。顯示層決定換行，不在JSON加入HTML標籤或富文字。
## 4. 編碼與不可翻譯內容
- Vue插值/Razor預設編碼/DOM textContent；不得v-html、Html.Raw、innerHTML或把譯文當程式碼/URL。aria-label/title/placeholder用屬性安全API設定。
- API/DB enum值、角色/ACL、權杖、路由/欄位名、檔案key、SQL/Excel公式、原始文件/自由輸入均不可翻譯；畫面顯示另以key映射。
- 不翻譯SPC規則code、Xbar-R/Xbar-S、Cp/Cpk、AD或版本號；術語說明可以翻譯。偏態/峰態已自現行常態UI移除，不能因字典範例恢復卡片或方法下拉。
- 單位依業務基準原值；μm、數字/日期處理不由translator修改。切語言不得影響原始量測、管制線、p-value或有效日選版。
## 5. 顯示格式與API錯誤相容
- locale只影響文字及明確的顯示formatter，不全域切換文化來解析輸入或JSON。API保留number/null與ISO時間；Asia/Taipei商業日期/InvariantCulture匯入與公式保持原規則。
- 常態p-value維持既有4位/極小正值<0.0001/空值處理，以未四捨五入值判定；字典不保存計算結果，不把格式文字送回API。
- 後續API增量可採`{code,messageKey,args,message}`，message舊相容欄位保留；args只允安全顯示資料，不包含密碼/token/內部例外。未知key退回既有安全message或通用key，不以譯文控制業務分支。
- 欄位驗證維持原field key與狀態碼，新增可翻譯的messageKey/args。此為介接契約，不宣稱三站API本次已變更。
## 6. 版本、來源hash與審核
- contractVersion/glossaryVersion採三段版號。增key可minor；規範/placeholder破壞變更多major；文案修正patch且相關審核失效。產品runtime與字典版本成對打包/回復，禁止在線上臨時更新。
- `sourceHash = SHA-256(decoded zh-TW string的UTF-8 bytes)`，小寫hex；不含JSON引號或換行附加，不做Trim/Unicode正規化。任一空白/標點/原文變更皆重新審核。
- metadata逐key含context、surface、sourceHash、placeholder名稱/次數、protectedTokens（如AD）；字典值與machine value分離。不得自稱hash證明譯文正確。
- ledger逐key逐locale含sourceHash、translationHash、status、reviewer、reviewedAt、severity、uncertain、comment。初稿為draft/reviewer=null/reviewedAt=null；來源hash變更改stale，不沿用舊reviewed。
- reviewer須為實際審核者，不能填AI為真人簽核；AI初稿/自查不計人工通過率。敏感權限/刪除警告/統計與公式術語全量人工審核、重大誤譯0；分語言報合格率及審核覆蓋率，不能把尚未審稿標成97%。

- translationHash以同樣UTF-8字串hash規則計算當前locale譯文；即使繁中未改，只改英文/簡中文案也使該locale審核stale。ledger由key/locale引用字典譯文，不另外複製文字造成雙重來源。

## 7. API 增量錯誤顯示實作補充（2026-10-26，08A～08C3B）
- API JSON 模組為前後端共用單一來源，後端 embedded resource；`X-SPC-UI-Locale` 僅明示 canonical 顯示語言，不改全域 Culture／資料解析，舊 `message`／HTTP 與欄位保持。
- 原 string response body 保持原樣，`X-SPC-Message-Key` 為附加已知 API key；具名參數使用 `X-SPC-Message-Args`（base64url UTF8 JSON，上限 4096 字元）。前端只接受已知 key 與 exact 純字串／有限數值參數；超長、非法編碼／JSON、未知 key 或 nested args 均回原文，不編譯外來模板。
- 服務只在明示例外建立點附加 metadata，原 Exception 型別、Message、ParamName、Inner 與 catch 分支不變；controller 只讀本模組 marker。未知例外不從文字猜 key，全域 500 隱藏內部錯誤策略保持。不得把 token、簽章、actor、SQL 或原公式 JSON 放入 args。
- 逐列訊息可附 `index/descriptor`，原 Errors／Warnings 陣列保留；來源文字或索引改變須丟棄過期 metadata。既有 UploadError 只按穩定 code 提供摘要／建議，原完整歷史錯誤保持原文，不推定遺失的歷史主檔參數。
- `details` 只容納明示安全子 descriptor，不放 inner Exception。前端最多 64 節點、子層深度 3，先驗證整棵已知 key／plain args 樹，再單次純文字組合；未知／循環／超限回原始安全訊息。metadata 不寫入既有匯入 payload 或稽核 JSON，API 顯示與歷史證據分離。
