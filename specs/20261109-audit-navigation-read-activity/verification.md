# 驗證紀錄

功能：`SPC-AUDIT-1103-TASK-006`；日期：2026-11-09。

| 驗收 | 驗證方式 | 結果 |
|---|---|---|
| AC-001 | Router 成功、alias、dynamic route、登入／unknown route 行為測試 | 單元／build通過；登入後實際進頁事件 97、104 通過 |
| AC-002 | API read/filter/export 安全摘要與敏感值負面測試 | 後端測試及實際查詢／篩選／Xlsx匯出事件 95～106 通過 |
| AC-003 | 未知 activity／PageKey、偽造欄位、未驗證 actor 測試 | 未知 activity／PageKey 單元測試及測試站匿名 POST 401 通過 |
| AC-004 | navigation ClientEventId 重送與 read 事件限制測試 | 同 session 重送單元測試通過 |
| AC-005 | endpoint 失敗時 router／GET／下載不受阻與一次記憶體重試 | 程式碼檢核／前端建置通過；實際操作完成且事件成功回報 |

## 已完成驗證

- `NavigationReadAuditTests` 4 項通過；加上既有 `AuditEventWriterTests` 共 23 項通過。
- `npm run build:test` 通過，i18n 2770 keys × 3 locales 通過；僅既有 bundle size 與 Browserslist 資料提示。
- 已發布 SPC 測試站：API／Web backup 為 `release/test/backend.backup-audit-navigation-read-20261109`、`frontend.backup-audit-navigation-read-20261109`；測試設定已明示 `Audit:NavigationReadEnabled=true`，環境仍為 `test`／`PMR_SPC_TEST`。
- `https://spctest.pmr.com.tw/`、目前 JS、CSS 均 HTTP 200 且 MIME 正確；`/api/health`、`/api/version` 均 200、環境 `test`、版本 `0.1.109`。目前 JS 含 `/v1/audit/activity`；匿名 POST 該 endpoint 回 401，沒有寫入事件。
- 使用者於測試站完成實際操作後，`PMR_SPC_TEST.dbo.AuditEvents` 產生事件 95～106：進頁 2、一般查詢 2、篩選 7、Xlsx 匯出 1。12筆皆為 Operator 27／`ihao_ting`／丁義豪、Editor、`Test`、`analysis.spc.overall`、`/spc`、HTTP 204／Succeeded，具 session hash 與 correlation。
- 實際篩選只保存欄位名稱、UTC日期範圍與結果筆數；匯出只保存 `Xlsx`、日期與筆數。允許清單外欄位 0、敏感內容洩漏 0、重複 ClientEventId 0；驗收腳本只接受 `PMR_SPC_TEST`。

## 驗收結論

- TASK-006 測試階段 DONE。使用者實際操作與資料庫事件鏈已完成端到端驗收；未取得畫面截圖，截圖遮罩與整案E2E仍依原排序留在 TASK-009，不影響本件可觀察資料結果。

發布狀態：測試站已發布；正式站未授權且未更新。
