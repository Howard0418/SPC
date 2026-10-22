# SPC-PARTICLE-U-ACCEPTANCE-TASK-001

日期2026-10-22；狀態：已完成來源唯讀檢查，待採樣體積／測試資料模式確認，尚未寫入。

## 範圍與現況
使用者要求沿用PD-3-257-04A-製造部落塵監控表單(黃光室) - 2026.xlsx建立U-chart測試，目標僅https://spctest.pmr.com.tw。主系統SPC，無Portal／TransFiles程式修改，無schema、角色、正式資料及發布需求。既有C圖1152筆不得覆蓋或重匯。

唯讀openpyxl檢查唯一工作表832列54欄、表頭與全部非空儲存格：B24日期／時間／位置，第25列粒徑，AA3量測時段，AA4地點，AA5監測通報條件；未找到採樣體積、體積單位或可換算的流量×採樣時長。量測時刻不是採樣時長，粒徑µm不是體積單位。原檔未修改。
`backend/MesSpc.Api/SpcEngine/Calculators/ParticleUChartCalculator.cs`要求每點SamplingVolume>0且SamplingVolumeUnit一致；缺少回SAMPLING_VOLUME_REQUIRED。既有測試批次未提供，不能直接變成有體積U圖，也不能由原始計數猜出體積。

## 計畫／BDD
- [x] 檢查來源及既有U圖規則，列明缺少的輸入。文件型驗證，不需build／發布。
- [ ] 使用者提供真實採樣體積／單位／適用範圍，或選擇明示假設的獨立模擬資料。
- [ ] 先決定不與既有批次／儀器混算的隔離方式，沿用預覽→確認API；保留原來源及真實／模擬標記，禁止靜默補1。若現有契約不能隔離，先記錄再評估最小修改。
- [ ] Given至少20個有效事件與正值體積，When查U圖，Then顯示Count/Volume與對應界限，實站核對並記錄MD。
- [ ] Boundary：不足20點仍保留正規化點；Invalid：無體積／非正值／混單位拒絕；Regression：原C圖批次、筆數與計算不變。
- [ ] 必要測試及資料新增後MD記錄頁面條件、批次、筆數、來源／假設與清除方法，完成後停止。

資料回復：僅依新批次處理，不刪改既有C圖。現在未新增資料，無需回復。
