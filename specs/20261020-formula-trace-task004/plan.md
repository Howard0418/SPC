# 計畫
- [規格](spec.md)：核心測試先行。SPC新增ChemicalPortalMeasurementService及受認證API；借用UploadService staging／confirm／SPC統計，交易外層鎖batch與排序PPC。
- ChemicalFormulaScheduleService遇外層交易時沿用，不建立巢狀交易、不提前commit；初驗／複驗分別prepare及trace，使用可信receipt結果覆蓋client量測值。
- Portal SpcProxyController轉送schedules／preview／confirm，沿用spc_access_token；master-data回傳已發布修訂識別。前端單筆／日報批次共用日期契約，變更清token、顯示版號與格式錯誤。
- Happy：跨日／初複驗／重送；Boundary：台北切日／未排程混合；Invalid：過期／偽造／缺raw／Viewer；Regression：既有日報upsert、查回及統計。SQL隔離交易故障測試，不以InMemory證明rollback。

- 發布必要保護：Portal既有publish-all會覆蓋環境設定，來源與部署hash不同；加入MSBuild既有appsettings/web.config防覆蓋targets。以舊時間戳sentinel隔離發布先證實原方式會覆蓋，再驗證保留。仍只用publish-all發布實際測試IIS，不改binding／環境值。
