# 小型變更：儀器列表捲動改善
- 功能 ID：20260916-calibration-table-scroll；版本：1；狀態：測試站已發布
- 授權：使用者要求大量資料仍方便左右捲動，不需到最後一筆。
- 基準：[儀器欄位](../20260916-calibration-details/spec.md)。
## 範圍與驗收
- R-001／AC-001：列表高度最多 60dvh，資料於表格內上下捲動，水平捲軸不隨資料筆數移至頁尾。
- R-002／AC-002：表格上方提供向左/向右按鈕，固定表頭；橫向滑動可讀所有欄位，無透明表頭重疊。
- R-003／AC-003：保留資料、編輯及通知行為；桌面與手機大量資料驗證。
- 手機既有側欄擠壓內容：App.vue 僅儀器校正路由在窄螢幕改為上下排列，導覽區限制高度可捲動，保留導覽。
## 計畫與任務
- [x] 修改 InstrumentCalibrationsView.vue 捲動容器、表頭及按鈕。
- [x] 以 100 筆 mock 驗證捲動、表頭、首末欄、手機及 build:test。
- [x] 同步需求/變更，備份後只發布 SPC release/test 前端。
## 驗證
- AC-001～003：6 項 Playwright 案例回報 ok，包括 1280/390px 各 100 筆資料：表格高度≤60dvh、內部上下/左右捲動、固定表頭位置、最右端可達，以及原欄位編輯/日期/衝突回歸。
- 手機首輪發現既有側欄擠壓至 44px；限定本路由改上下排列後表格寬度>250px，重驗通過。桌面/手機截圖已檢視。
- npm run build:test 成功；既有 bundle 大小及無關 EquipmentPointsView table 警告保留。
- 僅發布前端 D:/SPC/release/test/frontend，已核對 IIS SpcWeb 8083 指向；備份 release-staging/calibration-scroll-20260916-132959/frontend。頁面與新 JS 200。
- 不改 API/資料庫/通知；正式站未發布。瀏覽器使用隔離 mock API。
