# I18N-TASK-07B 粒子監控三語
- 版本1，2026-10-26，DONE，必要驗證與測試發布完成；[授權](../20261026-i18n-continuation/spec.md)／[工作池](../20261022-i18n-task01/tasks.md)。
- ParticleMonitoringView固定UI、日期驗證、圖表presentation與tooltip三語；保留API warning／來源／儀器／位置／單位原文、時間數值格式、C/U原識別與series名稱。
- 不改queryRange台北offset、from/to、location/particleSize/deviceCode trim、chartType、measurements固定pageSize50/sort desc、比較事件時間、controlStatus判定、資料與計算；locale不重讀、不重建圖或清草稿。
- 主系統SPC，無DB／後端／其他系統影響，只發布測試前端；rollback核對test後還原完整前端備份，資產先index最後、保留設定。
## BDD／測試矩陣
- AC1 Happy：Given 已查C/U圖與篩選，When 三語切換，Then UI／軸／legend／tooltip翻譯，原圖表實例、數列、選擇與query保持、無額外API。
- AC2 Boundary：Given 0/null、ready／不足20點及空資料，When 顯示，Then 原值／判定／比較查詢與空狀態保持。
- AC3 Invalid：Given 無效日期或API失敗／raw含HTML/i18n，When 操作，Then 無效日期不送API，固定錯誤可切語言、raw純文字。
- AC4 Regression：Given 三語查詢，When C/U選擇與設備代碼含空白，Then 原三端點與逐欄query保持、固定日期格式／取最新事件不改。
- AC5 三語四尺寸、表格手機橫捲；模板／字典／必要瀏覽器／build／測試發布驗證後提交上傳。
## 任務
- [x] 規格與目的。
- [x] 實作顯示與draft契約。
- [x] 必要驗證與測試發布。
- [x] 文件／提交上傳接續07C。
驗證詳見[紀錄](verification.md)；翻譯draft待TASK-20真人審稿。
