# 驗證紀錄
- 功能 ID：SPC-OPERATOR-PERMISSION-SESSION-REFRESH-1126
- 規格版本：1
- 日期／環境：2026-11-26／本機、SPC 測試站
- 實作狀態：完成
- 驗證狀態：自動化與實站連線驗證通過
- 發布狀態：SPC 測試站完成。Web `0.1.200 → 0.1.201`，API `0.1.178 → 0.1.179/test`。正式尚未發布。

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | `CurrentOperatorSessionTests` | 同一有效身分取得資料庫最新角色／授權 | SQLite 驗證更新及撤回，保留 `jti` 與到期時間 | 通過 |
| AC-002 | `playwright.permissions.config.ts` | 本人儲存後更新選單 | 模擬帳號勾選落塵輸入後，儲存成功即顯示入口，原網址與 Bearer 保留 | 通過 |
| AC-003 | 同上 | 切頁先同步再判斷權限 | 切頁取得新增權限、重整套用撤權、無可用頁面後可重整恢復 | 通過 |
| AC-004 | API 身分與角色測試、測試站 Chromium | 拒絕無效身分與已降級寫入 | 匿名 session HTTP 401。SQLite 五種異常身分拒絕，降為 Viewer 的舊憑證身分套用後寫入 HTTP 403 | 通過 |
| AC-005 | 現有登入與安全稽核回歸 | 明示授權及禁止替自己新增稽核權限 | 54 項通過，隔離 SQL 稽核測試未設定而略過 | 通過 |

## 建置與實站驗證

- API Release 全組 184 項通過，1 項開發用 Excel 檢查依設計略過。加入 Viewer 寫入檢查後，6 項聚焦測試再度通過。Node 設定／導覽 9 項、Chromium 導覽／權限 5 項、登入／SSO 3 項通過。三語字典 2,956 鍵與 `build:test` 通過。
- 發布前原 API `0.1.178` 曾發生 IIS HTTP 500.30，事件紀錄顯示啟動稽核資料庫連線錯誤。唯讀確認測試庫可連，回收 `SpcApiTest` 集區並確認同源及直連 health 均恢復後才開始部署。
- 測試首頁、`index-CYDK5Jcd.js`、`index-C6_AqEXo.css` 均回 200，MIME 分別為 HTML、JavaScript、CSS。測試同源與 `:8901` 的 `/api/health`、`/api/version` 回 `0.1.179/test` 且 health `ok`。Chromium 使用瀏覽器同源 API 複核，確認匿名 session 回 401。以模擬登入資料打開實站頁面，確認前台顯示 Web `v0.1.201`、API `v0.1.179`，版本 API 為實站回應。
- 備份位置 `D:\SPC\release\backups\spc-permission-session-1126-20261126-111831`。可還原 API 組件與 Web 首頁索引至原版本。部署核對測試設定雜湊，未寫入實站帳號或變更 schema。

## 未執行項目、限制及後續

- 正式發布未授權。
- 實站使用者操作以受控瀏覽器模擬帳號驗證，未使用實際帳號變更權限。

## 基準與變更紀錄更新位置

- 完成後同步 `docs/requirements.md`、`TODO.md`、`ai_docs/10_change_log.md`、`CHANGELOG_CUSTOM.md`。
