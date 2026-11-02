# 計畫
- SPC-CHEM-1102-TASK-001／v1；[規格](spec.md)。
- Portal僅修改`src/PmrPortal.Web/Pages/Spc/VariableMeasurementEntry.cshtml`及必要測試：送出前保存PPC／量測日期，成功後設定同製程、同日查詢，再重設輸入；保留原分頁、重試、取消及錯誤分類。
- 測試先行：保存後查詢範圍／N2與其他指定線別、POST失敗、GET失敗與GET-only重試、送出中切換條件、跨日及0；既有stage／effective／history回歸。沒有模型／API／DB變動。
- 必要API／Web Release build後，使用Portal根目錄`scripts/publish-all.ps1`發布實際本機測試IIS目錄；先核站台binding／實體路徑、備份及設定hash，避免把既有未提交程式修改帶入發布，必要時使用隔離來源。
- 驗證Web首頁、主JS／CSS的200與MIME，API health/version及環境、瀏覽器同源API。正式不發布，不使用D:/Sites/PmrPortal。
- rollback：原測試站同批備份還原本件產物，保留環境設定／uploads；無資料庫回復。原進度表同步001進度與原生里程碑，其他工作保持。
