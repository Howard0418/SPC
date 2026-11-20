# 驗證紀錄

## 目前結果
- 紅燈重現：重導守衛仍有效時，兩支 Operators API 回 401，重新整理按鈕持續 disabled；修正後會結束 loading 並顯示未授權訊息。
- 最小修正：實際導向登入期間仍保留畫面不閃錯；只有已存在重導守衛、頁面卻又收到 401 時，將請求終止為 `AUTH_REDIRECT_GUARD`，讓呼叫端 `finally` 正常執行。
- Playwright：`operators-auth-loading` 2項、`auth-expiry-redirect` 2項、作業人員權限／對話框焦點回歸2項，合計6項通過。
- i18n：2,881 keys × 3 locales 通過；`npm run build:test` 通過。
- 測試站：Web `0.1.166`，API維持 `0.1.156`；首頁、JS／CSS HTTP 200與MIME、health/version、test環境、`:8901`直接API及瀏覽器同源API基底均通過。
- 備份：`release/test/frontend.backup-operators-auth-loading-20261120`；無資料庫、角色、權限或API變更。

## 驗收狀態
| 驗收 | 狀態 | 說明 |
|---|---|---|
| AC-001 | 通過 | 正常 Operators 權限清單回歸通過；使用者確認有效登入畫面可正常完成載入。 |
| AC-002 | 通過 | SSO grace單次導向與並行成功取消導向通過；使用者確認重走錄影路徑後可返回作業人員頁。 |
| AC-003 | 自動通過 | 401守衛及部分500失敗均結束 loading並顯示訊息。 |
| AC-004 | 通過 | 權限顯示回歸、環境隔離與測試站連線驗證通過。 |

## 發布狀態
- 測試站：已發布 Web `0.1.166`；API未改，維持 `0.1.156`。
- 正式站：未授權、未發布。

## 2026-11-20 實站續驗

- Chromium 未登入開啟 SPC 測試站 `/operators`，僅導向 Portal 測試站登入一次，登入頁回 HTTP 200。SPC 同源工作階段儲存保留 `/operators`，Portal 登入的返回路徑為 `/Spc/Launch`。
- 瀏覽器確認同源 API health HTTP 200、版本 `0.1.156`、環境 `test`，受保護的人員 API 匿名回 401。驗收使用已部署 Web `0.1.167`，本次未更新站台。
- 使用者已確認同一頁的權限總覽畫面。登入操作服務目前離線，仍待有效帳號重走錄影中的逾期重導並確認 SSO 返回後解除載入。

## 2026-11-20 依優先順序再次執行

- Windows電腦操作技能初始化後，`list_apps`連續回報native pipe不存在；依指引重試一次、重置核心並重新初始化後仍相同，因此停止UI輸入，未操作登入、帳號、角色或權限，未冒充人工通過。
- `operators-auth-loading.spec.ts`及`auth-expiry-redirect.spec.ts`共4項重新通過：既有重導守衛401可結束loading、部分API失敗顯示可讀錯誤、SSO grace只導向一次、平行成功會取消短暫重導。
- 目前測試站首頁、主JS／CSS均HTTP 200且MIME正確；health=`ok`、API=`0.1.161/test`，匿名`/api/operators`正確回401。部署主資產含Web `0.1.172`及`AUTH_REDIRECT_GUARD`修正。
- 本輪無程式／資料／版本變更，不需發布；正式站未異動。有效登入者重走原錄影路徑仍是唯一未完成驗收，TASK-001保持未DONE。

## 2026-11-20 使用者人工驗收核銷

- 使用者明確回覆「作業人員登入重導驗收通過」，確認有效登入、登入逾期／重新登入、返回`/operators`及人員清單解除loading的錄影路徑已通過。
- 結合本輪4項核心回歸、目前測試站Web`0.1.173`／API`0.1.161/test`、首頁與JS／CSS MIME、`AUTH_REDIRECT_GUARD`部署資產及匿名人員API 401證據，AC-001～AC-004全數通過，TASK-001完成。
- 本次只同步驗收文件，不修改程式、資料庫、角色、權限、版本或測試站；正式站仍未授權發布。
