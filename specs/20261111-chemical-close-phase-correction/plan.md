# 技術計畫

1. 先建立前端單元測試，固定 `CLOSE` 不得翻譯成晚班；合法 `OPEN/MIDDLE` 顯示保持。
2. 將圖表標籤邏輯改為：班別僅 OPEN/MIDDLE；遇舊 CLOSE 以「收線待校正」安全標示，不能顯示晚班。
3. 用唯讀清冊將每筆 `CLOSE/GENERAL` 對照來源檔名、批次與 Trace；檔名含中班者回填 MIDDLE，其餘合格日期檔回填 OPEN，並將收線寫入 SamplingStage=CLOSE。無唯一來源者隔離。
4. 為每個環境產生前後清單與值／公式／F 指紋；先 COPY_ONLY、CHECKSUM、VERIFYONLY，再在單一交易中更新已核准列，保存稽核。
5. 測試區先套用、重跑確認 0 異動、發布測試站並驗證 Web／API；使用者畫面驗收後再套用正式區並驗證。

## 回復

依環境備份及逐筆 before 清單，在受控交易中還原 SamplingPhase／SamplingStage；不還原整庫，不觸及其他量測欄位。
