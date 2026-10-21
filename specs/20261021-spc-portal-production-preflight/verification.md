# SEC-FOLLOWUP-006 唯讀核對
日期2026-10-21。

## 版本與實際DB
- 正式遠端DLL：SPC API0.1.60.0；Portal API1.0.182.1/Web1.0.225.0。
- 本機測試DLL：SPC API0.1.67.0；Portal API1.0.190.0/Web1.0.233.0。SPC frontend/package.json版本0.1.69屬來源宣告，尚未核對正式/測試bundle hash。
- SPC正式appsettings SqlServer連線實際DB_NAME=PMR_SPC_2026。
- Portal正式appsettings EnvironmentSwitch.Target=Production，Production連線DB_NAME=PMR_PORTAL_UAT（沿用既有名稱，不因UAT字樣擅改DB）；web.config未列相應environmentVariable。尚未核對pool/machine級環境覆寫。

## SPC正式缺項
正式__EFMigrationsHistory已有20261008183026_AddSpcPointRemarks，但缺下列4筆：
1. 20261007090000_AddChemicalFTableVersionHistories：查sys.tables亦無ChemicalFTableVersionHistories。這是F表版本歷史，不能混同既有F表主檔。
2. 20261019235606_AddSpecificationLimitVersions：新增規格版本歷史表。
3. 20261020040256_AddChemicalFormulaTraceSchema：6表（ChemicalFormulaScheduleRevisions/Effectivities/Policies、ChemicalCalculationReceipts/Traces、ChemicalStatisticsJobs）、量測2欄及索引/外鍵/trigger/SpcChemicalDirectWriter角色與DENY；需完整DDL審查，不只是新增空表。
4. 20261020052530_AddChemicalDirectWriterProtocol：2個直寫procedure及execute grant。
- 本次查上述8張必要表（含規格/F歷史）及2程序皆未列出；SpcChemicalDirectWriter亦未列出。無DDL、無資料變更。
- 程式依據backend/MesSpc.Api/Migrations/以上4檔、ChemicalTraceSqlV1.cs、ChemicalDirectWriterSqlV1.cs。

## Portal與發布風險
- 正式migration歷史最新為20261015053702_AddGroupInsuranceWorkflow，與目前來源最新命名一致；尚未完成逐筆全清單/實體schema比對，不宣稱完全無pending。
- SPC Program.cs使用db.Database.Migrate；Portal src/PmrPortal.Api/Program.cs使用MigrateAsync，正式DDL未審查前不可啟動新版觸發。
- Portal Program.cs:58～62與PortalDataProtection.TestKeyDirectory限制為測試目標；目前測試金鑰修補不等於正式SMTP持久化完成，需正式環境金鑰保存方案與重啟相容檢查。
- Portal兩份使用者修改的團保xlsx/docx僅status確認，未讀內容或提交。
- 無安裝、停站、DB寫入、寄信或正式發布。文件檢查通過；build/測試發布不適用。SEC-FOLLOWUP-006未完成，總剩4件。

## 四筆DDL審查完成，未套用
- 已讀4筆Up及2份凍結SQL，整理[授權範圍](database-scope.md)：8表/2可空欄/21索引/9 trigger/2程序/直寫角色，無歷史回填或policy啟用，不加入個人角色。
- 7張前置實體表存在，2新增欄/公式複合唯一約束/TR_Chemical% trigger查無。只有sys目錄唯讀查詢，無業務資料輸出或修改。
- 文件內容/連結/diff檢查通過，build/發布不適用。正式複本驗證、正式金鑰相容及正式套用未執行，仍剩主要4件/TransFiles1件。

## 正式授權、備份與獨立SQL複本驗證
- 使用者明確同意四筆SPC遷移/role/procedure範圍，未授權個人正式writer成員或自動啟用policy。
- 讀取正式IIS既有設定，核對PMR_SPC_2026，COPY_ONLY/CHECKSUM備份及RESTORE VERIFYONLY通過。確切SQL備份路徑與UTC時點保留release-staging/spc-portal-preflight-20261021/backup-proof.json，不提交。
- 已還原唯一新庫PMR_SPC_RELEASE_VERIFY_20261021_169c50b7；不使用REPLACE、不覆蓋既有檔/庫，無站台指向此庫，未啟動背景服務或通知。SQL複本可建立不等於具有Runtime/整機演練環境。
- 新增opt-in測試tests/MesSpc.Chemical.Tests/ProductionFormulaMigrationRehearsalTests.cs，編譯旗標SPC_PRODUCTION_SQL_TESTS、連線環境變數SPC_PRODUCTION_REHEARSAL_CONNECTION；強制唯一驗證庫前綴並再次核對DB_NAME。連線僅保存在受限ACL私有clone.json，不提交或輸出。
- 1項實際SQL測試通過：pending恰為4筆；Up連續兩次、pending歸零；所有原60表原有欄位逐列JSON SHA256排序再聚合與筆數前後一致（不輸出資料值）；8新表皆0筆；DBCC CHECKCONSTRAINTS無違規。無故障回復、Down或新舊匯入/圖表業務驗證，不把結構測試冒稱全流程驗收。
- 正式站SpcApi/SpcWeb仍Started，正式4筆新migration計數仍0。此次只有備份與獨立SQL庫寫入，正式無DDL/業務資料變更、無停站/發布。
- --no-restore建置成功0錯誤；保留4個既有NU1902/NU1903（System.Security.Cryptography.Pkcs 9.0.4）警告，本次未變更套件；正式發布前仍需處理，不聲稱安全掃描通過。
- 文件與diff檢查通過。複本/備份保留，不自動刪除；正式套用待004維護與其他發布前置。總剩主要4件/TransFiles1件。
