# 驗證紀錄

- 本機 Chromium 趨勢圖既有回歸 18 項通過。新增焦點測試在本機與已發布測試站各通過 1 次，涵蓋原始陣列、items／data／results 包裝、空清單、格式錯誤及返回摘要沿用快取。功能驗證使用 API fixture。
- testhost build 與三語各 2,897 鍵檢查通過。SPC 測試 Web 0.1.177 → 0.1.178，API 0.1.163/test 維持。
- Chromium 在有效 TLS 下核對測試站首頁 200，主要 JS 200／application/javascript、CSS 200／text/css，前台顯示 Web v0.1.178／API v0.1.163。瀏覽器實際同源 /api/health、/api/version 及主機名稱 :8901 直連皆 200/test。
- 回復點為 release/backups/trend-summary-1123-20261123-112220/index.html。舊版資產保留，可還原備份入口至 release/test/frontend/index.html，恢復 Web 0.1.177 後重跑連線驗證。
- 測試站已發布。API、資料庫與正式站無異動。
