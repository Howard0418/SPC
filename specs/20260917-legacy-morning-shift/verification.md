# 驗證結果
- 核心測試先建，舊版2項失敗；實作後21項Chemical測試通過。新增GENERAL／空字串／純空白皆早班、讀取／日曆一致、中班排除、修改沿用ID、衝突拒絕讀取及寫回且數值不變。SQLite測試資料隨fixture清理。
- 保留N1/N2階段獨立與既有15項回歸；無DB/schema批次變更。使用者實際修改舊日報時才正規化早班代碼。
- Release publish成功；NU1900僅NuGet弱點查詢網路警告，未宣稱弱點掃描成功。
- 僅發布SPC API至D:/SPC/release/test/backend，IIS路徑已核對、AppEnvironment=test及PMR_SPC_TEST。備份backend.backup-legacy-morning-20260917-131432；DLL與stage hash相同，appsettings/web.config hash不變，offline已移除。
- 實際已登入Portal驗收：C1／2026-09-08／早班，原GENERAL班別日期現在顯示「已有資料，可載入」，點載入後「修改模式｜早班｜已載入6筆既有資料」，可見滴定值、量測值及調整資料。切換中班顯示「尚無資料，可新增」。未送出或修改真實量測。
- Portal未重啟或發布、正式站未變更；非手動日報來源仍維持歷史查詢，N1/N2未分階段仍不自動歸開／收線。
