# SPC 正式機發布

- 功能 ID：20261007-spc-production-publish
- 日期：2026-10-07
- 狀態：完成
- 涉及專案：SPC
- 授權依據：使用者要求「SPC要發佈，要更新正式機」

## 目的

將目前已完成並已在測試站驗證的 SPC 更新發布至正式機，並保留正式環境設定、備份、驗證與回復紀錄。

## 範圍

- SPC backend 正式發布備份目錄：`D:\SPC\publish\backend.backup-production-20261007-20261007-074921`
- SPC frontend 正式發布備份目錄：`D:\SPC\publish\frontend.backup-production-20261007-20261007-074921`
- 正式交付包來源：`D:\SPC\release\production\backend`、`D:\SPC\release\production\frontend`
- 正式資料庫目標：需由 production backend 設定確認為 `PMR_SPC_2026`
- 實際 IIS 正式站路徑：`SpcApi` → `D:\SPC\release\production\backend`；`SpcWeb` → `D:\SPC\release\production\frontend`

## 非範圍

- 不發布 Portal。
- 不發布或寫入 `D:\Sites\PmrPortal`。
- 不手動修改正式資料。
- 不執行未授權的資料修補或正式資料匯入。

## 需求與驗收

| 編號 | 需求 | 驗收 |
| --- | --- | --- |
| R-001 | 正式發布前需重建 production 交付包。 | AC-001：backend publish 與 frontend production build 通過，manifest 顯示 production 與 `PMR_SPC_2026`。 |
| R-002 | 發布前需備份現有正式 backend/frontend。 | AC-002：`D:\SPC\publish` 下有本次備份目錄，可描述 rollback。 |
| R-003 | 發布只限 SPC 正式目錄。 | AC-003：發布路徑不包含 `D:\Sites\PmrPortal` 或 Portal 目錄。 |
| R-004 | 發布後需 smoke test。 | AC-004：正式 frontend 首頁、主要 JS/CSS 與 backend `/api/version` 可回應。 |
| R-005 | 設定與機敏資料不得寫入回覆內容。 | AC-005：紀錄只寫目標環境與資料庫名稱，不揭露連線密碼或金鑰。 |

## 發布計畫

1. 重建 production release package。
2. 檢查 production manifest 與 frontend asset。
3. 備份 `D:\SPC\publish\backend`、`D:\SPC\publish\frontend`。
4. 以 `app_offline.htm` 暫停 backend 後複製 backend 交付包；保留正式設定策略以實際目錄與交付包核對。
5. 複製 frontend 交付包。
6. 執行正式網址 smoke test。
7. 更新 `CHANGELOG_CUSTOM.md`、需求索引與本規格驗證紀錄。

## 回復方式

若 smoke test 失敗，將 `D:\SPC\publish\backend` 與 `D:\SPC\publish\frontend` 還原至本次發布前備份，移除 `app_offline.htm` 後重新驗證正式網址。

IIS PhysicalPath 回復可使用 IIS 備份 `SPC-production-path-20261007-0756`，或將 `SpcApi`、`SpcWeb` 指回前一個確認可用的發布目錄後回收 App Pool。

## 驗證紀錄

- `scripts\publish-environments.ps1 -Configuration Release`：通過；production manifest 顯示 `AppEnvironment: production`、`Database: PMR_SPC_2026`、WebVersion `0.1.62`。
- production frontend 產出：`index-CTPUMGmw.js`、`index-B22nJqjf.css`。
- 發布前備份：`D:\SPC\publish\backend.backup-production-20261007-20261007-074921`、`D:\SPC\publish\frontend.backup-production-20261007-20261007-074921`。
- 發現並修正正式 IIS 路徑：原 `SpcApi`、`SpcWeb` 指向 `D:\SPC\release\test\...`，導致 `172.16.110.27:8081/api/version` 回 `environment=test`；已建立 IIS 備份 `SPC-production-path-20261007-0756`，並改指 `D:\SPC\release\production\...`。
- Smoke：`http://172.16.110.27:8081/api/version` 回 200，內容為 `environment=production`。
- Smoke：`http://172.16.110.27:8083/` 回 200/text-html，HTML 引用 `index-CTPUMGmw.js`。
- Smoke：`http://172.16.110.27:8083/assets/index-CTPUMGmw.js` 回 200/application-javascript。
- Smoke：`http://172.16.110.27:8083/assets/index-B22nJqjf.css` 回 200/text-css。
- 未發布 Portal，未寫入 `D:\Sites\PmrPortal`，未手動修改正式資料。
