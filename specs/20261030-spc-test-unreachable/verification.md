# 驗證紀錄

## T-001 唯讀盤點（2026-10-30）

- Portal API health：`http://172.16.110.27:8091/health` 回應 HTTP 200，資料庫環境為 test。
- SPC API health：`https://SPCTEST.pmr.com.tw:8901/api/health` 回應 HTTP 200。
- SPC Web：`https://SPCTEST.pmr.com.tw/` 回應 HTTP 200、`text/html`。
- 前端 JavaScript：`/assets/index-T6sye44A.js` 回應 HTTP 200、`text/javascript`，檔案大小 2,834,495 bytes，未重現 MIME 回傳 HTML 問題。
- 本次未修改 IIS、設定、資料庫或正式環境。

## 結論

目前本機 SPC 測試站端點與靜態資源均可正常回應；原回報的「無法連結」未在本次盤點重現。若使用者仍遇到問題，需補充發生時間、完整網址及瀏覽器 Console/Network 截圖，以辨識個別瀏覽器快取、Token 或網路路徑差異。
