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

## 正式發布授權與驗收

- 2026-11-25 使用者明確授權修正正式 SPC 人員比對的 Portal API 位址並發布。
- 範圍：沿用已部署的正式候選，只套用測試已驗證的連線錯誤提示、正式名單 API 位址及 Web／API 版號。保留既有刷新入口及權限。Portal 站台、正式業務資料、帳號及同步不在本次變更範圍。
- Given 正式編輯者進入「作業人員與權限主檔維護」，When 點選「比對 Portal 品保人員」，Then 由正式 Portal API 取得 JSON 並顯示唯讀比對摘要。比對、同步及角色編輯的授權邊界維持。
- 發布前核對目前版本、IIS 路徑、資料庫與 migration，保留站台及正式設定回復點。發布後核對首頁及本批主資產的 MIME、API 版本及 production、網頁實際 API 基底與已登入的比對結果。

## 正式發布驗證

- 正式發布 SPC-PROD-DIRECTORY-1125-W190-R3，來源 74ca3842d3bea494099097d0423e6f7dcc8d2032，以原正式版為基礎。Web 0.1.188 → 0.1.190、API 0.1.172 → 0.1.174。測試維持 Web 0.1.189／API 0.1.173。
- 正式網站設定只改 Portal 名單 API 位址。原始檔已保存並核對 SHA-256；以原檔寫入保留檔案 ACL 與編碼。SQL COPY_ONLY 備份與 CHECKSUM、VERIFYONLY 均通過，無待執行 migration。
- 正式首頁及本版主 JavaScript、CSS 回 200 且 MIME 正確。以瀏覽器實際使用的 8443 API 基底驗證 health、version 0.1.174／production、來源跨網域請求及匿名比對 401，前台顯示 Web 0.1.190 與 API 0.1.174。
- 正式編輯者從「作業人員與權限主檔維護」點選「比對 Portal 品保人員」，成功顯示唯讀比對摘要，Portal 品保 24 人、SPC 33 人。驗證僅執行唯讀比對。
- 回復實測：首批 JSON 預檢失敗後回復。R2 設定檔替換未保留 IIS 讀取權限，導致正式服務短暫中斷。回復原始站台套件、設定及同目錄的原有 ACL，恢復兩個應用程式集區，確認 Web 0.1.188、API 0.1.172 與健康端點。R3 先獨立驗證原檔寫入保留 ACL，部署後再核對實際服務。
- R3 回復點為同批引擎保護的正式站台快照及 protected/PortalDirectory-appsettings.original.json。先回復站台設定並保留有效 ACL，再使用發布工具回復該批應用程式及確認兩個集區。資料庫備份供復原作業使用，本批發布無須還原資料庫。
