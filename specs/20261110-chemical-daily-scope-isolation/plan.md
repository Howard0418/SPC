# 實作計畫
1. 先以 SQLite 測試重現 PROCESS 多筆造成誤判，並保護真正 CHEM 重複的 409。
2. 將日報及月曆查詢限制為啟用中的 CHEM 管制項目；檢查 PortalDaily 寫入映射範圍。
3. 更新 Portal 可辨識衝突的標題與說明。
4. 執行聚焦測試、Release build、測試站發布與 Web/API 連線驗證。
5. 同步需求、變更紀錄、TODO、中央進度表，提交並上傳 Develop。

回復方式：還原本工作提交並重新發布前一版 API／Web；本件無資料庫回復作業。
