# 缺漏檢查器與CI設計（工具尚未實作）
## 輸入/輸出契約
- 輸入：版本manifest、三語模組JSON、message-metadata、review-ledger及程式引用key清單。先限制repository內允許目錄，不讀使用者附件/設定/帳密或發送外部翻譯。
- JSON parser必須保留object member pairs，在建立map之前拒絕重複key；一般JSON.parse/ConvertFrom-Json最後值覆蓋不足以驗證。
- 先驗語法→nested結構/命名→三語leaf集合→參數multiset/禁用語法→sourceHash/protectedTokens→ledger→引用與疑似硬編碼。
- 只輸出相對檔名、key、錯誤代碼、期望/實際參數名及安全摘要；不得輸出業務參數值。機器報告放忽略的測試輸出目錄，不提交。
- 未來退出碼建議：0機械檢查通過、1內容/契約違反、2檔案或CLI設定錯誤。未設計/實作的檢查不得回報pass。
## 分階段門檻
- 機械檢查：draft可存在，但缺來源/三語key或參數不一致/重複/非法語法一律失敗。來源hash、translationHash與ledger必須符合本次文字，stale必須揭露。
- release檢查：除機械通過外，當次產品實際使用範圍所有敏感key須reviewed且reviewer/日期/來源hash有效；其他語意品質及覆蓋門檻沿TASK-01人工政策。範例全draft應被release gate拒絕，而不是產品功能故障。
- 不做「所有字典值都必須不同」檢查：AD、English、版本及特定code合法相同。相同翻譯可警告人工查核，不能自動替換。
- 硬編碼掃描僅產待分類清單，區分測試fixture、業務文件、使用者文字、機器值與UI；字典掃描過關不代表完成全站翻譯。動態組key須登記允許集合，不能用任意API值拼出namespace。
## 必要檢查與測試矩陣
| 類別 | 情境 | 預期 |
|---|---|---|
| Happy | 三語nested結構/leaf/參數一致，hash有效 | 機械通過，draft仍顯示未審 |
| Boundary | 參數順序不同、同參數重複兩次且次數一致 | 通過 |
| Boundary | English/AD原生名稱相同 | 依protectedTokens與情境通過 |
| Boundary | 0、0.0499999、0.05、極小p-value/ISO日期 | 字典不改值，後續formatter/adapter測試原值與判定不變 |
| Invalid | 同object重複key、物件/leaf衝突、array/null/空文字 | E_JSON_DUP/E_STRUCTURE/E_EMPTY |
| Invalid | 缺locale檔、未知locale、字面點key/非法segment | E_LOCALE/E_KEY |
| Invalid | zh-TW缺key、其他語言少/多key | E_BASE_MISSING/E_KEYSET |
| Invalid | 少/多參數、相同參數出現次數不同、{0}/未配對括號 | E_PLACEHOLDER |
| Invalid | literal compiler特殊語法或HTML標籤 | E_MESSAGE_SYNTAX/E_HTML |
| Invalid | 原文或譯文變更仍用舊hash/舊reviewed | E_SOURCE_HASH/E_REVIEW_STALE |
| Invalid | reviewed卻無reviewer/日期、unknown status | E_REVIEW_METADATA |
| Regression | fallback選取、繁中也缺、未知locale | 回退次序正確；基準缺key在CI失敗 |
| Regression | 參數含HTML/Email/@/管線與大括號 | adapter輸出純文字，不執行/二次編譯 |
| Regression | 切語言保留表單/焦點/查詢與ECharts狀態 | 各系統實作Task驗收，本Task不假稱已測 |
## TASK-02驗證範圍
- 本次可用一次性本機腳本檢查範例與故障fixture；不是交付完整checker/CI，也不改產品依賴。
- 後續TASK-04/10/15於導入各自adapter時，實作或重用可執行檢查；以此契約安排單系統Task。TASK-19做跨站、TASK-20真人審稿，不能提前核銷。
