# 小型變更：藥液日報原始滴定值顯示與第二輸入收斂
- 功能 ID：`PORTAL-CHEM-RAW-INPUT-1111-TASK-001`
- 版本：3
- 狀態：測試站完成，畫面驗收通過；正式未發布
- 專案／授權依據／需求基準：Portal＋SPC；使用者 2026-11-11 指出多數項目誤顯示第二原始值，且載入日報後未顯示既有滴定值。

## 問題、預期行為與範圍
- 現況一：Portal 只要看到日期公式排程，就假設公式需要 `Secondary`，造成單輸入公式也顯示第二原始值。
- 現況二：日報載入程式只讀 camelCase 欄位；若代理或舊契約回傳 PascalCase，既有分析值與原始滴定值可能顯示不一致。
- 驗收追查：2026-11-10 DP早班四筆分析值均有不可變Receipt／Trace原始滴定值（8.3、0.64、3.1、1.1），但daily API只讀UploadDetail；此批原始值因此未回傳，並非未保存。
- 預期：SPC 排程清單明示目前發行排程是否有任何公式使用獨立的 `Secondary` 變數；Portal 只有在為真時顯示初驗／複驗第二輸入。
- 預期：既有日報的初驗／複驗滴定值，同時接受 camelCase 與 PascalCase 欄位並顯示在輸入格。
- 非範圍：不修改公式、F值、量測資料、判定、保存或防重規則；不移除真正雙輸入公式的第二輸入。
- 系統／DB／發布／回復：SPC API 提供唯讀契約，Portal API/Web 顯示；無 schema 或資料寫入。測試站驗證後發布，正式站另行授權。回復為還原 API 契約與 Portal 顯示版本。

## 需求與驗收
- R-001：單輸入公式不得顯示「第二原始值」或「複驗第二原始值」。
- R-002：公式確實引用 `Secondary` 時仍須顯示並送出第二輸入。
- R-003：載入既有日報時須回填初驗及複驗的原始滴定值，不因 JSON 命名格式遺失。
- R-004：daily API須優先從最新Initial／Recheck Trace所指Receipt回傳原始輸入；無Receipt時才回退舊UploadDetail，不得補造不存在的值。
- R-005：Receipt／Trace 已保存的原始輸入必須原樣顯示；保存值為空值時畫面保持空白，不得再以舊 UploadDetail 的值覆蓋。
- AC-001（Given/When/Then）：Given 發行排程只有 `Primary` 公式，When Portal 取得主檔，Then 只顯示一個初驗與一個複驗滴定輸入。
- AC-002（Given/When/Then）：Given 發行排程含 `Secondary`，When Portal 取得主檔，Then 顯示第二輸入且保存契約保持。
- AC-003（Given/When/Then）：Given daily API 回傳 PascalCase 原始值，When 使用者載入日報，Then 畫面回填初驗／複驗滴定值且不觸發重算或寫入。
- AC-004（Given/When/Then）：Given 量測有Receipt／Trace但沒有UploadDetail滴定欄位，When載入日報，Then API回傳Receipt的初驗／複驗Primary與Secondary；量測及追溯保持不變。
- AC-005（Given/When/Then）：Given Receipt明確保存空的Primary或Secondary且UploadDetail仍有舊值，When載入日報，Then對應輸入格為空；Given Receipt保存非空值，Then必須顯示該保存值。
- 回歸風險：空值、直接輸入項目、真正雙輸入、舊 camelCase、歷史載入不得受影響。

## 計畫與任務
- 受影響檔案：SPC `ChemicalPortalMeasurementService`／測試；Portal `SpcProxyController`、`VariableMeasurementEntry`／測試；需求與變更文件。
- [x] T-001：先建立單／雙輸入排程與兩種 JSON 命名回填測試。
- [x] T-002：SPC 排程清單回傳 `requiresSecondaryInput`。
- [x] T-003：Portal 只依旗標顯示第二輸入，並兼容載入欄位命名。
- [x] T-004：驗證、發布測試站、連線檢查及文件／進度同步。

## 驗證
- 方式、環境：SPC／Portal 單元測試、Portal Playwright 焦點測試、Release build、測試站 Web/API/實際 API 基底連線。
- 實際結果及證據：SPC `ChemicalPortalMeasurementTests` 9/9、Portal API 7/7、頁面 Playwright 8/8、兩專案 Release build 通過。單／雙輸入、camelCase／PascalCase及載入不重算均覆蓋。
- 驗證結果：新增 Receipt／Trace 無 UploadDetail 的回填測試；相關 API 測試 14 項通過。DP 2026-11-10 既存 Receipt 值為 PARRADELETE 8.3、過硫酸鈉 0.64、PARRARINSE 3.1、硫酸 1.1，未重匯或改寫資料。
- 空值規則驗證：Receipt 欄位存在且為空時保持空白，不回退顯示 UploadDetail 舊值；Receipt 欄位有值時一律顯示保存值。相關 API 測試 15 項通過。
- 發布狀態：測試 SPC API `0.1.125` 待發布驗證。Portal API `1.0.230`／Web `1.0.271` 不需重發。正式未授權、未發布。
- 畫面驗收：測試 Portal 實際載入 DP／2026-11-10／早班，四筆滴定值依序為8.3、0.64、3.1、1.1，分析值222.11、0.60、92.07、5.39與判定均保持；未按儲存。
- 限制或未決問題：待具權限使用者在測試站確認實際線別只保留必要第二欄，載入既有日報可見初／複驗滴定值。
- 進度表：原檔已備份並同步小工作、Portal／SPC里程碑及37／13計數；ZIP封裝13項、2工作表、1 drawing、公式錯誤0，渲染與原路徑重讀通過。
