# I18N-TASK-06B3 點位備註、排除操作與OCAP表單三語
- 2026-10-26；DONE，實作／必要驗證與測試發布完成；[持續授權](../20261026-i18n-continuation/spec.md)／[任務](../20261022-i18n-task01/tasks.md)。
- SPC SpcChartView剩餘固定UI、右鍵選單／備註視窗、異常點列表與品質追溯／OCAP表單及原生alert。既有資料名稱／JSON／API錯誤／班別機器值不翻譯。
- 原POST/PUT/DELETE端點、DTO數值與ID/pointScope/reason、OCAP中文選項value／Open/InProgress/Closed、字串空白、500字限制、保存／清空／取消與錯誤時草稿保持。只翻譯顯示標籤；不新增權限或流程卡控、不寫真資料。
- 視窗復用LocalePicker原handler，不因overlay阻擋語言切換。必要手機樣式僅可達性修正，無後端／DB／其他系統或正式站變更。
## BDD／矩陣
- AC1 Happy：Given 既有點位與備註／OCAP草稿，When 三語切換，Then DOM／原稿／選項原value／數值日期／圖表選擇保留、不新增API。
- AC2 Boundary：Given 零量測值、null／無批號／無ID、備註500字、空或既有備註，When 保存／清空／取消，Then 端點與原DTO／長度及身份阻擋保持。
- AC3 Invalid：Given API拒絕或參數含HTML/i18n外觀，When 顯示錯誤／切換，Then 固定提示翻譯、原文純文字，失敗不丟草稿、不送錯ID。
- AC4 Regression：Given 點位排除／恢復、OCAP狀態與中文選項，When 任一語言送出，Then machine values及請求與原行為相同，圖表／備註重讀不變；原權限不變。
- AC5 三語四尺寸、備註視窗操作可達；模板／ledger／build與相應自動化後發布測試站、提交上傳接續06C。
## 任務
- [x] 規格／目的。
- [x] 模板檢查、UI字典與draft來源、顯示層實作。
- [x] 單元／瀏覽器DTO與回歸／build／測試發布。
- [x] 文件同步、提交上傳，接續06C。
Rollback：核test目錄，以完整前端備份資產先index最後回同一目錄、不覆設定。見[驗證](verification.md)。
