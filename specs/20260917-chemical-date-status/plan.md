# 計畫
- SPC新增GET /api/v1/manual-measurements/calendar?machineId&year&month&samplingPhase&samplingStage，依月份界線唯讀篩選，回傳有資料日期與historical flags。
- Portal同權限代理GET /api/spc/measurements/calendar；Web獨立JS日曆，選擇條件切換即重新查詢並防過期回應。
- 先建SQLite測試日期／階段／來源／刪除及非法參數；日曆UI測試上色、日期選取、不自動載入、失敗及晚到回應。
- 編譯與必要回歸後備份SPC API、Portal API/Web，僅發布release/test，保留設定／附件；失敗回復應用備份。無DB遷移。
