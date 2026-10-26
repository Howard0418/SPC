# I18N-TASK-06B2 圖表標籤／直方圖與總覽表
- 2026-10-26，DONE，實作／驗證與測試發布完成；依[連續授權](../20261026-i18n-continuation/spec.md)與[工作池](../20261022-i18n-task01/tasks.md)。
- SPC：SpcChartView圖內標題／軸／legend／tooltip、顯示開關／直方圖常態固定UI及SpcSummaryTable。點位寫入表單留B3。
- 保留series name／id與資料／數值／公式、分布bin、p值格式／0.05判定、固定AD、query／DTO／原始主檔／JSON／API原文、日期、排序與排行原值。總覽fallback排序仍使用原中文machine比較，不依譯文排序。
- 語言切換僅更新presentation option，不dispose、不改series／dataZoom、不重算／新增API；tooltip固定文字翻譯、動態資料編碼為純文字，不二次編譯參數。
- 無DB／後端／Portal／正式變更。字典draft，真人審稿留20；測試站發布沿原備份／環境核對流程。
## BDD／驗證矩陣
- AC1 Happy：Given 已繪圖及總覽排序／排行，When 切三語，Then 固定標籤更新，原series與zoom、selected、排序／排行、DOM與URL保持，不查API。
- AC2 Boundary：Given 0／null／小p值／0.05、單邊規格與空總覽／無有效histogram，When 顯示，Then 原數值與判定不變，不恢復偏態峰態或方法下拉。
- AC3 Invalid：Given API名稱／備註含HTML與i18n符號，When tooltip或表格顯示，Then 保留純文字；原錯誤／note不翻譯。
- AC4 Regression：Given 各雙軸類型／多series與日期分段／點位，When locale patch或繼續點擊，Then 只改文字surface，series索引／互動與原讀寫契約保持。
- AC5 三語四尺寸與滾動／點位／原常態回歸，必要build／來源hash／字典／測試後發布測試站並逐件提交接續B3。
## 任務
- [x] 規格及目的。
- [x] 先建presentation單元與模板檢查，再實作／draft ledger。
- [x] build、瀏覽器／回歸及發布驗證。
- [x] 文件／提交上傳，直接接續。
回復：測試frontend完整備份資產先index最後回同目錄，保護設定；詳[驗證](verification.md)。
