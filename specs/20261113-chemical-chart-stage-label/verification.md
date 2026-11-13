# 驗證紀錄

- AC-001／AC-002：先以主檔只有 `controlScope=CHEM`、同日 `OPEN/OPEN` 與 `OPEN/CLOSE` 建立紅燈，實際舊結果為兩個 `11/09 早班`；修正後為 `11/09 早(開)`、`11/09 早(收)`。
- `node --test tests/chemical-single-line.test.mjs`：8/8 通過；同一 I-MR 序列仍保留兩點，MR 對齊及一般線別 GENERAL 回歸通過。
- `npm run build` 與 `npm run build:test`：i18n 2863×3 通過、Vite build 成功；只有既有 chunk 大小警告。
- 測試站發布：release `spc-chem-stage-label-20261113` 完成；SPC Web `0.1.129`，API維持`0.1.127/test`，migration 0。測試庫 `PMR_SPC_TEST` 備份已完成且 `VERIFYONLY` 通過。
- 發布權限：`SpcAuditApiTest003` 僅在發布期間暫時加入 `dbcreator`，未授予 `sysadmin`；finally 已撤除，最終狀態 `dbcreator=0／sysadmin=0／BACKUP DATABASE=1`。
- 實站連線：`https://SPCTEST.pmr.com.tw/` 200 `text/html`；主 JS `index-DOTcJAJQ.js` 200 `application/javascript` 且含 `0.1.129`／`早(收)`；CSS 200 `text/css`；同源 `/api/health`、`/api/version` 均 200、`0.1.127/test`。無 API 離線、環境錯置或 MIME 錯誤。
- 限制：原生瀏覽器操作管道與 browser host 均不可用，故未取得登入後管制圖畫面；使用者需重新整理測試站做人工視覺驗收。正式站未發布。
- 進度表：原檔備份為`進度表-藥液管制圖開收線前-20261113-1600.xlsx`；更新後 SHA-256 `51F76149B493E68DEE546238AC450EA47D5E00ACD324AB02EE1E3BCF0EDFEA0B`與候選一致，OpenXML 0錯誤、重複 Print_Area 0，渲染通過。新增待正式／人工驗收項後，核心未完成40件、SPC相關16件。

