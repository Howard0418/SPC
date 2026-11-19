# 團保名冊複製貼上匯入

## 目的

以 Excel 九欄資料直接貼上取代團保名冊 `.xlsx` 上傳，保留預檢、錯誤明細、確認匯入、批次歷程及既有資料驗證。

## 範圍

- 主系統：Portal；資料庫 schema 不變；SPC、TransFiles 不變。
- 移除團保資料管理頁面的 Excel 檔案選擇器。
- 支援含標題列或純資料列的 Tab／換行貼上。
- 預檢與確認匯入使用 JSON，沿用九欄順序、必填、關係、日期、重複、同員工一致及正式環境 TEST 識別碼規則。
- 確認匯入後保留貼上內容，供使用者核對或修正。
- 2026-11-19 畫面修訂：取消大文字貼上區，改為固定九欄格線；點任一格後可從 Excel 貼上矩形資料，超過既有列數時自動增列。

## BDD 驗收

- Given 使用者貼上 Excel 九欄資料，When 按預檢，Then 顯示有效筆數或逐列錯誤。
- Given 貼上資料含正確標題列，When 預檢，Then 標題列不計入資料筆數。
- Given 預檢通過，When 確認匯入，Then 建立既有團保匯入批次與名冊資料，來源顯示為複製貼上。
- Given 匯入完成或失敗，Then 貼上內容不得清空。
- Given 使用者進入頁面，Then 不再看到 `.xlsx` 檔案選擇器。
- Given 使用者點選格線儲存格，When 從 Excel 貼上多列九欄，Then 資料依起始格與列欄位置對齊，超出列數自動增列。

## 非範圍

- 不修改既有歷史批次或名冊資料。
- 不修改員工查詢、團保申請、郵件或角色權限。
- 正式站發布仍須另行授權。

## 驗證

- Happy Path：含／不含標題貼上、預檢、確認匯入。
- Boundary：空白列、民國／西元日期、Tab 欄位內前後空白。
- Invalid：欄數錯誤、必填空白、關係錯誤、日期錯誤、重複資料。
- Regression：團保 API／Web 測試、Release build、測試站連線與版本。

## 驗證結果

- 紅燈：Web 契約測試確認原頁面缺少貼上控制與 JSON 傳送。
- 綠燈：團保 API 63項、Portal Web 52項通過；Release build 0警告／0錯誤。
- 測試發布：Portal API 1.0.276／Web 1.0.314；首頁與實際 CSS／JavaScript 200 且 MIME 正確，Web 版本正確、API health 為 test／database ok，同源匿名及 JSON 貼上端點未登入皆為 401。
- 正式區未授權、未發布；登入後實際貼上及確認匯入待人工驗收。
- 2026-11-19 格線修訂：Web 52項、團保 API 63項、JavaScript 語法及 Release build 通過；測試區 API 1.0.277／Web 1.0.315，首頁、CSS／JavaScript、health、同源 API 與 test 標籤驗證通過。待人工確認格線貼上，正式未發布。
- 2026-11-19 正式發布：使用者確認驗收並授權。正式現行 API1.0.254／Web1.0.279 完整備份至 `C:/ProgramData/PMR-Releases/Production/portal-gi-grid-20261119-20261119-081449`，隔離候選只加入本件後發布 API1.0.255／Web1.0.280。首頁、Login、實際 CSS／JavaScript、貼上 JSON 端點匿名401、API health production／database ok、候選與部署 DLL 雜湊、四份保護設定及 uploads 445檔均通過；無 migration 或正式名冊寫入。

狀態：已驗收／正式發布完成。

## 2026-11-19 正式組合回歸修正

- 問題：團保正式候選從舊 Git 基底重建整個 Web DLL，未包含先前正式已驗收的最新版藥液量測錄入頁與三個共用前端檔案，造成正式頁面退回舊版；資料庫、公式及量測資料未回退。
- 授權：使用者明確要求將目前正式已驗收的全部功能更新至正式。
- BDD：Given 正式站原有已驗收功能與本次團保格線皆須保留，When 建立修正候選，Then 候選必須以今天發布前完整正式候選為基底，再合併團保 API／格線頁；不得直接使用測試區整包。
- 驗收：正式藥液頁必須包含初驗／複驗滴定值、班別與取樣階段；三個藥液共用 JS、團保格線、正式版本、production health、資產 MIME、保護設定及 uploads 均須通過。
- 回復：正式發布前完整備份 API／Web；若驗證失敗，回復本次備份，不回復資料庫。

## 2026-11-19 正式組合回歸修正結果

- 以今日回歸前已驗收的完整正式候選為基底，合併團保九欄格線後發布正式 Portal API 1.0.256／Web 1.0.281；沒有直接套用測試區整包。
- 藥液量測錄入頁已恢復初驗／複驗滴定值、班別、取樣階段及三個既有共用前端檔案；團保九欄格線仍保留。
- 團保 API 63項、Web契約4項、藥液前端61項及兩個 Release build 全數通過，0警告／0錯誤。
- 正式首頁、Login、實際 CSS／JavaScript、API health production／database ok、版本、匿名401、候選／部署 DLL、四份保護設定與 uploads 445檔均通過；無 migration、正式名冊或量測資料寫入。
- 修正前完整備份：`C:/ProgramData/PMR-Releases/Production/portal-accepted-composite-20261119-20261119-084130`。

狀態：已驗收／正式組合修正發布完成。
