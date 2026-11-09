# 驗證紀錄

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
- 瀏覽器 API 基底為同源 `/api`，非標準埠 `https://spctest.pmr.com.tw:8901/api/health` 亦為 HTTP 200。
- 匿名呼叫 preview 回 401，Editor 權限保護生效。
- 回復點：`release/test/frontend.backup-analysis-limits-20261109`、`release/test/backend.backup-analysis-limits-20261109` 及上述資料庫備份。

## 尚待驗收

- 瀏覽器操作工具回報 `browser access host is unavailable`，無法取得登入後總覽、預檢、成功／錯誤及版本歷史截圖。
- 此項為 AC-008 外部阻擋；未通過前不標 DONE，不發布正式站。
