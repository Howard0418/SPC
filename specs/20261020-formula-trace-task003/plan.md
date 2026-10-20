# 計畫
- [規格](spec.md)：核心先測試；加入ChemicalFormulaScheduleService、相容Excel解析器、受認證API及獨立Vue元件。
- 日期沿用VersionHistoryDateRange；發布使用Serializable交易及SQL Server PPC UPDLOCK/HOLDLOCK，baseRevision樂觀防護；已消耗／發行receipt期間保留。
- 唯讀preview以DataProtection token綁定actor／canonical payload／revision，有效10分鐘；prepare重新計算核對，交易鎖定排程，receipt有效10分鐘。
- 所有decimal輸入以字串傳輸，拒絕未知變數，公式／F內容由DB取得；不信任客戶端公式或結果。
- 後端單元／SQL隔離測試、前端瀏覽器測試、build及測試站健康／版本／設定保留核對。正式不操作。
