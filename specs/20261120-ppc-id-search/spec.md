# SPC-PPC-ID-SEARCH-1120-TASK-001

- 規格版本：1
- 狀態：完成（2026-11-20）
- 主系統：SPC Web
- 資料庫影響：無
- 發布影響：需遞增Web版本並發布SPC測試站；正式站另行授權

## 問題與目標

「SPC 管制項目設定與規格維護」現行搜尋只比對管制類型、料號、製程、機台／槽位與檢驗特性，未比對PPC ID，因此輸入`2587`無法找到PPC 2587。本件只擴充現有前端篩選，不改API、權限或資料。

## 需求與BDD

- R-001：搜尋欄必須能以PPC ID的十進位文字搜尋。
- R-002：既有料號、製程、機台／槽位與特性搜尋行為保持。
- R-003：搜尋提示與說明須以繁中、簡中、英文明示可搜尋PPC ID。

- Given 清單含PPC ID 2587，When 使用者在搜尋欄輸入`2587`，Then 只顯示符合現行狀態／製程／範圍／槽位篩選且ID命中的列。
- Given 搜尋欄輸入既有文字欄位，When 篩選清單，Then 結果與修正前一致。
- Given 使用者切換三語系，When 查看搜尋說明與placeholder，Then 均可辨識PPC ID為可搜尋欄位。

## 驗收與測試

- AC-001（Happy Path）：Playwright fixture加入ID 2587，輸入`2587`後只剩該列。
- AC-002（Boundary）：純數字ID搜尋不影響其他篩選條件與排序。
- AC-003（Invalid Input）：無命中ID時沿用現行空結果，不發送額外API請求。
- AC-004（Regression）：現有PPC前端聚焦測試、三語i18n檢查與test build通過。
- AC-005（測試站）：首頁、主JS／CSS、API health／version及瀏覽器實際使用的同源API路徑驗證通過。

## 實作、回復與非範圍

- 只修改`PartProcessCharacteristicsView.vue`的computed篩選、搜尋文案、前端測試及Web版本。
- 不改後端API、PPC資料、公式、F表、權限或schema。
- 回復方式：以測試站發布前Web完整備份還原，再重驗首頁、資產與同源API。

## 驗證紀錄

- 紅燈：fixture已存在PPC 2587，輸入`2587`後`ppc-edit`為0列，證明現行篩選沒有比對ID。
- 綠燈：將`row.id`轉為文字納入現有computed篩選；輸入`2587`後精確只剩一列，且沒有額外API請求。搜尋字串同時`trim`，原有文字比對與組合篩選保持。
- 聚焦回歸：15項通過，涵蓋ID搜尋、空結果、圖表連結、三語及1920／1366／1280／390寬度。
- i18n／build：2895鍵×3語系一致；`npm run build:test`通過，僅有既有Browserslist與大chunk提示。
- 擴大PPC測試套件49項中39通過、10失敗；失敗為3項PROCESS樣本數舊預期5／實際1，以及7項現行排程頁籤不可用導致timeout，與本次ID篩選、文案及版本檔無交集，未跨範圍修正或冒稱全綠。
- 版本：Web `0.1.172`→`0.1.173`；API未改，維持`0.1.161/test`。無schema、PPC、公式、F表、權限或資料寫入。
- 測試站發布：IIS`SpcSingleTest`實際目標`D:\SPC\release\test\frontend`；發布前完整備份`D:\SPC\release\backups\web-0.1.172-before-ppc-id-search-20261120-163812`。
- 發布後驗證：首頁、主JS`index-DEMqJ0nr.js`、CSS`index-pm66YOio.css`均HTTP 200且MIME正確；同源及`:8901` API均為`0.1.161/test`，Chromium直接走`https://spctest.pmr.com.tw/api/version`回200。Playwright再以測試站已發布資產執行ID 2587測試，1項通過且無額外API請求；無API離線或環境錯置。
- 回復未執行；若需回復，停止`SpcSingleTest`後以上述備份還原，再重驗版本、首頁、資產與API。正式站未授權、未發布。
