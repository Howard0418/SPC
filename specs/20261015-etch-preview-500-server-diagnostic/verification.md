# 驗證紀錄
- 功能 ID：SPC-ETCH-PREVIEW-500-SERVER-DIAGNOSTIC-20261015
- 規格版本：3
- 日期／環境：2026-10-15／本機與 SPC 測試站
- 實作狀態：已發布，2026-10-19 人工預覽與製圖驗證完成
- 驗證狀態：既有自動化通過；人工預覽、使用者確認匯入與新增日期製圖通過
- 發布狀態：SPC 測試站 backend 已發布；正式站未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | 相關單元測試與測試站未授權 preview | 資料庫例外為安全 503；未授權請求不得 500 | 26 passed；`POST /api/v1/etch-import-test/preview` 未帶權杖回 401。 | 通過 |
| AC-002 | 同上 | 未預期例外為安全 500 回應 | 26 passed，含 `EtchPreviewFailureResponseTests.UnexpectedException_ReturnsSafe500WithTraceId`。 | 通過 |
| AC-003 | 同上 | 預覽不建立資料 | 26 passed，既有 `EtchTests` 預覽案例通過。 | 通過 |

## 未執行項目、限制及後續
- 後端 build：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` 通過，0 warnings / 0 errors。
- 發布：2026-10-15 已發布到 `SpcSingleTest`，遠端備份為 `D:\SPC\release\test\backend.backup-etch-preview-server-diagnostic-20261015-104005`；保留 `appsettings.json`、`web.config`、`private-data`。`/api/health` 回 `ok`、`/api/version` 環境為 `test`。
- 待使用者使用轉檔工具對單筆日報執行預覽，回傳錯誤代碼與 Trace ID 後再定位實際資料庫根因；不得按確認匯入。
- 預覽鎖定修正：`EtchReportSyncService` 只在確認匯入建立序列化交易及 `sp_getapplock`；預覽維持讀取既有資料與主檔，但不取得寫入鎖。`Etch` 相關測試 24 passed、backend build 0 warnings / 0 errors；2026-10-15 已發布 SPC 測試 backend，備份 `D:\SPC\release\test\backend.backup-etch-preview-readonly-20261015-112638`，health 回 `ok`／`test`。待使用者以日報 25/50 點模式預覽確認。
- 授權方案修正：IIS 事件紀錄的 `No authenticationScheme was specified` 已定位為實際根因。JWT 金鑰存在時一律註冊 JWT 驗證；`Auth:Enabled` 只控制全站預設授權。`AuthenticationStartupPlannerTests` 與 `Etch` 測試共 26 passed，backend build 0 warnings / 0 errors。2026-10-15 已發布 SPC 測試 backend，遠端備份 `D:\SPC\release\test\backend.backup-20261015-etch-preview-auth-scheme`；health 為 `ok/test`，不帶權杖的 preview 回 401，未寫入資料。待具權限 TransFiles 預覽確認。

## 基準與變更紀錄更新位置
- `docs/requirements.md`
- `CHANGELOG_CUSTOM.md`
- `ai_docs/10_change_log.md`

## 2026-10-19 接續人工驗證（T-004，尚未完成）
- 使用者已登入 Portal 測試站；從品保專區的 SPC 入口完成 SSO，目的地主機為 `spctest.pmr.com.tw`，頁尾為測試環境，角色為編輯者。
- Given 已登入 Portal 測試站，When 從品保專區開啟 SPC，Then SPC 測試站免再次輸入密碼並顯示已登入編輯者：通過。
- Given SPC 測試站既有 PT1 資料，When 查詢 2026-07-19～2026-10-19 總表並點選 A side 平均咬蝕量製圖，Then 顯示 22 個管制點與 Xbar/S 兩張曲線：通過，瀏覽器畫面及圖表已確認。
- 此結果只驗證既有資料讀取及製圖，不能當成新日報 25/50 點預覽或確認匯入驗收。
- Happy Path：SSO、既有總表、既有 Xbar-S 圖已驗證；25/50 點代表日報預覽待 TransFiles 實際結果。
- Boundary Case：25/50 點切換尚待驗證；Invalid Input：本次未執行，沿用既有自動化證據；Regression Risk：既有 PT1 總表及製圖可用，但不代表全線別回歸通過。
- 瀏覽器與 TransFiles 登入工作階段分開；目前可用自動操作介面只支援瀏覽器，無法直接操作 TransFiles 桌面程式。已請使用者從 TransFiles 執行代表日報預覽並提供結果；不要求提供密碼，不擷取登入憑證。
- 本次未呼叫 confirm、未寫入資料、未修改程式或重新發布；T-004 保持未勾選，兩件工作皆未標示 DONE。

## 2026-10-19 最終驗證（取代上方待辦狀態）
- 使用者提供 TransFiles 預覽畫面：兩站預覽 130/130；新增/需補同步 5、完全相同 125、衝突 0、隔離 12 不匯入，HTTP 500 未再發生。
- 使用者另行確認測試庫匯入，完成畫面為成功 130、衝突略過 0、待重試 0；預覽與確認寫入為不同步驟。
- 唯讀核對使用者本機結果檔 `咬蝕匯入結果_c47f13ccf0c8.json`：130 筆均為 SUCCESS；125 筆 SPC unchanged=true、5 筆 false；pointCount=51 共 64 份、101 共 66 份。原始 JSON 不提交。
- 5 份新增/補同步：2026-10-16 PT1/PT2/QE1/QE2，2026-10-17 QE2。
- 瀏覽器 `spctest.pmr.com.tw` 測試環境查詢 2026-10-16～2026-10-17 All 總表，四線共 12 列：QE2 A/B/ER 各 2，其餘 PT1/PT2/QE1 A/B/ER 各 1；與上述日報日期線別一致。
- PT1 A side（25 點模式）及 QE2 A side（50 點模式）皆成功載入 XBAR_S，分別呈現 1 與 2 個日報子組；不將子組數混稱為原始 25/50 點數。QE2 顯示 2 個既有規則 OOC 提示，未修改或處置資料。
- Happy Path：兩站預覽/確認/新日期圖表通過；Boundary：25/50 點模式與 125 份 unchanged 結果通過；Invalid Input：沿用 2026-10-15 26 passed 的自動化證據，本次不刻意製造站台錯誤；Regression：SSO、PT1 既有製圖及新增日期四線總表可用。
- T-004 完成。Portal 錯誤代碼/Trace ID 的 TransFiles 錯誤路徑人工驗收仍屬另一件小工作，不以本次成功路徑宣稱通過。
- 本次未變更應用程式，build/test 與重新發布不適用；沿用 2026-10-15 測試站發布。正式站未操作。程式回復可還原既有 backend 備份；資料回復須另立具批次備份及授權的工作，不自動刪除本次資料。
