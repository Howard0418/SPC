# 日期公式與管制圖示範資料
- ID：SPC-FORMULA-CHART-DEMO-20261021；狀態：DONE（資料建立及技術驗證，使用者需求確認待操作）。
- 使用者明確要求寫入SPC日期公式/管制圖測試資料供需求確認，之後自行清除。
- 主系統SPC；僅PMR_SPC_TEST/測試站，不改Portal/KM/正式站、不改schema或角色、不發布。
- 建立專用DEMO-FORMULA-20261021標記的製程/機台/線別/槽位/特性/PPC，沿用既有I-MR圖型。兩公式A Primary*2、B Primary*3，初驗與複驗保留實際引擎證據；不偽造trace或套用至既有業務PPC。
- 日期2026-10-10～10-21共12日；10/10～15為A，10/16～21為B。兩個規格/管制分段對應日期；最後一日刻意超界，供觀察異常。固定資料輸入，無真人個資。
- 使用既有Schedule/Preview/Confirm/SpcService產生receipt、trace與統計。此為示範資料寫入，不宣稱TransFiles/真人SSO端到端驗收。示範呼叫採不寄信的IEmailNotificationService，避免測試告警寄出。
- 驗證：寫前確認DB名稱及既有migration，不Migrate/EnsureCreated；建立後確認12量測、24初複驗trace、兩公式版本、統計結果及GetInteractiveChartAsync點位/分段。重跑以專用代碼及每日唯一requestId避免重複。
- 回復/清除：先停用此PPC即可退出日常查詢；使用者清除前依交付ID核對，trace為不可變證據，不以全庫刪除/停用trigger清除。程式不自動刪測試資料。執行結果及範圍記於verification.md；輸出ID清單不進Git。
