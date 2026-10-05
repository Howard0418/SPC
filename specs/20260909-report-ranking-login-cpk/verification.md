# 驗證紀錄
- 功能 ID：20260909-report-ranking-login-cpk
- 規格版本：1
- 日期／環境：2026-09-09／測試環境
- 實作狀態：完成
- 驗證狀態：公式說明建置、診斷與 HTTP smoke 通過；AD/工號登入 UAT 待執行
- 發布狀態：已發布測試環境，正式環境不發布。

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 測試站週/月報表查詢，切換 OOS/Cpk 與前5/後3 | 顯示正確排行 | SPC Web 建置成功；`SpcSummaryTable.vue` 已含排行指標、最高5位/最低3位切換，登入後查詢操作待 UAT。 | 部分通過 |
| AC-002 | 測試站週/月報表查看公式說明 | 公式、sigma 與數值來源清楚 | 前端已顯示雙邊、只有 USL、只有 LSL 公式，並說明 sigmaWithin/sigmaOverall 與 Cpk/Ppk 對應；SPC Web 回應 200。 | 通過 |
| AC-003 | 以測試 AD 帳號/工號登入 Portal 並進入 SPC | 兩種識別皆可登入或明確拒絕原因 | Portal 工號比對已增加空白/連字號正規化；Portal/ SPC 建置成功。實際 AD/工號登入待測試帳號 UAT。 | 部分通過 |

## 已執行檢查
- SPC Web `npm run build:test`：成功。
- Portal API/Web `dotnet build --no-restore`：成功。
- HTTP smoke：SPC API health `200`、SPC Web `200`、Portal 藥液頁未登入 `302`；`SpcWeb`、`SpcApi`、`PmrPortalApi` IIS 狀態為 `Started`。
- Cpk 既有測試案例驗證 sigmaWithin、Cpk 與 Ppk 公式；完整測試專案因既存 `AuthControllerTests` 建構式錯誤未能執行。
- 測試發布備份識別碼：`20260909-133846`。
- 本次公式說明補強發布備份識別碼：`20260909-223136`；SPC Web App Pool `Started`。

## 已執行檢查
- SPC Web `npm run build:test`：成功。
- Portal API `dotnet build --no-restore`：成功。
- Portal Web `dotnet build --no-restore`：成功。
- Cpk 後端既有單元測試案例已存在，明確驗證 sigmaWithin、Cpk 與 Ppk 公式；完整測試專案仍受既有 `AuthControllerTests` 建構式錯誤影響，待後續修復測試基礎後重跑。
