# 技術計畫
- 功能 ID：SPC-OVERALL-001；規格版本1；[規格](spec.md)。
- 新增 Overall service／controller，沿用 SpcService 圖表引擎，依序讀取有資料的啟用PPC，不平行使用DbContext。查詢無500／1000筆限制，日期沿用量測日。聚合以PPC及主圖位置為案件，主副圖合併，告警依量測ID及最新狀態判斷，不以產品／製程／特性模糊配對。
- 圖表引擎新增預設關閉的完整讀取參數，僅Overall啟用；先建1001筆與舊預設回歸測試再修改核心。
- GET /api/v1/spc/overall?startDate=yyyy-MM-dd&endDate=yyyy-MM-dd；回傳success/data/message，繼承既有條件式全域登入保護，不新增角色。
- /spc 顯示獨立Overall元件；日期與載入錯誤獨立，既有單項查詢不變；沿用三語字典與Tailwind。
- 驗證：聚合單元／SQLite整合、前端Playwright日期/錯誤/零值/手機版、既有相關回歸、API與Web Release build。
- 發布：核對IIS及test設定後備份release/test/backend與frontend、保留appsettings/web.config/private-data，使用staging產物更新；health／頁面／唯讀API驗證。回復同次備份，無DB回復需求；正式站不發布。
- 收尾依規則同步原進度表及小工作總表，OpenXML／渲染／列印驗證後取代；僅本件必要檔案提交上傳。
