# I18N-TASK-06B1：管制圖查詢與監控摘要介面三語
- 版本1；2026-10-26；狀態：DONE，已發布測試站；B2／B3未開始。
- 授權：使用者「開始下一項」；[任務](../20261022-i18n-task01/tasks.md)／[翻譯契約](../../docs/i18n/contract.md)；沿用已確認固定AD與數值、日期、匯入分工基準。

## 範圍與計畫
- SPC SpcChartView.vue：頁首／查詢欄位／週月報控制／查詢與下載錯誤／監控摘要／下載與排除清單固定UI三語；至 Chart Display Box 前的固定介面。
- 06B依規模拆B1本件、B2圖內標籤／直方圖與總覽表、B3點位備註／排除操作及OCAP表單。只完成本件；共享總覽表與圖內文字尚未翻譯不得標全頁完成。
- 主檔名稱、群組名稱與代碼、enum／DTO、API URL/query、日期與31/93天限制、數值／取位、AD、CSV內容／檔名、圖表數列／zoom與備註草稿保持。顯示字典不能用作選項值、搜尋或業務判斷；錯誤於顯示時翻譯，API原文保留。
- 無Portal／TransFiles／後端／DB／正式站變更；必要時僅此元件使用的報表／分析別名路徑沿用現有手機堆疊導覽。
- 手機測試紅燈：週月報月份欄位44px，僅 `/monthly-control-chart`、`/process-analysis/capability`、`/process-analysis/violations` 沿用已驗證堆疊，與既有 `/spc` 對齊。
- 截圖發現英文週期選項與查詢按鈕共列仍截字，手機查詢控制改全寬、按鈕另列；sm以上維持原排列。
- 不新增角色權限，不改排除／恢復／OCAP寫入邏輯；本件只翻譯排除清單既有顯示與恢復按鈕，寫入仍原DTO。
- 先記目的與模板檢查，再接字典／draft ledger，完成必要build、query/CSV與狀態保留瀏覽器驗證後發布測試站，提交並上傳，STOP。

## BDD 與矩陣
- AC1 Happy Path：Given 使用者在spc／能力／異常分析／週月報路徑，When 切三語，Then 頁首／查詢／摘要即時改變，選項原ID／DATE/PART/MONTH/WEEK與主檔原文、既有日期、DOM／URL／圖表狀態不變，不發額外API。
- AC2 Boundary：Given 0／null規格與能力、空總覽、ALL與單一線別／料號、31/93天及週月邊界，When 查詢，Then 既有數字取位及query保持，超限或日期倒置仍阻擋，預設與補空日期規則保持。
- AC3 Invalid Input：Given API錯誤原文／主檔含HTML/i18n外觀或無資料404，When 查閱／切語言／重試，Then 固定提示翻譯、原文純文字保留，不清除查詢草稿，不擴張查詢範圍。
- AC4 Regression：Given 已繪圖與快取總覽，When 切語言／下載／返回總覽／開排除清單，Then 數列、zoom、勾選、原CSV內容／檔名與query保持；恢復沿用原端點DTO。圖內仍原語言至B2，寫入表單留B3。
- AC5：Given 三語與1920/1366/1280/390寬度，When 操作查詢與摘要，Then 控制項可達、可換行，主檔原名不翻譯。

## 任務與驗證
- [x] T1：規格與工作池拆分，實作前記目的。
- [x] T2：翻譯前模板紅燈、固定UI／錯誤字典與draft來源。
- [x] T3：單元／build、三語瀏覽器及相關原行為回歸。
- [x] T4：備份發布測試站、發布後重驗、文件同步；提交上傳以本件Git紀錄為準，STOP。
- 驗證詳[紀錄](verification.md)；譯文仍draft／uncertain，人工審稿在TASK20。
- Rollback：核實SpcSingleTest/test目錄，備份整份frontend，資產先index最後發布；回復同一目錄備份資產／index，不覆設定、不刪其他資產。
