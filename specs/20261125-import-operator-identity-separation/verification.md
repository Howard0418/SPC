# 量測匯入與登入主檔分離驗證
- 功能 ID：SPC-OPERATORS-IMPORT-IDENTITY-1125
- 日期：2026-11-25，台灣時間
- 狀態：測試發布及驗證完成，正式尚未發布。

| 驗收 | 環境 | 方式及結果 |
| --- | --- | --- |
| AC-001／AC-002 | 本機 SQLite | 通用計量與計數的預覽、確認及資料查回通過。陌生名稱仍保存，已隱藏工號可匯入，原有已授權人員保留。 |
| AC-003 | 本機 SQLite／回歸 | 工號、空白及重送通過，既有藥液隔離／專用匯入與權限、SSO 核心回歸已執行。 |
| AC-004 | SPC 測試站 | Web 0.1.189 → 0.1.191／API 0.1.173 → 0.1.175。實際首頁、JavaScript、CSS 均 200 及正確 MIME。同源及 8901 直連 health、version 回 200，回傳 test。 |

- 六個新增案例在修正前有四個失敗；修正後全部通過。焦點與回歸合計 89 通過，其中 API 套件 77 通過，另有 1 項既有隔離 SQL 測試略過；身分套件 12 通過。
- 兩個既有 Portal 量測點排除測試，在本次及基準提交 f85a847c 都以相同錯誤失敗。既有失敗獨立重驗，不納入 89 個通過。
- API Release 與 Web testhost 建置成功，三語目錄 2,919 鍵。Chromium 確認目前入口顯示 Web 0.1.191、API 0.1.175；畫面從同源 API 取得測試站的實際版本回應，正常驗證 TLS。匿名匯入確認與人員比對均回 401。
- 保留測試站 appsettings 及 IIS 配置，SHA-256 一致。備份位於 release/backups/operator-import-identity-1125，可回復 Web／API 及發布紀錄，再核對原本兩版。無 schema 或共享測試資料寫入。
- 正式站發布與正式人員整理仍需授權。依[正式個案](../20261125-production-operator-account-merge/verification.md)依序辦理。
