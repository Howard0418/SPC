# 驗證紀錄

日期：2026-11-19

## 結果

- SPC 新增 `/chemical-adjustment-parameters`，編輯者選單新增「藥液量測調整公式」。
- 新頁保留固定格線、五欄貼上、預覽、批次儲存與正數／字數檢核。
- 儲存仍呼叫 Portal `/api/spc/chemical-adjustment-parameters/{id}`，沿用既有 SPC 寫入白名單與品保／製程角色 Email 通知。
- Portal 品保專區卡片改由 `/Spc/Launch?returnUrl=/chemical-adjustment-parameters` 開新分頁；舊 `/Spc/QualityAssurance` 回 302 導向相同 SSO launch。

## 自動驗證

- `node --test tests/chemical-adjustment-paste.test.mjs`：3 passed。
- `npm run build:test`：通過；i18n 2,865 keys × 3 locales 通過。
- `dotnet build src/PmrPortal.Web/PmrPortal.Web.csproj -c Release --no-restore`：0 warning／0 error。
- CORS：由 `https://spctest.pmr.com.tw` 對 Portal 測試 API 預檢回 204，允許指定 Origin、credentials 與 GET；未登入 GET 回 401 且仍含正確 CORS header。

## 測試站發布

- SPC Web：0.1.148 → 0.1.149；API 未修改，維持 0.1.142／test。
- Portal Web：1.0.319 → 1.0.320；API 未修改，`/health` 回 200 且 dbEnvironment=test。
- SPC 首頁、主 JS、主 CSS、`/api/health`、`/api/version` 均回 200；JS/CSS MIME 分別為 `application/javascript`／`text/css`。
- Portal `/app-version` 回 1.0.320.0；舊頁回 302 至新 SSO 目標。
- 正式站未發布。

## 未完成驗證

- Windows Computer Use native pipe 與 browser access host 均不可用，無法代為完成已登入瀏覽器的實際畫面驗收；保留待人工驗收。
- 中央進度表未覆蓋；使用者於2026-11-19明確指示後續停止例行更新，故不再列為本工作阻擋或待補項。

## 回復

- Portal Web 可由發布前備份目錄回復；SPC Web 可由前一版 0.1.148 原始碼重新建置回復。
- 資料庫未變更，無資料回復步驟。

## 2026-11-19 現況再驗證

- `node --test tests/chemical-adjustment-paste.test.mjs`：3/3通過；現行 `npm run build:test` 通過，i18n 2,878 keys × 3 locales通過。
- SPC測試站 `/chemical-adjustment-parameters` 與目前主 JavaScript 均HTTP 200，JS MIME為`application/javascript`；API health=ok、version=`0.1.155`、environment=test。
- Portal舊網址 `/Spc/QualityAssurance` 回302，Location仍為`/Spc/Launch?returnUrl=%2Fchemical-adjustment-parameters`，沒有恢復舊Portal維護頁。
- 本輪為現況驗證，沒有產品程式變更或重複發布；登入後實際畫面與儲存仍待人工驗收，正式未發布。

## 2026-11-19 人工驗收

- 使用者已確認 SPC 藥液量測調整公式新頁畫面，頁面移轉與顯示結果通過。
- 本次僅核銷測試區人工畫面驗收狀態，沒有程式、API、資料庫、設定或網站發布變更；正式站仍須另行授權。
- 原 Excel 進度表依既定規則不例行更新。
