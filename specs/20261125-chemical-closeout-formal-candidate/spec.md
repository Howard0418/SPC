# 2026-11-25 藥液量測四項正式候選

以 SPC 正式建置 d42466c9、Portal 正式建置 01cfa9a 為基準，在獨立本機 worktree 整合複驗第二值留白、載入初驗調整、移除錄入比對工具及採 SPC 現行調整公式。Portal 錄入頁所引用的滴定值貼上腳本一併納入。刷新入口另行驗收。

SPC Web／API 與 Portal Web／API 建置成功。SPC 焦點 15 項、Portal 滴定值貼上 3 項通過。Portal Chromium 24/25 通過，歷史查詢欄數檢查在正式基準與候選皆為預期 14、實際 15，尚待核對測試契約。

尚未遞增發布版號或部署。實際發布須依共用版本規則逐環境核對，並保留現行正式版本的回復點。

## 重驗結果

- 正式基準與候選的歷史查詢均有「操作」欄。依既有畫面契約修正候選測試，螢幕 15 欄、列印 17 欄，兩個調整欄仍依媒體切換。候選 Portal 28/28、SPC 15/15 通過。
- 本機產物已建置。SPC Web 0.1.187／API 0.1.171，Portal Web 1.0.364／API 1.0.325。已核對編譯組件版本，未更新正式或測試站。

## 正式發布及回復紀錄

- SPC ac5ebcf5／SPC-PROD-CHEM-CLOSEOUT-1125-W187：正式主機及資料庫唯讀預檢通過，Apply 在 backup-database 階段中止，狀態 failed-before-apply。實際正式應用 SQL 登入查回 BACKUP DATABASE 權限為 0；未套用網站檔案。保留 Web 0.1.154／API 0.1.141，首頁資產與直連 API production 健康通過。
- Portal a83fb61／PORTAL-PROD-CHEM-CLOSEOUT-1125-W364：正式發布曾完成 Web 1.0.364／API 1.0.325。因 SPC 仍為原版，立即以同發布的回復點還原。確認 Web 1.0.293／API 1.0.268，登入頁主要 JS／CSS 與直連 health production 通過。無 schema migration。
- 本批未完成同步正式發布。重試需以獨立 SQL 維護身分交接新校驗備份，使用新發布識別，並各站實測發布版本。
- SPC 正式且無 migration 的重試允許明示當次維護備份 UUID。發布在停站前以固定維護設定重新查備份來源、檔名、完整備份型別、校驗及新鮮度，並再次執行 VERIFYONLY。驗證失敗即中止。

## 2026-11-25 正式重試

- SPC Web 0.1.154 → 0.1.187／API 0.1.141 → 0.1.171；Portal Web 1.0.293 → 1.0.364／API 1.0.268 → 1.0.325。兩筆 R2 均通過發布程序。SPC 同批維護備份 UUID 及來源核對、VERIFYONLY 通過，兩站皆無 migration。回復點為各 R2 發布資料夾。
- 兩站 Web 入口及主要 JavaScript／CSS 200、MIME 正確。SPC 實際 JS 的 8443 API base、CORS、health/version 與 Chrome 可見雙版本均通過。Portal app-version、直連 health production、API 組件版本、同源保護路由 401 及登入後藥液頁成功。
- Chrome 藥液錄入已核對無量測時間及跨日試算，日期選擇仍可用。正式 11/23 C3 早班顯示無既有日報，該正式個案的日報儲存與重載待找具既有資料的案例。C5 11/23 新預覽為 74.072、稀釋各 209 L，未送出。正式主檔調整量公式使用 Target，已排程第 12 版使用 100，兩者不同；程式目前以正式現行主檔為準，若需各 −385 L 則須另行確認變更正式主檔。未寫入業務資料。
