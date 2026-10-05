# 驗證紀錄
待執行；先前資料同步與公式試算證據沿用，不重做。
## 2026-09-17 驗證與發布
- 測試先建：HistoryTests 舊程式3項失敗；實作後Chemical專案15項全過（含12項既有階段回歸）。SQLite記憶體資料隨fixture清理。
- Portal歷史載入4項測試先失敗再全過：1205筆跨3頁、逐頁日期傳遞、第二頁HTTP失敗、重複頁／總數變動、空集合。
- 既有chemical-stage-ui 3項通過，保留班別／階段與晚到回應規則。
- SPC API、Portal API/Web 的 dotnet publish -c Release --no-restore 全部成功。NuGet弱點查詢因網路限制出現NU1900，不影響編譯與測試；未宣稱弱點掃描通過。
- 備份識別 history-complete-20260917-114615；三個實際目的地均release/test，DLL與stage hash相同，appsettings*.json/web.config hash不變，offline已移除。無資料寫入／migration，正式站未發布。
- SPC /api/version=200,test；Portal /health=200,test,database ok；Login=200。
- 真實資料唯讀預期：N2共451筆，其中2026-08-03有20筆。新瀏覽頁因重啟停在登入，已請使用者登入；正式畫面筆數與舊日期驗收待完成，尚不標示端到端通過。
- SPC diff --check通過；Portal檔案既有未提交內容有尾端空白，保留未一併清理。本次新增區塊未引入尾端空白。

## 真人登入後畫面驗收（2026-09-17）
- 使用者重新登入後，沿用本人已登入瀏覽器至Portal測試藥液頁。全部製程顯示2424筆；N2顯示451筆／23頁。
- N2起迄日皆20260803，顯示20筆／1頁，資料日期2026/8/3，與先前唯讀SQL預期一致。原200筆以外舊資料可查詢，端到端驗收通過。
- 僅選製程與查詢日期，未新增、修改或刪除量測。沿用已通過測試與發布，不重跑或重新發布。
