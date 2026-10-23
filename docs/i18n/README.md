# 國際化共用規範
- 契約/詞彙版本：1.0.0（2026-10-24）；I18N-TASK-02。共用規範與參考範例；SPC產品試點另見TASK-04。
- [字典契約](contract.md)｜[詞彙初稿](glossary.md)｜[缺漏檢查設計](validation-design.md)｜[範例說明](examples/README.md)。
- [TASK-02規格與驗證](../../specs/20261024-i18n-task02/spec.md)｜[完整工作池](../../specs/20261022-i18n-task01/tasks.md)。
- 主文件只在SPC維護。後續各系統建置採本機版本化快照，不能在執行時依賴SPC網站或外部翻譯；本次不複製到Portal/KM。
- 範例機械檢查通過不等於三語文意合格；所有翻譯初稿尚待業務審核，沒有97%或全站三語驗收成果。

## TASK-03偏好設計（未實作）
- [本人偏好/身分與schema提案](preferences-plan.md)｜[後續驗證設計](preferences-validation.md)｜[契約案例](examples/preference-api-cases.json)。僅文件，不代表已有跨站同步或偏好資料表。

## TASK-04 SPC測試站試點
- [規格/驗證/回復](../../specs/20261024-i18n-task04/verification.md)：共用App47鍵三語與隔離本機偏好；測試Web0.1.72，Portal偏好API未接入、頁面內文未翻譯。
- [初稿審核帳本](../../specs/20261024-i18n-task04/translation-review.json)：141列均draft；不是真人審稿或全站交付。

## TASK-05A 基本主檔（測試站）
- [規格/驗證](../../specs/20261024-i18n-task05a/verification.md)：Parts/Characteristics固定UI新增110鍵，共157鍵三語；抽屜復用原本機語言控制，原API模型與資料名稱保留。測試Web0.1.73，非全部TASK-05完成。
- [本批審核帳本](../../specs/20261024-i18n-task05a/translation-review.json)：330新draft列，與pilot141列共471列；尚無真人審核。
