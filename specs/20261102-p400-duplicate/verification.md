# 驗證
- 2026-11-02測試環境PMR_SPC_TEST：N1／催化／P400有2593（無單位、無量測／計算／追溯）及2619（ml/L、6筆量測／計算）。N2的2515為不同線，70筆量測，保留。
- COPY_ONLY／CHECKSUM備份及RESTORE VERIFYONLY成功；備份位於SQL Server的`C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\PMR_SPC_TEST_P400_20261102_164229.bak`。
- 經測試站現有PUT API僅設2593的isEnabled=false；API查回僅此欄差異，其他主檔完全相同。DB UpdatedAt隨正常API更新，其他定義及關聯不變。未永久刪除。
- 全站量測16113筆、量測指紋-1635324244前後一致；N1保留2619及N2 2515定義／計算／量測數不變。7組輸入濃度等價檢查誤差不超過1e-8，不代表其他調整公式已驗收。
- Chrome實際測試站「藥液分析總覽」N1＋催化＋全部狀態：P400一啟用一停用；僅啟用時只一筆P400及一筆氯化亞錫。測試Web113／API97，正式未修改。
- 初次version請求曾逾時，後續health／version及資料查回成功；不宣稱已修正逾時根因。主檔編輯頁曾持續載入，未宣稱該頁人工驗收完成。
- 原Excel已同步11/02實際日期、總表248列及原生P400里程碑為藍色測試完成／待正式；核心36、SPC直接14保持。備份`進度表_P400測試去重後同步前_20261102-165115.xlsx`。ZIP／其他儲存格與原生物件保持檢查通過，OpenXML候選及原路徑均0錯誤；作者工具重讀渲染與公式錯誤掃描通過，LibreOffice列印11頁、相關第4／11頁目視通過。未做Microsoft Excel人工開啟。
- 無應用程式或schema修改，build／網站部署不適用；沿用現有API，未重新發布網站。
- 回復：重新盤點測試2593仍符合原業務鍵後，以現有API恢復isEnabled=true；原Excel以當次備份回復。正式尚待另行授權及獨立盤點。
