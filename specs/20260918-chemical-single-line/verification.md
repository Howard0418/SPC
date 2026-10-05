# 驗證與發布
- 先建排序/階段測試失敗，再實作。後端9個不同案例通過（Etch既有7例＋ChemicalSequence2例），含實際SQLite服務圖表順序、管制界限試算一致與MR相鄰差；未改咬蝕分組。
- Node3例通過：只針對N1/N2 CHEM、階段標籤、執行實際renderECharts函式確認同日3筆仍為單一量測線，MR兩點軸對齊，中班/收線tooltip正確。未自動操作瀏覽器。
- SPC API Release publish及前端build:test成功；Vite仍有既有大bundle提示。
- API0.1.59、前端0.1.58已发布D:/SPC/release/test/backend及frontend。設定保留、備份、部署hash相符，health/首頁均200；詳../../release-staging/chemical-single-line-20260918/deployment.json與frontend-deployment.json。
- 實站唯讀驗證：N2 PPC2512，9月15點，ID順序與資料庫一致，samplingStage OPEN/CLOSE均保留，全部MR與相鄰差一致。證據../../release-staging/chemical-single-line-20260918/live-verification.json。
- 資料庫未修改、正式站未發布。人工畫面確認：重新整理SPC測試站，選N1或N2藥液項目及日期區間，看單一量測線並將滑鼠移到點位核對班別/階段。
