# 驗證紀錄

## T-001 唯讀盤點（2026-10-30）

- Portal API health：`http://172.16.110.27:8091/health` 回應 HTTP 200，資料庫環境為 test。
- SPC API health：`https://SPCTEST.pmr.com.tw:8901/api/health` 回應 HTTP 200。
- SPC Web：`https://SPCTEST.pmr.com.tw/` 回應 HTTP 200、`text/html`。
- 前端 JavaScript：`/assets/index-T6sye44A.js` 回應 HTTP 200、`text/javascript`，檔案大小 2,834,495 bytes，未重現 MIME 回傳 HTML 問題。
- 本次未修改 IIS、設定、資料庫或正式環境。

## 結論

目前本機 SPC 測試站端點與靜態資源均可正常回應；原回報的「無法連結」未在本次盤點重現。若使用者仍遇到問題，需補充發生時間、完整網址及瀏覽器 Console/Network 截圖，以辨識個別瀏覽器快取、Token 或網路路徑差異。

## T-002 根因補充（2026-10-31）

- `https://SPCTEST.pmr.com.tw/`、`:8901/api/health`、`:8901/api/version` 從診斷端均回 HTTP 200；API 版本為 `0.1.92`、環境為 `test`。
- 已發布前端 bundle 內使用 `https://SPCTEST.pmr.com.tw:8901/api`；同源 `https://SPCTEST.pmr.com.tw/api/version` 回傳 SPA HTML，表示 443 尚未代理 `/api`。
- 根因：使用者瀏覽器需要直接連 TCP 8901；用戶端網路／防火牆／TLS 路徑無法連到該連接埠時，前端 `/version` 健康檢查失敗而顯示 API 離線。
- 建議修正：在測試 IIS 的 443 站台配置 `/api/*` 反向代理到測試 API，再改前端使用同源 `/api`。
- 本工作區不是測試 IIS 主機：不存在 `applicationHost.config`，且未安裝 URL Rewrite；未在錯誤主機修改 IIS 或發布設定。待於 `172.16.110.27` 執行受控設定、備份及驗收。

## T-002 套用嘗試與回復（2026-10-31）

- 使用者回覆已安裝 URL Rewrite／ARR 後，完成同源 `/api` 前端建置：API 基底3項測試通過、i18n 2,759×3通過、Vite build通過；bundle不含8901或舊8081。
- 發布測試站後，首頁及 `/api/*` 均立即回 `500.19 / 0x8007000d`，表示 IIS 尚未註冊或載入 `<rewrite>` 設定區段；不是後端 API 故障。
- 已立即回復原 `web.config` 與 `index.html`，SHA-256與備份一致；回復後首頁、`:8901/api/health`、`:8901/api/version` 均回 HTTP 200。
- 同源代理尚未完成。須先在 PMR-013 確認 `RewriteModule` 與 ARR Proxy 真正載入，再重新套用；正式站未修改。

## T-003～T-005 完成紀錄（2026-10-31）

- 使用者確認 ARR Proxy 已啟用後，同源 `https://SPCTEST.pmr.com.tw/api/health` 與 `/api/version` 均回 HTTP 200 JSON；版本 `0.1.92`、環境 `test`。
- 重新發布測試前端及 `web.config` 至 `D:\SPC\release\test\frontend`。首頁 `/`、SPA 路由 `/spc`、同源 API、目前 JS 資產均回 200；JS MIME 為 `application/javascript`。
- 目前 bundle 含同源 `/api`，不含 `:8901` 與舊 `:8081` API 位址；API 基底3項測試、i18n 2,759×3與 `npm run build:test` 均通過。
- 發布前備份保留於 `D:\SPC\release-staging\spc-test-same-origin-20261031\backup`；需要回復時可還原備份的 `index.html`、`web.config` 與前一版資產。
- 自動瀏覽器驗收因瀏覽器權限被拒未執行，不繞過權限；待使用者 Ctrl+F5 後確認頁尾 API 顯示連線。此限制不影響已完成的 HTTP、路由、MIME與bundle檢查。
- 原進度表及「小工作總表」已同步至2026-10-31；備份雜湊一致，重新開啟／渲染、ZIP結構、公式錯誤掃描及 OpenXML SDK 驗證0錯誤。
- 正式站、正式資料庫與正式 Portal 均未修改。
