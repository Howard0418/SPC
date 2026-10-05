# 計畫
- SPC ProcessesController.GetMeasurements 加 skip/start/end，先過濾再分頁及計數，time+Id排序；保留原response。
- Portal代理傳遞新參數；頁面每500筆循環讀取，日期傳本地時間字串避免擅改時區規則，最後做既有關鍵字篩選。查詢序號防過期回應。
- 核心測試先建：SQLite記憶體資料>500、同時間排序、日期邊界、空結果與無效參數；頁面載入邏輯測試多頁／失敗／重複。
- 建置三應用後核對IIS test路徑、備份，保留appsettings/web.config，上線測試，再真人已登入頁面唯讀查詢驗證。
- 回復：各測試應用備份；無migration，不需資料回復。正式站不發布。
