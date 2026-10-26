# I18N-TASK-08C5A 共用即時錯誤與主檔接線
- 2026-10-26 DONE，必要驗證與測試發布完成；依[連續授權](../20261026-i18n-continuation/spec.md)。C5拆A共用轉接及料號／特性／製程／管制群組、B其餘頁面與巢狀DTO，無新增業務範圍。
- 主系統SPC；Portal/TransFiles無變更，DB/schema/API契約與版本不變。前端訊息狀態改存經驗證描述，既有字串介面保留供原生alert使用；僅共用轉接採用的四頁改接新介面。
- R1 Given已出現已知API錯誤，When切語言，Then原畫面訊息即時更新；不重新請求、不卸載DOM、不清草稿／query／選取。原字串fallback仍逐字顯示。
- R2 Given複合錯誤或本機UI訊息的巢狀API參數，Then先將已驗證子項翻成純文字再插值；原名含HTML、i18n符號與0不重編譯。伺服器仍只允許已包裝spc.api namespace，外部key不得直接進t。
- R3 Given未知key、錯誤args、循環／過深／過多details，Then沿用既有formatter的原文或根摘要fallback；新狀態為獨立快照，不受原response後改影響。不能改授權、狀態碼、DTO／寫入／公式／原型。
- 測試評估：Happy三語切換，Boundary複合／0／巢狀參數，Invalid未知與超限，Regression舊字串API、四頁及共用shell。先建單元測試再改共用邏輯。
- 發布僅SPC測試Web0.1.101/API沿用0.1.81；核IIS及PMR_SPC_TEST。前端完整hash備份、assets先index後，回復原index/assets，設定不動。正式另授權，譯文仍draft。
- [計畫](plan.md)／[任務](tasks.md)／[驗證](verification.md)。
