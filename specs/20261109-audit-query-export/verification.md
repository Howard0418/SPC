# 驗證紀錄

狀態：測試階段 DONE。

## 結果

- API 提供預設 7 日／最多 31 日、時間／人員／頁面／事件／目標／結果篩選，依 `OccurredAtUtc, Id` 倒序的穩定游標分頁。
- 查閱與匯出分別要求資料庫現況明示 `admin.audit.read` 與 `admin.audit.export`；匯出權限仍依賴查閱權限，舊 JWT 不可繞過即時撤銷。
- 回傳與 Xlsx 排除 session／嘗試帳號／去重雜湊及 Details；公式型字串以文字儲存。查詢／匯出事件須先保存，保存失敗回 503 且不交付結果。
- 頁面 `/audit-events` 具六類篩選、下一頁與獨立匯出按鈕；Editor／Viewer 均只在具明示查閱權限時顯示入口及通過路由守衛。

## 驗證

- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore`：522 通過、11 個既有明示隔離測試略過、0 失敗。
- 本件權限／頁面靜態測試 2 通過；三語系 2,788 keys × 3 locales 及 `npm run build:test` 通過。
- 測試站首次發布因執行中 DLL 鎖定，原發布／回復均未完成；確認僅測試站後停止 `SpcSingleTest` pool，以既有備份保護重新部署並成功啟動。維護檔已移除。
- 測試站 API `0.1.110`／Web `0.1.121`；首頁、目前 JS、CSS、`/api/health`、`/api/version` 均 HTTP 200 且 MIME 正確，主 JS 含 `/audit-events`，匿名查詢端點回 401。
- 最終回歸補正 Viewer 具明示權限時的導覽入口後，只重發測試 Web；目前主資產為 `index-CRUzeajZ.js`，包含 `/audit-events` 與 `admin.audit.read`，API仍為0.1.110且health為ok。
- Chrome 既有分頁連接工具仍回 `timeoutMs`，無法取得登入後畫面；不冒充人工畫面驗收。登入畫面、遮罩截圖與全案 E2E 依 TASK-009 保留，不阻擋本件自動化可觀察行為完成。
- 原進度表已同步 11/09、原生甘特與小工作總表；核心未完成 43、SPC 相關 19、完成 195，人工 11／待核銷 4 另計。

## 回復

- 測試站備份：`release/test/backend.backup-audit-query-export-20261109`、`release/test/frontend.backup-audit-query-export-20261109`；最終Web補正前備份為`frontend.backup-audit-query-export-20261109-r2`。
- 回復前版 Web/API，保留既有 `AuditEvents`；本件無 schema migration。

正式站：未授權、不得發布或異動。
