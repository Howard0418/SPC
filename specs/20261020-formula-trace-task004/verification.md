# TASK-004 驗證（2026-10-20）

狀態：實作、自動化驗收及測試站發布完成；真人正常SSO業務驗收待依[Antigravity案例](antigravity-tests.md)操作，不列為已通過。正式站／TransFiles／KM未操作。

## 驗收與命令
- AC-01～04：`ChemicalPortalMeasurementTests` 7項涵蓋可信結果覆蓋偽造值、初複驗兩筆trace、actor綁定冪等、缺raw／改日期、同日更新沿用ID與追加歷史、混合舊項目及交易回復。核心先紅後綠。
- AC-05～07：controller確認限制Admin／Editor，Portal缺個人token拒絕、保留403／409；raw1.004／stored1／USL1.002須NG，統計仍1、警報ActualValue=1.004、只在commit後通知。
- `dotnet test tests/MesSpc.Api.Tests --filter "ChemicalPortalMeasurementTests|ChemicalFormulaScheduleTests|SpcPointExclusionCalculationTests|ChartSegmentDetailsTests" -p:UseSharedCompilation=false`：最終22 passed。
- `dotnet test tests/MesSpc.Chemical.Tests`：59 passed（藥液／咬蝕／日期與階段回歸）。
- 擴大SPC回歸96 passed／1 failed／1 opt-in skip：失敗`DeleteItem_RemovesPortalDailyMeasurementAndDerivedRecords`為既有EF InMemory不支援交易警告，該controller/test未修改；既有同項基準見[先前驗證](../20261019-version-history-date-filter/verification.md)。不將全套宣稱通過。
- Portal `dotnet test tests/PmrPortal.Api.Tests --filter "ChemicalEffectiveProxyTests|SpcProxy|ChemicalAnalysisCatalog"`：18 passed。
- Node精度／歷史／新token helper共14 passed；`node tests/chemical-effective-ui.cjs` 5、既有stage 4與calendar 3瀏覽器案例通過。涵蓋日期與原字串、初複驗確認、清除過期請求、409保留草稿、載入歷史不重算及明確重新預覽。
- `tools/testing/Invoke-ChemicalTraceSchemaRehearsal.ps1 -TestFilter ChemicalPortalMeasurementSqlTests`：完整SQL scenario passed。COPY_ONLY/CHECKSUM/VERIFYONLY後還原隔離庫`SPC_FORMULA_SCHEMA_VERIFY_20261020_f4805433`；只啟用隔離合成PPC，並發同requestId只產生一批；故障注入至複驗trace時，先前receipt／初驗trace／量測更新全回復。測試trigger最後移除，未在共用庫建立。
- SPC Release publish、Portal API/Web build/publish成功；既有Pkcs套件弱點警告保留，不擴張套件修補。
- Portal `tests/verify-publish-settings.ps1 -ProjectName PmrPortal.Api`及Web：隔離發布PASS。舊時間戳設定仍保留，來源額外appsettings不導入，web.config不覆蓋；原publisher覆蓋問題已先證實。

## 計算／儲存證據
- `ChemicalPortalMeasurementService.ConfirmAsync`：外層Serializable交易、request app lock及排序PPC lock，沿用UploadService；所有列通過才commit，commit後才通知。既有EF audit將CreatedBy寫System，因此request hash綁定已認證actor，不用client operator作授權。
- `UploadService` evidence hook沿用日報upsert，receipt／trace追加保存；token在staging前移除、不存UploadDetails。`SpcService`讀已保存raw結果判規格，不重新套最新公式，統計保持既有儲存值。
- `ChemicalFormulaScheduleService`遇外層交易沿用，不提前commit。無新migration、不開啟共用policy、不回填歷史、不將目前公式冒充實際版本。

## 測試發布與回復
- SPC測試API **0.1.65**／Web **0.1.68**；Portal API **1.0.183.0**／Web **1.0.226.0**。
- 核對實際IIS：SpcSingleTest=`D:/SPC/release/test/backend`；PmrPortalApi/Web=`D:/PmrPortal/publish/portal-api`、`portal-web`，Portal正式/測試以health.dbEnvironment=test核對。Portal僅由既有`scripts/publish-all.ps1`發布。
- 應用備份：`D:/SPC/release/backup-formula-task004-20261020-131536`、`D:/PmrPortal/tmp_publish/backup-formula-task004-20261020-131536`。
- 測試DB備份：SQL主機預設備份目錄`SPC_TASK004_TEST_PRE_20261020-131536.bak`，COPY_ONLY/CHECKSUM且RESTORE VERIFYONLY成功。
- 發布前後appsettings*.json／web.config／uploads與private-data的檔案集合及hash一致；migration數、receipt數、既有量測／公式／F歷史指紋一致。未跑新migration，正式環境未操作。
- 發布後SPC `/api/health`=200/ok/test/0.1.65；Portal `/app-version`=200/1.0.226.0、新helper JS=200；兩站preview／confirm匿名均401，SPC schedules匿名401。
- rollback：停對應測試應用，從上述應用備份還原DLL／Web，保留環境設定及業務資料；無schema需Down。若後續開啟policy，不得回退成舊Portal繼續寫該項目；需先停止該入口。此次未執行rollback。

## 限制及下一步
- 實際Viewer登入403及正常SSO跨站新增／複驗／圖表真人操作仍待人工案例；自動化權限／回歸不冒充真人完成。
- 真正TransFiles DB直寫／consumer是TASK-005，圖表呈現逐點實際trace是TASK-006；本次停止。剩5件＝SPC2＋KM暫緩3。

## 提交／上傳狀態
- SPC提交`5747caa5`已上傳origin/Develop。Portal本機提交`f4f95d0`完成；推送至`https://github.com/Howard0418/WebSiteApi.git` main遭自動核准審查拒絕（無該目的地及精確推送的明確授權、所有權未確認），已請使用者核准，未繞過。測試站發布已完成、不受影響。
