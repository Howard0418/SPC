# I18N-TASK-02：共用字典契約與詞彙初稿
- 日期2026-10-24；版本1；狀態：DONE（共用契約/詞彙初稿/檢查設計完成；產品未接線）。
- 授權：使用者允許開始國際化小工作；依每次一件規則，只處理TASK-02，不自動開始03或04。
- 主系統/文件主位置SPC；相依Portal/KM只採共用契約，不修改其repo或執行碼。DB/schema/發布影響：無。
- 基準：[前案分析](../20261022-i18n-task01/analysis.md)、[總任務](../20261022-i18n-task01/tasks.md)；語言zh-TW預設/fallback、zh-CN、en-US。SPC常態頁最新採AD且移除偏態/峰態，範例不得恢復舊介面。
## 需求/BDD
- R-001/AC-001 Given 三套不同框架，When 檢視共用契約，Then 明確定義UTF-8/JSON/nested穩定key、locale、具名placeholder、fallback及版本/hash，不依賴另站線上翻譯。
- R-002/AC-002 Given 一份繁中來源及三語範例，Then leaf key集合與placeholder名稱/出現次數完全相同、來源hash可重算，審核狀態明示draft。
- R-003/AC-003 Given 缺key/重複JSON key/參數漂移/非法locale/來源變更，Then 缺漏檢查設計有明確失敗規則、診斷及測試案例；不將單次範例通過當完整CI工具已實作。
- R-004/AC-004 Given 切語言或文字輸入，Then 契約不翻譯machine值/欄位/URL/公式/權限/文件原文，不改數值解析/AsiaTaipei商業日；格式與文字插值不可注入HTML。
- R-005/AC-005 Given TASK-02完成，Then 文件/範例/詞彙/ledger設計可供後續接線，清楚區分機械完整性與真人翻譯品質，不宣稱97%或三語UI完成。
## 交付與邊界
- docs/i18n/契約、詞彙、檢查設計及examples；只含文件/示意資源，不接入產品、不安裝套件、不建偏好API或DB。
- Happy：三語結構/插值一致；Boundary：重複參數、空字串/極長譯文、N/A及小數/日期；Invalid：重複key/不允語法/缺參數/缺來源；Regression：代碼/公式/原文與權限不變。
- 翻譯皆AI初稿、業務審核者尚未指定，不阻擋技術契約，但不能正式啟用未審敏感/統計文字。
- [計畫](plan.md)｜[任務](tasks.md)｜[驗證](verification.md)。

- [共用規範入口](../../docs/i18n/README.md)。沒有真人翻譯審核/產品runtime驗證成果，與本Task技術交付完成分開記錄。
