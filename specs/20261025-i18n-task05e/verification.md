# 05E 驗證與發布
- 日期：2026-10-25；狀態：DONE（本件軟體與測試站驗證完成；譯文人工審核留 TASK-20）。
- 實作：兩個公開頁固定 UI／主檔補建確認及結果提示共 89 鍵／267 列 draft；累計 964 鍵三語。API enum、來源文字、payload 解析、錯誤與建議原文保持。只對這兩路徑沿用手機堆疊導覽，預覽 badge／按鈕換行。

## 實際驗證
| 項目 | 結果 |
|---|---|
| 翻譯前模板檢查 | 兩頁固定中文紅燈，翻譯後通過 |
| `npm run test:i18n` | 58 passed；包含兩頁模板／編譯、review hash／參數／protected token／完整來源覆蓋 |
| `npm run build:test` | 通過；964 鍵×3。既有 chunk 大小提示仍在，無新增套件 |
| 本機 Playwright uploads | 35 passed：兩頁×三語×四尺寸、DOM/query 保留、raw 純文字、0／空白／無效日期、補建取消／成功進度／失敗、製圖及手動設定 query、空資料、Viewer／舊別名 |
| 本機 shell 回歸 | 17 passed：共用導覽、偏好／角色隔離、草稿與 storage 失敗 |
| 已發布測試站 uploads＋shell | 35＋17＝52 passed；頁面無未處理 JS 例外 |
| 同源檢查 | `verify-built-api.mjs` 14 requests 全部同源 SPC 測試站 |
| 實際唯讀健康／版本 | `/api/version`、`/api/health` HTTP200，environment=test、API0.1.72、status=ok |
| 實際靜態檔案 | index、CSS、主 JS、guide JS 共4檔 SHA256 與 dist 相符 |
| 手機視覺 | 390 英文清單／預覽截圖檢視；操作可達，寬表內捲，主內容無水平溢出 |

## 發布與回復
- 測試站：<https://SPCTEST.pmr.com.tw>，Web0.1.79／API 沿用0.1.72。未更動 API／DB／Portal／TransFiles／正式站。
- 發布前 appcmd 確認 `SpcSingleTest/` 實際路徑 `D:\SPC\release\test\backend`，安全讀取 AppEnvironment=test。
- 前端備份49檔逐一比對雜湊；複製4個 payload，資產先、index 最後，保留舊資產與4份受保護設定；未重啟 pool。
- 備份：`D:\SPC\.codex-tmp\i18n-task05e\backup-frontend`；部署摘要／截圖等本機證據留該暫存目錄，不提交。
- 回復：先重核上述 test 站實際目錄與環境，再將備份中原有資產及 index 覆回 `D:\SPC\release\test\frontend`（資產先、index 最後）；不覆寫 web.config／appsettings，不刪其他檔案。重查版本／健康與原 index 雜湊。此回復方案未實際觸發。

## 限制與停止點
- 瀏覽器業務 API（含 POST 主檔補建）全 mock；真實環境僅匿名版本／健康／靜態唯讀。不冒稱真 DB／SSO／實際補建或真人翻譯審核。
- 譯文仍 draft／uncertain；原始資料不翻譯，未接線舊上傳元件不重新接線。未恢復網頁上傳或確認匯入。
- BDD 五項分別對應上述瀏覽器案例；Happy Path／Boundary／Invalid Input／Regression Risk 均已覆蓋。
- 剩 TASK-06～21 共16件；另受阻1、暫緩2。STOP 於05E，不自動開始06。
