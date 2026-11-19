# SPC 入口網站按鍵開啟新分頁

- 工作代號：`SPC-PORTAL-NEW-TAB-1119-TASK-001`
- 優先級：P1
- 主系統：SPC Web
- 相依系統：Portal（僅既有入口網址）
- 資料庫／API：無異動
- 發布：測試站通過後自動發布；正式站另行授權
- 回復：還原 `App.vue` 的入口網站連結行為與前一版 Web 資產。

## Requirement

右上角「入口網站」按鍵不得取代目前 SPC 頁面；點擊後應在新的瀏覽器分頁開啟既有 Portal 首頁，讓使用者保留 SPC 畫面進行對照。

## BDD

- Given 使用者已登入 SPC，When 點擊「入口網站」，Then 瀏覽器以新分頁開啟既有 Portal 首頁，SPC 原分頁不導頁。
- Given Portal 網址來自測試或正式環境設定，When 產生連結，Then 沿用既有首頁網址正規化規則，不改登入、登出或 SSO 行為。
- Given 新分頁連結，Then 使用 `noopener noreferrer`，避免新頁控制原 SPC 分頁。

## 驗證範圍

- Happy Path：入口連結具正確 `href`、`target="_blank"` 與安全 `rel`。
- Boundary：測試區使用測試 Portal 網址；正式建置仍由正式環境設定決定。
- Invalid Input：沿用既有 URL 建構，不擴張環境設定處理。
- Regression：登出按鍵維持原導向；語系、主選單及 SSO 不變。
