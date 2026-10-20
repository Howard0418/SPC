# 計畫
- 核心測試先行，沿用現有ChartSegmentDetailsService／元件，新增小型ChemicalTraceQueryService及唯讀API。
- 量測先依PPC／segment／point篩選與分頁；分段顯示各階段最新證據，單筆模式對全部trace歷程分頁。原始receipt只投影可閱欄位，不回傳預覽token。
- 共用ChemicalCalculationDetails.vue整合分段、管制圖點與趨勢圖點；子組缺成員ID不拿第一筆冒充子組唯一公式。
- Happy：新舊混合、初複驗、點選；Boundary：跨日／多版本／分頁／子組；Invalid：損壞快照／越PPC／錯segment／負skip；Regression：既有11分段瀏覽器、排程與統計回歸。
- 交付Antigravity具體操作及SQL／API／UI自動驗證紀錄；不宣稱正常使用者人工SSO已驗收。
