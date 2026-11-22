# 驗證紀錄

- 測試庫 C1 主檔唯讀核對，鍍銅1／鍍銅2 的槽位順序為 1／2，各槽內依序為硫酸、硫酸銅、氯離子、S RF-A、S RF-B。
- 12 項 Chromium 回歸通過。新增交錯 API 資料與槽位順序相同案例，驗證畫面先列完整的鍍銅1，再顯示鍍銅2，槽內依 PPC 順序。testhost build、三語各 2,897 鍵及前台版號驗證通過。
- SPC 測試 Web 0.1.176 → 0.1.177，API 0.1.163/test。首頁及主 JS／CSS 200、MIME 正確；瀏覽器同源與 :8901 直連 API 及 health 通過。正式區未發布。
- 回復點 release/backups/chemical-overview-order-1123-20261123-075755/index.html，原資產保留。無主檔、公式或資料庫寫入，登入後畫面待使用者確認。
