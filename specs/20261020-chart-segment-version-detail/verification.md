# 驗證紀錄
- 日期：2026-10-20；本次只驗收 spec v2 的 AC-D01～06。
- 自動化、build及測試站發布：完成；以下區分fixture與實際站台。
- Antigravity人工操作：待使用者執行，不以mock測試代替真實人工驗收。
- 正式站：未發布。DB：未修改、無migration。

## 驗收與證據
- AC-D01／D06：服務保留精確起迄、欄位回退；日期按鈕與真實ECharts線段點擊通過。區間外無量測不誤畫到首尾點，沒有推算公式生效日。
- AC-D02：PPC隔離公式歷史、目前公式與原因展開、台北修改時間及Unknown狀態測試通過；線上2425主檔GET200含7筆公式歷史。
- AC-D03：非藥液、空歷史／分段、清單／明細重試、390px通過。
- AC-D04：分段慢請求競態與歷史分頁／換段重置通過；換PPC清除依watch及卸載保護，未真人演練跨項目競態。
- AC-D05：單元測試涵蓋錯誤分頁、其他PPC分段、停用項目、無寫入及Authorize；線上400／404／匿名401通過。
- Regression：I-MR、Xbar-R/S、P/U、原點位查閱及藥液TrendChartView共用日期入口通過；不修改統計／公式運算。
- 先加入測試，首次因服務未建立編譯失敗；實作後查閱／日期／歷史17 passed，擴大相關回歸41 passed。
- 新增瀏覽器10例整批通過，再補藥液趨勢圖第11例獨立通過；既有歷史頁9 passed。均為本機攔截API，不是真人SSO驗收。
- backend Release build／publish及frontend build:test成功；保留既有NU1902／NU1903及Vite大chunk警告，未改套件。
- Antigravity：[antigravity-tests.md](antigravity-tests.md)，AG-01～10待使用者操作。

## 發布與唯讀核對
- IIS已確認SpcSingleTest的443／8901 bindings及D:/SPC/release/test/backend；僅更新API0.1.63／Web0.1.67，未用同時發布production的腳本。
- 備份：D:/SPC/release/backup-chart-segment-20261020-091930。appsettings.json／web.config hash不變，app_offline.htm已移除；可還原同批程式，無資料回復需求。
- 最終線上index-B5cegE0E.js SHA256與dist一致，API DLL與staging一致；HTTPS health/version確認ok/test/0.1.63。
- Viewer短效診斷權杖GET新API200，負數400、未知分段404、匿名401；權杖未保存／輸出，不代替真人SSO。
- 實際served bundle同源檢查14請求通過（該檢查攔截API，與前述真正HTTP查詢分開）。
- 依檔名初查有2個舊檔差異；改核對DbContext／Migration識別後，實際註冊52個全已套用、pending0。不修改不相關舊檔、不新增或套用migration。
- 業務筆數前後一致：公式歷史7、規格歷史0、VariableMeasurements13739；分段0，發布後migration歷史57筆。未新增測試業務資料。
- 真實分段／跨界／分頁因測試資料不足，不能記為線上通過；離線fixture已覆蓋。正式站、Portal、TransFiles及KM未操作。
