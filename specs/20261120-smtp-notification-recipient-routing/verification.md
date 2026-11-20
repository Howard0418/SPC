# 001 OOS／OOC 分類收件驗證

- 2026-11-20 已加入 OOS／OOC 獨立選人、依部門批次勾選及預設回退提示。兩個公式欄顯示待啟用。
- 後端 15 項測試通過，含實際警報服務的 OOS／OOC 對象分流、明確空陣列、無效人員拒絕、既有設定儲存與 SMTP 內容回歸。郵件均以測試替身驗證。
- 前端 12 項 SMTP 三語系與版面案例通過。3 項儲存與寄測試信案例初跑被 WEB `unknown`／API `test` 環境保護擋下，明示測試 Web 環境後 3 項通過。測試站 Web build、三語系 2,888 鍵及 API Release publish 通過。前端建置使用隔離來源，未包含工作區既有趨勢圖改動。
- 測試 Web `0.1.167` → `0.1.168`、API `0.1.156` → `0.1.157`。首頁、設定頁及主 JS／CSS HTTP 200，資產 MIME 和 Web 版本正確。瀏覽器驗證發布資產的同源 API 基底，實際 health／version 正常、環境 `test`，SMTP 設定匿名 401；`:8901` 使用者端直連版本及環境正確。
- 部署保留測試站設定檔雜湊，無 migration。回復使用 `release/test/backend.backup-smtp-recipients-1120-20261120` 和 `release/test/frontend.backup-smtp-recipients-1120-20261120` 的程式與 Web 資產，保留現行設定檔後重驗。
- 尚待有權限的使用者登入測試站，用指定測試信箱做實際收件驗收。未發實際郵件，正式站未發布。
