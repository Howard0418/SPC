# 驗證紀錄

- testhost build 通過，三語各 2,897 鍵。隔離 Chromium 在 1280 與 390 px 驗證繁中／簡中／英文，側欄群組與連結字級均為 16 px，選單標籤無水平溢出，版號為 0.1.176。
- SPC 測試 Web 0.1.175 → 0.1.176；API 0.1.163/test。實站首頁、主 JS／CSS 200 且 MIME 正確，Chromium 同源及直連 8901 API 核對成功，health 200。
- 正式未發布。測試回復點 release/backups/sidebar-1122-20261122-174611/index.html，前版雜湊資產仍保留。本次未含工作區既有趨勢圖修改。
