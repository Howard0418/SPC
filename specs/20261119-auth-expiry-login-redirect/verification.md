# 驗證紀錄

- 日期：2026-11-19
- 自動化：過期 Token 導登入與短暫 401 後成功回應自動重試，共 2 項 E2E 通過；新增 MutationObserver 證明導向前從未渲染401／載入失敗錯誤框，修正前同測試為1失敗／1通過。
- Build：三語系 2,865 鍵檢查、Vite test build及API Release build通過。
- 測試發布：SPC API `0.1.145`、Web `0.1.153`。
- 實站：Web 首頁 200；主 JS `application/javascript`、CSS `text/css`；API `/api/health`、`/api/version` 200 且為 test；同源API亦為`0.1.145/test`，測試主資產顯示Web`0.1.153`且不含正式API位址。
- 環境隔離複核：瀏覽器實際同源基底 `https://spctest.pmr.com.tw/api` 回 API `0.1.140`／`test`，後端設定 `AppEnvironment=test`、資料庫 `PMR_SPC_TEST`；測試主 JS 不含正式 API `PMR-SPC-SRV:8443`。正式 API 維持 `0.1.134`／`production`，本次未更新。
- 備份：`D:/SPC/release/backup-auth-expiry-test-20261119-20261119-092948`。
- 正式狀態：未發布，待人工驗收及另行授權。
- 回復：以備份目錄回復 `release/test` API／Web，重新啟動 `SpcApiTest` 集區。

## 錄影續驗結果

- 來源：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/2026-09-30 13 59 20.mp4`，4.93秒、640×358。
- 逐格結果：管制圖初始化於約2.7～3.1秒顯示「無法載入檢驗項目基準及管制圖配置：未授權（401）」後才切換頁面。
- 根因：API攔截器排定登入導向後仍將401拒絕回傳頁面，頁面catch先渲染錯誤；短暫SSO競態也只取消導向，沒有重試原失敗請求。
- 修正：401導向期間由共用攔截器暫停錯誤傳遞；並行成功時重試原請求，重試仍401時直接導向且不顯示錯誤框。
- 進度表依停止例行更新規則不適用，本次未開啟或修改Excel。
