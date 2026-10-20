# TASK-005 驗證（2026-10-20）
狀態：實作、自動化、真實低權限連線及測試發布完成。正常Portal登入後使用者操作的業務匯入／圖表驗收另見後續TASK-006，不冒稱已人工完成。

## 驗證
- 核心先紅：ChemicalStatisticsProcessorTests缺類別編譯失敗，新增consumer後通過「缺圖表→Failed／無半筆統計→修正後Completed→重試不重複」。
- `dotnet test tests/MesSpc.Api.Tests --filter 'ChemicalStatisticsProcessorTests|ChemicalPortalMeasurementTests|ChemicalFormulaScheduleTests|SpcPointExclusionCalculationTests|ChartSegmentDetailsTests' -p:UseSharedCompilation=false`：23 passed。
- `dotnet test tests/MesSpc.Chemical.Tests --no-restore`：59 passed。
- Python先紅（缺chemical_direct_writer），後`python -m unittest test_chemical_direct_writer test_direct_import test_etch_upload`：19 passed；py_compile成功。
- `tools/testing/Invoke-ChemicalTraceSchemaRehearsal.ps1 -TestFilter ChemicalDirectWriterSqlTests`：完整SQL scenario通過，最終隔離庫`SPC_FORMULA_SCHEMA_VERIFY_20261020_4aa2813d`，來源備份與最終備份皆CHECKSUM／VERIFYONLY。
- SQL涵蓋migration Up/Down/Up、低權限只execute、錯actor51220、直接DML229、可信初複驗2.47／4.69、同receipt並發與新receipt同內容重送去重、不同內容衝突、過期51215、重發排程51216、job插入晚期故障全列rollback、雙consumer只產一筆統計及低權限status回Completed。
- 初次fixture缺ChartGroup FK及PartId空值相容差異已修正；權限負向測試須避免未REVERT連線被pool重用，隔離fixture關閉pool並補REVERT。最終scenario重新通過，不把失敗回合列通過。
- Release publish成功；既有Pkcs NU1902/NU1903保留，未安裝或更新套件。

## 實作與安全
- EF migration `20261020052530_AddChemicalDirectWriterProtocol`：只建立兩個procedure及execute grant，無新資料表、無歷史回填。
- `CommitChemicalMeasurementWithTrace`：PPC鎖，receipt actor與個人DB principal一致，來源／台北日／階段／修訂／到期／已消耗／目前規格檢查；server結果落batch/detail/VM/trace/job同交易。相同來源不同內容或既有日報不覆寫，明確修正走Portal。
- `ChemicalStatisticsProcessor`：job鎖與PPC鎖；統計及Completed原子寫入，失敗回復後安全記錄Failed與重試時間；commit後通知。正常程序不重算歷史；寄信仍為既有服務的提交後嘗試，不宣稱外部SMTP exactly-once。
- TransFiles原始值Excel新入口，保留文字及明確offset；只有preview/prepare用API，確認量測確實由SQL程序落地。失敗逐列隔離，Pending不冒稱統計成功，結果JSON可查回。
- TransFiles工作區非Git repo。新增模組／PowerShell／單元測試及只含本次GUI變更的patch保存至[來源交付](../../tools/transfiles-chemical-direct/README.md)；現場來源與交付hash一致，既有GUI檔案與使用者資料不整包加入SPC。

## 發布、個人帳號及回復
- 備份VERIFYONLY後發布SPC測試API **0.1.66**，Web維持0.1.68；實際IIS SpcSingleTest=`D:/SPC/release/test/backend`、DB=`PMR_SPC_TEST`、SQLMachine=`PMR-MESSQL`。正式／Portal／KM未更新。
- 應用備份`D:/SPC/release/backup-formula-task005-20261020-133726`；SQL預設備份目錄`SPC_TASK005_TEST_PRE_20261020-133726.bak`。
- 部署後health ok/test/0.1.66；兩procedure及migration存在。既有量測／公式／F歷史指紋、receipt數及環境設定hash不變；共用policy未啟用。
- 使用者明確提供`pmr\ihao_ting`。IP Windows連線18452，改已核對DNS主機後18456；SQL可解析該既有Windows身分但無登入。按規格v2建立該Windows SQL登入（default DB僅測試）、測試DB user，只加入SpcChemicalDirectWriter；未修改AD、未授予正式庫／sysadmin／db_owner。
- 實際本人Windows連線確認USER_NAME=PMR\ihao_ting、writer=1、db_owner/sysadmin=0；直接SELECT及UPDATE均229，status procedure成功。個人環境變數只存無密碼Windows整合連線；client可從HKCU讀取，不需重新登入Windows。Python→隱藏PowerShell→SQL實際唯讀狀態查詢PASS。
- rollback：先停新TransFiles入口與worker，還原上述應用；保留trace/job及新procedure，不對已落地資料執行Down。必要時DBA移除本次writer role membership即可停個人入口；可保留Windows登入，不改其他角色或正式DB。此次未執行rollback。

## 待真人驗收
- 測試專用PPC跨日原始值Excel、正常Portal登入／確認與查回、圖表實際版本：由TASK-006提供操作案例。
- EXE：TransFiles `發布版_20261020_日期公式DB測試/BatchConvertTool.exe`；PyInstaller既有環境打包成功、隱藏啟動6秒存活（只停止本次測試程序）。SHA256=`EB4777FF2C2B90755622FA9FAFC8403ADE035D1121393B4C21FA8D860F515139`。不將啟動存活等同完整視覺／真人驗收。
