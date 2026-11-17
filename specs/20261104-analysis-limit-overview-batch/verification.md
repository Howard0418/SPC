# 驗證紀錄

## 2026-11-17 畫面全寬子項

- 依使用者截圖讓表格填滿容器；僅增加w-full，保留1380px最小寬及手機橫向捲動。
- 工具3項、Playwright2項與test build通過；測試Web0.1.133／API0.1.132 test及首頁／主資產／同源API驗證通過。
- [子項規格與驗證](../20261117-analysis-limits-full-width/spec.md)。原進度表已同步；本項不代表整案AC-008全部完成，正式未更新。
- 後續使用者確認全寬子項並授權正式發布：僅將w-full加入原正式來源，正式Web0.1.134／API0.1.126 production；5項回歸、production build、備份雜湊、首頁／資產／實際8443 API及正式Origin的Chromium fetch通過。原進度表正式里程碑已同步，父件其餘AC-008不自動完成。

狀態：自動化與測試站發布完成；待實際瀏覽器畫面／截圖驗收；正式未授權。

## 自動化

- 後端完整回歸：82 通過、1 個既有明示隔離測試略過、0 失敗。
- 批次界限核心涵蓋格式、順序、空值／0、缺 Token、過期衝突、六欄限定、原子 rollback、版本與不可變歷程。
- 前端界限工具測試 3 通過；Playwright BDD 2 通過，涵蓋跨頁異動一次送出與 `95%` 格式拒絕。
- i18n：2,860 keys × 3 locales；測試模式 build 通過。

## 測試資料庫

- 目標只限 `PMR_SPC_TEST`；先建立 `PMR_SPC_TEST_analysis_limits_20261109.bak`，COPY_ONLY／CHECKSUM 與 RESTORE VERIFYONLY 通過。
- migration `20261109135930_AddLimitChangeHistory` 已套用；新增資料表 2、不可變 trigger 2，發布後批次／明細皆 0 筆，未異動任何既有界限。
- 一般測試帳號被正確禁止備份／建表；依既有授權改用受控維護身分且強制改接測試資料庫。正式資料庫未執行查詢或異動。

## 測試站發布與連線

- Web 首頁：HTTP 200 / `text/html`。
- 主 JavaScript：HTTP 200 / `application/javascript`；主 CSS：HTTP 200 / `text/css`。
- `/api/health` 與 `/api/version`：HTTP 200；版本 0.1.113、環境 `test`。

## 2026-11-17 正式發布

- 使用者已確認畫面並授權正式發布。
- 焦點測試 `LimitBatch*`：16/16 通過；API Release publish、Web production build、三語 2863 鍵檢查通過。
- 正式發布前：API 0.1.133／Web 0.1.139；發布後：API 0.1.134／Web 0.1.140，環境 `production`。
- 正式首頁 HTTP 200 `text/html`；主 JS `index-BlzugLq4.js` HTTP 200 `application/javascript`；主 CSS HTTP 200 `text/css`；JS 含 Web 0.1.140 及 `analysis-limit-overview` 路由。
- 正式 `/api/health` 與 `/api/version` 均成功，回 API 0.1.134／production。發布證據 phase=`complete`、status=`completed`。
- 正式資料庫已存在 `20261109135930_AddLimitChangeHistory`；第一次含 migration 的唯讀 preflight 因待執行清單不相符而安全停止，未寫入資料。第二個交付包不重複套用 migration，正式資料庫無結構或資料異動。
- 瀏覽器控制介面未連線，無法追加 UI 自動化截圖；使用者既有畫面確認與正式 HTTP／資產／API 實站驗證作為本次驗收證據。
- 瀏覽器 API 基底為同源 `/api`，非標準埠 `https://spctest.pmr.com.tw:8901/api/health` 亦為 HTTP 200。
- 匿名呼叫 preview 回 401，Editor 權限保護生效。
- 回復點：`release/test/frontend.backup-analysis-limits-20261109`、`release/test/backend.backup-analysis-limits-20261109` 及上述資料庫備份。

## 尚待驗收

- 瀏覽器操作工具回報 `browser access host is unavailable`，無法取得登入後總覽、預檢、成功／錯誤及版本歷史截圖。
- 此項為 AC-008 外部阻擋；未通過前不標 DONE，不發布正式站。
