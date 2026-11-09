# 小型變更：稽核紀錄顯示可辨識的頁面與操作名稱
- 功能 ID：SPC-AUDIT-1103-TASK-009-LABELS
- 版本：1
- 狀態：部分完成；測試站檔案已更新，外部連線／畫面驗證受阻
- 專案／授權依據／需求基準：使用者 2026-11-09 回報稽核表直接顯示內部代碼，無法直覺辨識頁面。

## 問題、預期行為與範圍
稽核資料的 `PageKey`、`EventCode`、`Outcome`、`Source` 與 `TargetType` 是穩定追溯代碼，不應修改資料庫值；查詢頁需以目前語系顯示可讀名稱，並在次要文字保留原始代碼。未知代碼須原樣顯示，避免錯譯或隱藏新事件。本件只調整 SPC 前端顯示，不改稽核寫入、查詢、權限、資料庫或正式站。

## 需求與驗收
- R-001：已知頁面及事件先顯示可讀名稱，下一行保留原始代碼。
- R-002：結果、來源與目標類型顯示可讀名稱；HTTP 與 ID 不變。
- R-003：空值顯示 `-`；未知代碼只顯示原值且不重複。
- AC-001（Given/When/Then）：Given 查詢回傳 `admin.audit.events` 與 `navigation.page.enter`，When 使用者查看表格，Then 顯示「使用者活動紀錄」、「進入頁面」及兩個原始代碼。
- AC-002（Boundary）：Given 後端新增前端未知代碼，When 表格呈現，Then 原始代碼仍可見且不顯示錯誤名稱。
- AC-003（Regression）：查詢、篩選、分頁、匯出、權限及稽核內容均不改變。

## 計畫與任務
- 受影響檔案：稽核顯示工具、`AuditEventsView.vue`、三語系、前端測試及本件文件。
- [x] T-001：先建立已知／未知／空值的顯示單元測試。
- [x] T-002：實作 R-001～R-003 並完成前端 build。
- [ ] T-003：測試站目錄已更新；Web／資產／API 實站驗證受連線與瀏覽器工具阻擋。正式站不發布。
- [x] T-004：同步需求、變更紀錄、TODO 與原進度表。

## 驗證
- 方式、環境：Node 單元測試、i18n 檢查、Vite test build、SPC 測試站 HTTP／MIME／health／version。
- 實際結果及證據：顯示／權限回歸 5 項通過；三語系 2,817 鍵檢查與 `build:test` 通過。測試站 `release/test/frontend` 已更新為 Web 0.1.122，備份 `frontend.backup-audit-readable-20261109-130749`。發布資產包含中文標籤且不含正式站 Host。
- 發布狀態：測試站檔案已更新；命令列對測試站 TLS／連線失敗，Windows 瀏覽器控制管線不存在，依規則不宣稱完整發布完成。正式站未發布。
- 限制或未決問題：待測試站恢復可連線後補首頁／JS／CSS MIME、health/version、同源 API 與登入畫面截圖；TASK-009 效能基準仍分開追蹤。
- 進度表：已同步為部分完成／外部連線與畫面阻擋；原檔 SHA-256 `F66803EFC37E5391A33A392C8EFC9C7FFB4EC5CD26C616D2C847D00B605F85AD`，OpenXML 0 錯誤、ZIP 必要 part 與原生 drawing 1 保持，重開及兩處渲染通過；Microsoft Excel 人工開啟未執行。
