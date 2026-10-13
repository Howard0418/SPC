# SPC/Portal/KM Web API 單一網址設定

- 日期：2026-10-13
- 主系統：SPC
- 相依系統：PmrPortal、KM
- 類型：跨系統設定與發布規則
- 狀態：執行中

## 目的

使用者要求 SPC、入口網站與 KM 都採同一個對外網址承接 Web 與 API。對使用者、瀏覽器與跨系統 SSO 設定，只能使用站台根網址；API 一律掛在同站 `/api/...`，不得再對外宣告另一組 API URL。

## 現況

- SPC 測試站已由 `SpcSingleTest` 同站承接 Web 與 `/api`，對外入口為 `https://SPCTEST.pmr.com.tw/`。
- Portal 測試站由 `PmrPortalWeb` 對外承接 Web 與 `/api/{**path}` proxy，內部 upstream 為 `PmrPortalApi` `http://172.16.110.27:8091/`。
- KM 測試站為單一 ASP.NET Core 站台 `KM-Test`，目前沒有獨立 API site，但 KM 依賴 Portal 時仍可能設定到 Portal 內部 API 位址。
- Portal API 先前 `SPC_API_BASE` 設到 `.../api` 時，因程式再接 `api/v1/...`，產生 `/api/api/...` 並造成 SSO 405。

## 範圍

- 建立三系統單一對外網址基準。
- 修正測試站與原始設定，使跨系統設定使用站台根網址。
- 保留必要內部 upstream 設定，但不得作為對外 API 網址給使用者或其他系統使用。
- 增加 `/api` 尾段防呆，避免 BaseUrl 重複組成 `/api/api`。

## 非範圍

- 不操作正式站。
- 不修改資料庫 schema。
- 不把 Portal API DLL 混入 Portal Web 實體目錄；Portal Web 仍以 proxy 轉送 `/api`。
- 不新增 KM DNS 名稱；若需 `kmtest.pmr.com.tw`，需先完成 DNS 指向後再加 IIS hostname binding。

## 統一網址基準

| 系統 | 對外 Web URL | 對外 API URL | 內部 upstream |
| --- | --- | --- | --- |
| SPC 測試站 | `https://SPCTEST.pmr.com.tw/` | `https://SPCTEST.pmr.com.tw/api/...` | 同站 |
| Portal 測試站 | `https://portaltest.pmr.com.tw/` | `https://portaltest.pmr.com.tw/api/...` | `http://172.16.110.27:8091/`，僅供 Portal Web proxy |
| KM 測試站 | `http://172.16.110.27:8086/` | `http://172.16.110.27:8086/api/...`（若未來新增 API） | 同站 |

## 需求

- R-001：對外說明與跨系統 WebBaseUrl 必須使用站台根網址，不包含 `/api`。
- R-002：前端瀏覽器呼叫 API 時必須使用同源 `/api/...`。
- R-003：Portal Web 可保留 `PortalApi:BaseUrl` 作為內部 upstream，但該設定不得被視為對外 API 網址。
- R-004：KM 呼叫 Portal 驗證與人員 API 時，應可使用 Portal 單一入口 `https://portaltest.pmr.com.tw/`，由 Portal Web proxy 轉送至 Portal API。
- R-005：BaseUrl 若誤填到 `/api`，程式需正規化成站台根網址，避免 `/api/api`。

## BDD 驗收

### Scenario: Portal 取得 SPC token
Given Portal 設定的 SPC base URL 是 `https://SPCTEST.pmr.com.tw`  
When Portal 交換 SPC SSO token  
Then 目標 endpoint 必須是 `https://SPCTEST.pmr.com.tw/api/v1/auth/portal-sso`

### Scenario: KM 透過 Portal 單一入口驗證
Given KM 設定 `PortalApi:BaseUrl=https://portaltest.pmr.com.tw/`  
When KM 呼叫 `api/auth/me` 或 `api/auth/login`  
Then 請求必須送往 `https://portaltest.pmr.com.tw/api/...`，不得使用 `http://172.16.110.27:8091/` 作為對外設定

### Scenario: Portal Web 同站 API
Given 使用者開啟 `https://portaltest.pmr.com.tw/`  
When 瀏覽器呼叫 `/api/auth/me`  
Then 請求留在 `portaltest.pmr.com.tw` 同源，由 Web proxy 轉送內部 API

## 驗證計畫

- 檢查設定檔不再把 SPC/Portal 對外 base URL 設為 `/api` 尾段。
- 建置 Portal API/Web 與 KM。
- 測試站 smoke：SPC `/api/health`、Portal `/api/auth/me` 未登入 401、KM 首頁或登入頁可回應。
- 正式站未授權，不發布。
