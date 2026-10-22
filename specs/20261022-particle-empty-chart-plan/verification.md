# 唯讀驗證紀錄

- 日期：2026-10-22；環境：本機原始來源；狀態：規劃完成，功能／資料寫入未開始。
- 讀取AGENTS、SDD／需求／變更、既有Particle規格與進度，沿用現有已確認R1～R9、序列隔離、20點門檻、U圖分母與預覽確認規則。
- 依spreadsheets唯讀檢查規範，以既有openpyxl讀取原檔，再呼叫既有TransFiles純解析方法；沒有save／export，也沒有安裝套件。原檔讀前讀後SHA256一致。
- 查明來源範圍2023-10-25～2026-09-29；候選25,301筆、已識別78筆問題；來源時間與數值解析不是API預覽或實際匯入成功證明。
- B482、B483的日期文字及C734:C739「量測儀外送校正」已逐格核對；截圖查詢範圍R1／0.5四點，2026-08-01～09-30同序列32個有效點。
- 檢查ParticleCChartCalculator少於20點仍回points、ParticleUploadService錯誤阻擋Confirm、ParticleMeasurements落點、DeviceCode空值與原始量測查詢差異。
- 未連線資料庫／站台，未查使用者實際批次；未寫入測試資料、改原檔、程式、migration或部署，不宣稱根因已全部確認／圖表已修好。
- 文件內容、連結及本次staged diff核對；純規劃build／程式測試／發布不適用。後續執行與測試結果依tasks.md保留未完成。
