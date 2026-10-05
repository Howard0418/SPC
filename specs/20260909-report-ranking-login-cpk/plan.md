# 技術計畫
- 功能 ID：20260909-report-ranking-login-cpk
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- SPC Web：`SpcChartView.vue`／`SpcSummaryTable.vue` 增加週月報表排行與 Cpk 公式說明。
- SPC API：維持 summary 契約與既有 capability 計算；必要時補充可呈現的統計欄位，不改公式。
- Portal API：`AuthController`、`SpcSsoTokenService` 強化 AD/工號識別正規化與 SSO 相容。

## 實作方式
- R-001：在 `SpcSummaryTable` 以目前 `data` computed 產生 OOS/Cpk 排行，提供指標與前5/後3選項；只在週月報表顯示。
- R-002：在週月報表總表上方加入 Cpk/Ppk 公式說明，依單邊/雙邊規格呈現；補充 sigmaWithin 與 sigmaOverall 定義。
- R-003：統一 Portal 登入帳號與 SSO 欄位的 domain、email、大小寫、空白正規化；不繞過 AD 驗證、不改密碼流程。

## 相容性與風險
- 排行不變更 API 與資料庫，既有總表排序保留。
- Cpk 公式說明明確區分畫面 Cpk 標籤與實際 Ppk 數值。
- 登入失敗仍返回既有錯誤及稽核，避免將模糊帳號自動合併。

## 驗證與發布
- SPC Web `npm run build:test`；Portal API/Web `dotnet build`。
- 使用既有前端/後端登入測試與測試站 HTTP smoke；實際 AD/工號登入需使用測試帳號 UAT。
- 只發布 `SpcWeb`、`SpcApi`、`PmrPortalApi`、`PmrPortalWeb` 測試站，發布前備份，正式不發布。
