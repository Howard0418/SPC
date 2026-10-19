# 驗證
- 使用者已授權 SPC＋Portal 正式站發布。
- 唯讀確認本機僅有兩系統測試 IIS，正式目標在遠端主機；尚未寫入正式站。
- 正式主機 IIS 已確認 SPC 為分離 API/Web，Portal 亦為分離 API/Web，路徑均非禁止路徑。
- 其餘待驗證，不標示完成。

## 前置驗證進度
- SPC 後端全量 59 passed、前端 node 測試 15 passed。
- Portal Api.Tests 初跑 93 passed / 12 failed。已定位測試本身的中文 JSON 跳脫判斷、過期固定日期、訪客請求缺必要到訪時間、InMemory 每個 DbContext 使用不同資料庫名稱及 Windows EventLog 寫入環境依賴；只修正測試資料/隔離，不改產品邏輯或降低權限。
- 已以不啟動 Web Host、不連 DB 的 EF 檢查器產生實際 migration 列表與 SQL；SPC F表 migration 缺 DbContext 標註，未被現有 EF 探索，正式已有表，不列為待執行。實際待執行為 SPC SpcPointRemarks、Portal ExpandFeatureSettingsMessage / AddGroupInsuranceWorkflow 共 3 筆。
- 已向使用者另行詢問是否將上述正式 schema 更新納入；未取得回覆前不寫入正式 DB 或覆寫會自動 migrate 的站台。

## 發布前產出（尚未正式上線）
- Portal 測試fixture已依既有 CHANGELOG 業務基準修正（完成填寫直接完成/紙本簽名，異常改善欄位選填）；全量 105 passed / 0 failed，不修改產品邏輯或降低權限。
- Portal Compatibility.Tests 8 passed；SPC backend 59 passed、前端 node 15 passed。
- Release 隔離包完成：SPC API 0.1.60、Web 0.1.63；Portal API 1.0.178、Web 1.0.221。Portal 依既有 publish-all.ps1 產包至新的 staging 路徑，未更新本機測試 IIS。
- 四份 delivery ZIP 與每檔清單/hash/manifest 已生成，設定與上傳/私有資料不進包。SPC 前端環境為 production，API/Portal 為已核對的正式端點，不含測試 API/登入指向。
- 既有 SPC Pkcs 套件 NU1902/NU1903 與前端 chunk-size 警告仍存在；未在發布範圍更新套件或架構。
- 具體 DB 變更、備份與回復界線見 database-upgrade-review.md；正在等待正式 migration 授權，正式 DB 與站台尚未寫入，T3/T4 保持未完成。

- Portal Etch 測試專案：本次發布前回歸 10 passed、0 failed；對應咬蝕診斷轉送與測試環境限制。

## 使用者暫不更新正式資料庫
- 2026-10-19 明確回覆「暫不更新正式資料庫」。
- 未套用 migration、未建立正式 DB 備份、未將套件傳至正式主機、未覆寫正式檔案或停止正式 AppPool。先前遠端操作均為唯讀盤點。
- 新版 SPC/Portal 啟動會執行 migration，因此保留隔離包，不執行 T3/T4；無需正式站回復操作。
- 工作未完成，提交/上傳正式發布成果未執行；既有工作區修改與準備包保留。

## 2026-10-19 正式發布結果（取代先前暫緩狀態）
- 使用者更正授權更新正式 DB；2026-10-19 10:43–10:45 執行。兩站網站與 IIS 備份完成；兩個正式 SQL DB 使用 COPY_ONLY/CHECKSUM 備份且 RESTORE VERIFYONLY 通過。
- 發布備份目錄：兩台正式主機 C:\ProgramData\PMR-Releases\SPC-PORTAL-20261019-104341；SQL 備份位於 SQL Server 預設備份目錄，以 DB 名稱及相同時間戳命名；安全 evidence 留在主機及本機 ignored staging，不提交。
- 三筆已授權 migration 套用並核對 history/schema：SPC SpcPointRemarks；Portal message nvarchar(max) 與五張團保表。未重建 F 表、未匯入名冊或手動更動業務值。
- SPC API 0.1.60 / Web 0.1.63；Portal API 實際 DLL 1.0.178.0 / Web app-version 1.0.221.0。四包檔案 SHA-256 核對一致，保留 appsettings、web.config、附件與私有資料，未改 IIS binding。
- 正式 SPC health/version environment=production；SPC HTTP 首頁及全部新 JS/CSS MIME 通過；Portal HTTPS Login、app-version、API health 回 200；兩套咬蝕 preview 匿名請求均回 401。四 AppPool Started。
- 既有必要測試：SPC 後端 59、前端 15；Portal API 105、相容性 8、咬蝕 10，全通過。來源自測試產包後無產品程式修改，不重跑相同測試。
- 登入後 SSO、實際資料查詢及人工製圖未於正式站操作，不宣稱完成人工業務驗收；既有 SPC HTTPS 前端跨協定 API 拓撲限制未改造，沿用 HTTP 正式入口。
- 無 app_offline 或檔案刪除；舊資產與本次備份保留，未執行 rollback。若需回復，以同批網站備份回復應用並保留 additive schema；整庫還原需另核對後續業務資料。

## 來源上傳狀態
正式部署已完成；自動核准審查拒絕 GitHub push，原因為內部程式/測試/發布文件的特定遠端外傳尚未獲明確授權。未繞過審查；先完成本機提交，遠端上傳保留待辦。
