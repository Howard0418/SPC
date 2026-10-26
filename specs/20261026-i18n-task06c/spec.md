# I18N-TASK-06C 趨勢圖與統計顯示三語
- 2026-10-26；DONE，必要驗證與測試發布完成；依[連續授權](../20261026-i18n-continuation/spec.md)及[工作池](../20261022-i18n-task01/tasks.md)。
- SPC TrendChartView全部固定UI／查詢錯誤／點位抽屜及備註視窗、圖內presentation與tooltip；共享總覽／分段證據沿用既有字典。共用chart標籤factory由B2提取供兩頁使用，原series識別／data不改。
- 保留93天日期驗證／query／CHEM群組／原master、數值／統計公式／p值格式與0.05判定／固定AD、排除與備註端點／DTO／reason、原日期與字串／排序／pointindex與DOM草稿。不得依譯文排序／選版／判定，不新增寫入或恢復偏態峰態。
- 僅locale patch標題／軸／legend／toolbox等文字，保留series／zoom及原實例；tooltip參數編碼。備註視窗復用LocalePicker，統計卡片穩定key不使用翻譯結果。
- 無DB／後端／其他系統／正式變更；只測試站，draft真人審稿留20。
## BDD／矩陣
- AC1 Happy：Given 已選線別／趨勢點與草稿，When 三語切換，Then UI更新，原查詢／選擇／實例／zoom／數列／抽屜／DOM保持，不加API。
- AC2 Boundary：Given 零／null／無資料、93/94天、p值0.049999/0.05/極小值，When 顯示或查詢，Then 原數值格式／未取位判定／限制與空狀態保持；不恢復方法下拉或偏峰峰態。
- AC3 Invalid：Given raw含HTML/i18n或API失敗，When tooltip／表格／原生alert／切語言，Then 原文純文字，固定提示翻譯，失敗保留草稿。
- AC4 Regression：Given 備註／排除／恢復或總覽製圖返回，When 任一語言操作，Then 原端點、pointScope、ID、reason、DTO及重讀不變；原圖表/分段回歸通過。
- AC5 三語四尺寸／手機抽屜與備註操作可達；必要單元／build／本機及發布後驗證後提交上傳接續06D。
## 任務
- [x] 規格與目的。
- [x] 紅燈模板／顯示factory測試、實作／draft ledger。
- [x] 單元／瀏覽器／回歸／build／測試發布。
- [x] 文件／提交上傳接續06D。
Rollback：核SpcSingleTest/test與目錄，完整前端備份；原資產先index最後覆回同目錄、不覆設定。見[驗證](verification.md)。
