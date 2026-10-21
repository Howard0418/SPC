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
