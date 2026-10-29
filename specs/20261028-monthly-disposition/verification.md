# TASK-005 測試完成紀錄
- 日期2026-10-28；依004→005順序完成開發及測試站發布。2026-10-29 取得正式發布授權後，已發布正式 SPC。
- 摘要新增AbnormalCount／HandledCount／UnhandledCount；直接沿OverallStatistics合併主副圖、OOC／OOS及子組告警，不建立第二套結案規則。僅讀取該圖量測ID之完整告警，不使用500筆分頁。
- 月報摘要及前期比較讀完整資料，原單張互動圖預設1000點限制保持；排除點依原圖表規則。Excel各分類尾端新增「異常件數」「已處置件數」「未處置件數」，保留前25欄及004三表／產品分類。
- 畫面三語及Excel明示依目前處置狀態，非月底快照。缺告警列未處置，Open／InProgress未完成、Closed／legacy ack已完成，子組沿Overall相關告警全部完成口徑；缺API欄位畫面顯示—，不假裝0。
- 測試先行：新增1001點案例舊版失敗，前端缺件數欄位案例失敗後才實作。最初前端重跑曾被暫存回置觸發的Vite EBUSY遮罩攔截，重啟開發伺服器後確認真正缺欄位失敗，不把遮罩錯誤當功能測試結果。
- API全套305通過／8條件略過；新增InMemory及SQLite兩條實際查詢驗證1001點／1000告警、同點雙訊號去重、缺告警末筆及結案變化；量測／告警數量不變，無新增計算結果。Excel對照同一DTO得到1001＝501＋500。
- backend/MesSpc.Api.Tests的Overall回歸9項通過（含主副圖、子組、屬性／趨勢、排除與預設1000限制）；既有UnitTest1兩項nullable編譯警告保持。
- UI本機39項通過，涵蓋三語／四種寬度、月／週邊界、分類、件數與錯誤；排除004已證明原版同樣失敗的1項排除點刷新案例，並非宣稱全套40項通過。測試站已發布bundle的月／週／分類／件數5項通過，API使用fixture，不宣稱正式資料或測試DB月報人工驗收。
- 前端build:test、2759鍵三語檢查通過；API Release publish成功。補長中文備註邊界測試先失敗，限制Excel欄寬255並保留全文換行後，報表相關29項通過；不以截斷文字修正格式。
- SPC測試API0.1.92／Web0.1.113，核對SpcSingleTest／PMR_SPC_TEST後發布，appsettings／web.config指紋保持，版本／health／首頁通過。最後DLL SHA256 83E878D1F8A1D06213F3BBD00FBED111DB769B88E781C4AF25417D250F53A7F0。正式SPC API90／Web70及Portal未變。
- 回復：release/backup-monthly-disposition-20261028後端及frontend；最後換行候選另有release/backup-monthly-disposition-wrap-20261028。停止測試集區且等待Stopped後還原對應檔案，再啟動／驗證；不操作正式IIS。
- 匯出樣本經ClosedXML型別／數值測試與LibreOffice重開／PDF核對，四類保留3＝2＋1與可讀表頭／註記。此為寬表Excel，未另改列印縮放設定，沒有宣稱Microsoft Excel人工驗收。
- 原進度表已更新：階段3摘要／日期／同表平台版本與明細、216列005狀態及10/28原生藍色里程碑310；原23圖形及其他專案內容保持。原檔曾被鎖住，使用者回覆已關閉後核對內容hash未變，才備份取代。
- 備份Voice/備份/進度表_月報005測試前_20261028-133903.xlsx；原檔SHA256 4041954B0D849E5D6A9A0CD4182304D550E314F0600E07E24AA5D3D03DA50BAD。ZIP／OpenXML0錯誤、重開計數213／26／171／11／4、公式錯誤0及11頁列印檢查通過。
- 未完成核心仍26件，含004／005測試完成但正式待授權；009等F與有效日。其他暫停／受阻不恢復，無DB結構、公式、F或既有量測變更。

## 正式發布驗收（2026-10-29）
- 正式備份：`C:/inetpub/wwwroot/production/backend.backup-monthly-disposition-20261029-125242`、`frontend.backup-monthly-disposition-20261029-125242`。
- `https://172.16.119.140:8443/api/health` 搭配正式 SNI Host 回 HTTP 200，`environment=production`、`version=0.1.92`；正式首頁回 HTTP 200。
- 僅更新 TASK-005 必要 API/Web 檔案，未執行資料庫 migration、未修改 Portal。
