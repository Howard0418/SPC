# 06C 驗證與發布
- 2026-10-26：54新鍵／162列draft；累計1389鍵三語。TrendChartView固定文字、原生錯誤、統計卡、tooltip與圖內標籤接入；共享chart factory提取，原series名稱及資料保持。
- 72 i18n單元、5共用顯示／factory測試通過；build:test通過，保留既有chunk大小提示。
- 本機三語四尺寸12、p值0／極小值／0.049999／0.05／null 5、日期93／94天與反轉1，共18情境通過。圖表實例／數列／zoom、日期、查詢次數、未處理JS錯誤驗證；手機英文截圖檢視。
- 原分段／點位17與固定AD常態8回歸通過。完整點位備註寫入流程沿用B3，06C未改DTO／reason；本批未新增實際業務寫入驗收。固定AD、不恢復偏態峰態。
- 發布後18情境通過；發布版不讀ECharts內部option，實例／數列／zoom深度斷言在本機dev完成；發布驗證canvas保留與UI／查詢／日期。所有業務API攔截，無真資料異動。
- 真匿名version/health HTTP200、API0.1.72/environment=test/status=ok；四檔SHA256與dist一致，14同源API請求通過。
- 只SpcSingleTest Web0.1.84，IIS實際backend D:\SPC\release\test\backend、AppEnvironment=test核對通過；完整前端備份65檔，payload4／保護設定4，不重啟pool。
- 回復：核對同一測試目錄，使用 .codex-tmp/i18n-task06c/backup-frontend 備份，先還原資產再index，排除web.config/appsettings，不接觸正式。
- 三語仍為draft，真人審稿留TASK-20；剩16件直接接續06D。正式站未改。
