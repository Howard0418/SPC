# 小型變更 Portal 人員比對連線錯誤處理

- 功能 ID：SPC-OPERATORS-DIRECTORY-1125
- 主系統：SPC API。相依系統：Portal 品保名單 API。
- 診斷：正式 SPC 人員比對回未處理的 500。應用程式紀錄顯示舊 Portal HTTP 位址 307 轉向 IP 的 HTTPS，握手時連線被重置。從正式 SPC 主機使用 Portal 正式 API 主機名稱查驗健康及受保護的名單 GET，均回 200，名單內容為 JSON。
- R-001：上游 HTTP／TLS 連線失敗與超時時，回傳可理解的 502 連線提示並記錄日誌。取消由呼叫端決定。沿用既有非 JSON 及上游非成功回應，保留人員同步前的查驗。
- AC-001：單元測試覆蓋成功、無效內容、TLS、逾時、呼叫端取消及同步讀取失敗。同步遇到上游中斷時尚未開始資料交易。
- 執行：測試站發布、核對前後 Web／API 版本、主資產、實際 API 基底及測試環境。正式連線設定更新須另行授權，另案更新正式 Web 與 API 版號。正式資料、帳號與設定僅唯讀診斷。
- 回復：套用前的測試 Web／API 快照與原設定；正式環境回復為同次發布前的應用程式與連線設定。

## 測試與發布紀錄

- BDD：Given Portal 上游 TLS 連線中斷或逾時，When 編輯者查詢或執行同步前置名單讀取，Then 系統回 502 與可翻譯的 DIRECTORY_CONNECTION_FAILED，日誌保留例外，資料交易尚未開始。呼叫端取消仍傳遞取消。
- 修正前新增案例重現 3 項例外。修正後人員比對及權限稽核回歸 44 項通過，1 項既有隔離 SQL 測試略過。API 發布建置及 Web testhost 建置成功，三語 2,919 鍵通過。
- SPC 測試 Web 0.1.186 → 0.1.189／API 0.1.170 → 0.1.173。首頁、目前 JS／CSS MIME、瀏覽器同源 /api 及 8901 直連 /api/health、/api/version 均成功且環境為 test。匿名比對回 401。Portal 測試目錄回 200 JSON。原測試環境設定雜湊一致。回復點 release/backups/operator-directory-1125。
- 正式主機舊來源回 307，IP 目的的 TLS 握手中斷，應用程式紀錄確認為 HttpRequestException 及連線重置。同一主機以正確 Portal API 主機名稱查健康回 production，帶既有服務憑證的唯讀名單 GET 回 200 JSON。正式 SPC 連線設定及發布待另行授權。
