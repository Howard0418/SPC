# SPC-CHEM-1102-TASK-001 正式生效核銷

- 日期：2026-11-18
- 狀態：DONE
- 主系統：Portal；相依系統：SPC
- 資料庫影響：無；發布影響：不重複部署；rollback：只需回復本次文件與進度狀態

## 目的

確認已完成四線隔離實站驗收的保存後查詢與保存規格快照，是否已隨後續Portal／SPC正式版本生效。若正式來源及部署版本證據一致，核銷既有小工作，不重新發布、不操作正式量測或公式資料。

## BDD驗收

- Given Portal保存後查詢已在測試站依送出PPC與量測日期刷新，SPC新保存已有伺服器規格快照
- When 核對目前正式Portal與SPC建置來源及部署版本
- Then Portal正式來源須保留`savedPpcId`／`savedMeasuredAt`並以同日查詢，SPC正式來源須保留`ChemicalSavedSpecificationService`的Capture／Present整合
- And 正式Web／API環境證據須正常，才能標示完成；不得重算或改寫歷史資料

## 驗證結果

- Portal正式API1.0.252／Web1.0.277隔離來源包含`savedPpcId`、`savedMeasuredAt`與`showSuccessCard(..., measuredAt)`，保存後固定查詢該量測日；正式Login與`/app-version`為Web1.0.277，`:8443/health`為production。
- SPC正式API0.1.134建置來源包含`ChemicalSavedSpecificationService`，`ChemicalPortalMeasurementService`執行Capture，`ChemicalHistoryQueryService`執行Read／Present；正式發布既有health／version為0.1.134／production。
- 四線5筆隔離實站驗收既有結果保持：保存批次、同日歷史與日報一致且皆OK；測試專用主檔已停用，追溯量測保留。
- 功能已隨後續正式版本生效，本輪未傳檔、未停站、未修改程式、API、DB、公式、F、主檔或量測，也未觸發新版本遞增。
- 中央進度已由核心未完成29→28、完成250→251、SPC相關9→8、Portal相關9→8；人工註記11與待核銷4不變。
- 原活頁簿已備份為`進度表-before-SPC-CHEM-1102-closeout-20261118.xlsx`，備份SHA-256為`63E617AA1B9ADE87DF82DAD46C39DA3968D93F7C81625CEB09A1B4128D21B34D`。
- 候選檔經工作表、目標列、計數、SPC／Portal分類及公式錯誤檢查後取代原檔；原檔與候選檔SHA-256同為`445BB8981472E4FEDDF1F92B26962AB408CBB4CB5B0D5815581F82DAC47E1582`，原生里程碑物件已保留並新增本件完成菱形。
