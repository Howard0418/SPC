# 驗證紀錄
- 功能 ID：20260929-chemical-dust-monitoring
- 規格版本：1
- 狀態：草稿／待使用者確認

## 本次小工作驗證
| 驗收 ID | 驗證方式 | 結果 |
|---|---|---|
| AC-007 | 建立 SDD 草稿文件後停止，不進入下一個小工作 | 待回報後由使用者確認 |
| AC-001 | 唯讀盤點 SPC 現有 U-chart/C-chart、落塵入口與 AttributeMeasurement 路徑 | 通過；已確認後端具備 C/U 計算器與 Attribute 查詢路徑，網站已有落塵上傳雛形，但資料入口仍需調整 |
| AC-002 | 唯讀盤點 TransFiles 現有 Excel 解析、預覽、上傳與確認流程 | 通過；已確認藥液與咬蝕量路徑分離，落塵需新增獨立分流與 Attribute 上傳契約 |
| AC-003／AC-004 | 唯讀盤點 CA/CPK 計算與顯示位置 | 通過；已確認 CA 在後端計算時取絕對值，CPK/Ppk 直接以 mean 與規格計算，未依賴 CA 欄位 |
| AC-005 | 唯讀盤點既有規格/分段模型與圖表顯示 | 通過；現行僅支援管制界線分段，規格線為 PPC 單一值 |
| AC-006 | 唯讀盤點藥液 F 表公式來源與既有公式保存方式 | 通過；現行有公式 JSON 與來源公式紀錄，但無獨立 F 表/引用索引 |
| AC-003／AC-004 | 新增 CA 負值與 CPK/Ppk 回歸測試，並實作 CA 保留正負號 | 通過；已解除既有測試編譯阻擋並通過 `SpcEngineBaselineTests` |
| AC-001 | 新增 SPC 落塵監控 DUST 群組與 C/U chart 主檔種子 | 通過；`UnitTest1` 驗證空資料庫初始化後有 `DUST_C`、`DUST_U` |
| AC-001 | SPC 管制圖查詢/畫面辨識 DUST 維度 | 通過；後端 build、前端 scope test、前端 build 通過 |
| AC-008 | 若執行環境無法完成測試，需提供使用者可操作的測試方式 | 通過；已納入後續回報規則 |
| AC-001 | SPC Attribute 計算器支援 `DUST_C`/`DUST_U` 產生 C-chart/U-chart | 通過；`SpcEngineBaselineTests` 新增 DUST 圖型代碼測試 |

## 後續驗證計畫
| 驗收 ID | 驗證方式 | 狀態 |
|---|---|---|
| AC-001 | U-chart/C-chart 後端計算測試與前端圖表測試 | 部分通過；後端計算測試通過，前端實資料圖表仍缺落塵量測資料 |
| AC-002 | TransFiles Excel 解析、預覽、匯入、重跑與衝突測試 | 未執行 |
| AC-003 | CA 負值 API/畫面/報表顯示測試 | 未執行 |
| AC-004 | CPK 使用 CA 絕對值的 golden test | 未執行 |
| AC-005 | 7/1、8/1、9/1、10/1 分段規格圖表測試 | 未執行 |
| AC-006 | F 表公式 dry-run、apply、受影響線別查詢測試 | 通過；本地測試與 build 通過，測試站已發布，畫面操作由使用者確認正常 |

## 發布狀態
- 已發布 SPC 測試站 backend/frontend；備份識別 `chemical-dust-20260929-213750`。正式站未發布。

## 小工作 2 盤點結果
- 後端 `AttributeChartCalculator` 已支援 `C`/`C_CHART`/`C-CHART` 與 `U`/`U_CHART`/`U-CHART`。
- `AttributeDataPoint` 與 `AttributeMeasurement` 已有 `DefectCount`、`UnitCount` 欄位，符合 C-chart 與 U-chart 的基本資料欄位。
- `SpcService.GetInteractiveChartAsync` 對 Attribute 類別會讀取 `AttributeMeasurements`，並呼叫 `AttributeChartCalculator.Calculate(...)`。
- `SeedData` 已建立標準 Attribute chart types：`P`、`NP`、`C`、`U`，但尚未看到專屬落塵 `DUST` 群組種子。
- 前端 `VariableUploadView.vue` 已有落塵模式與「落塵監控區域交叉表」解析，但目前送 `/uploads/variable`，不是 Attribute 匯入；且範本解析 R1-R12 的 0.5/1/5/10um，不符合本次固定 1um/5um。
- `SpcChartView.vue` 已能依點位 `uclStat`/`clStat`/`lclStat` 畫動態管制線，對 Attribute 的 C/U 圖可沿用基本圖表顯示。
- 阻擋後續設計的問題：U-chart 的 `UnitCount` 分母來源尚未確認；若固定 1um/5um Excel 沒有檢查單位數/面積/體積欄位，不能正確產生 U-chart。

## 小工作 3 盤點結果
- TransFiles 入口為 `batch_convert_gui.py`；GUI 目前資料類型只有「藥液」與「咬蝕量」。
- 藥液流程會解析日報 Excel，輸出 `批次轉換結果_藥液匯入檔*.xlsx`，欄位為量測日期、量測時間、量測員、類別、線別、槽位、管制項目、量測值、規格、範圍、SamplingPhase 等。
- 藥液「轉換並上傳 SPC 預覽」會呼叫 `/v1/uploads/variable/excel?portalDaily=true`，必要時補建主檔，再由使用者確認 `confirm?mode=upsert`；這是 VariableMeasurement 流程，不適合直接承接落塵 C/U chart。
- 咬蝕量有獨立 `etch_reports.py` 與 `etch_upload.py`，走 Portal 測試站 `/api/etch-import-test/preview`、`/confirm`；此流程固定測試站、Cookie 不落地、必須先預覽，適合作為落塵「preview 再 confirm」安全模式參考，但 API 不可混用。
- 現有 Excel 讀寫使用 `openpyxl`，測試集中在 `test_batch_convert_gui.py`、`test_etch_upload.py`；落塵 parser 可沿用 openpyxl 與既有 unit test 風格。
- 落塵後續實作建議新增獨立資料類型「落塵監控」，解析固定 1um/5um，產出 Attribute 上傳 payload 或呼叫新增的 SPC dust preview API；不可影響既有藥液與咬蝕量輸出。
- 待確認：TransFiles 落塵 Excel 的固定版型、1um/5um 欄位名稱、區域/線別/日期位置、C-chart 與 U-chart 的 `UnitCount` 分母來源。

## 小工作 4 盤點結果
- 後端核心位置：`backend/MesSpc.Api/SpcEngine/Calculators/ProcessCapabilityCalculator.cs`。
- `CapabilityResult.Ca` 目前在雙邊規格時以 `Math.Abs(mean - target) / halfWidth` 計算，因此 API 回傳的 CA 已無正負號。
- `Cpk` 目前以 `min((USL - mean) / (3 * sigmaWithin), (mean - LSL) / (3 * sigmaWithin))` 計算，未讀取 `Ca` 欄位。
- `Ppk` 目前以整體 sigma 的上下側能力取最小值，未讀取 `Ca` 欄位。
- 前端 `SpcChartView.vue` 的能力指標列直接顯示 `chartResult.capability.ca`，`formatNumber` 只做小數格式化，不會取絕對值。
- 前端 `SpcSummaryTable.vue` 直接顯示 `row.ca`，排序也以 `ca` 原值排序；若後端改為負值，前端可顯示負號。
- 後端 `SpcService.GetChartSummaryListAsync` 將 `result.Capability?.Ca` 放入 summary DTO；Excel 總覽 `SpcOverviewReportService` 也直接輸出 `item.Ca`。
- 現有測試 `SpcEngineBaselineTests.Capability_GoldenValues_ShouldSeparateWithinAndOverallSigma` 只驗證 Cpk/Ppk，尚未驗證 CA 正負號；後續應新增平均值低於 target 時 CA 為負、CPK/Ppk 維持既有公式的 golden test。

## 小工作 5 盤點結果
- `PartProcessCharacteristic` 保存單一 `USL`、`LSL`、`TargetValue`。
- `ControlLimitSegment` 只保存 `UCL`、`CL`、`LCL` 與日期區間，未保存 `USL`、`LSL`、`TargetValue`。
- `ControlLimitSegmentsController` CRUD 與前端分段 UI 都只處理管制界線，且有重疊區間防呆。
- `SpcService.GetInteractiveChartAsync` 用 `mapping.USL/LSL/TargetValue` 建立整張圖的 `ControlLimits`；每點只套用分段 UCL/CL/LCL。
- OOS 判定目前以 `mapping.USL/LSL` 判定，未依量測日期切換規格。
- `SpcChartView.vue` 規格線 `USL/LSL/Target` 是靜態 markLine；動態階梯線目前只給 `UCL/CL/LCL`。
- 因此第 4 項需求不能靠現有 `ControlLimitSegments` 完成；需新增規格歷史/分段模型或受控擴充欄位，並同步 OOS、Capability、圖表規格線、summary/export。

## 小工作 6 盤點結果
- SPC 主檔 `PartProcessCharacteristic` 目前有 `FormulaConfigJson` 與 `ChemicalAnalysisConfigJson`，藥液分析公式主要存在 `ChemicalAnalysisConfigJson`。
- `MasterDataV2Controller` 對 `ChemicalAnalysisConfigJson` 只做 JSON 格式清理與保存；目前沒有拆出 F 表儲存格、版本或引用關係。
- 前端 `PartProcessCharacteristicsView.vue` 可編輯藥液分析公式 JSON；`ChemicalAnalysisOverviewView.vue` 可依線別/槽體匯出濃度公式與設定狀態，但不顯示 F 表引用。
- `tools/ChemicalFormulaMigration` 會讀 Portal 的 `ChemicalAnalysisRules.json`，將舊規則轉入 PPC `ChemicalAnalysisConfigJson`；套用時可寫 `migratedFrom`、`migratedAt`，但不是共用 F 表資料模型。
- Portal `ChemicalAnalysisRules.json` 每筆規則有 `SourceFormula` 與 `GlobalValues`，例如來源公式引用 `F!$B$3`，且 `GlobalValues` 保存 `F!B3` 等值。
- Portal `ChemicalAnalysisCatalogService` 目前以檔案載入規則並計算，未提供 F 表儲存格異動影響查詢。
- 因此第 5 項需求需新增 SPC 端 F 表模型/匯入或同步機制、公式引用解析與反查 API，讓修改 `F!B3` 等共用儲存格時可列出受影響線別/槽體/分析項目。
- T-016A 補充：第一版來源建議以 Portal `ChemicalAnalysisRules.json` 匯入 `GlobalValues`，並解析 SPC 既有 PPC `ChemicalAnalysisConfigJson.sourceFormula` 的 `F!` 儲存格引用，建立可查詢的影響索引。
- 使用者已提供 F 表第一版基準資料；後續驗證需確認系統保存 `F!B3`～`F!B10` 的標準液名稱與 F 值，且可由任一儲存格反查引用的藥液線別/槽體/分析項目。
- T-016B 驗證：後端 build 通過；目前僅驗證模型/migration 可編譯，尚未套用資料庫 migration，也尚未驗證 dry-run、apply 與影響查詢。
- 後續需新增驗證：濃度公式包含 `F!B3` 時，計算結果使用目前啟用 F 表的 `F!B3` 值；修改並啟用新版 F 表後，不修改 PPC 公式也能得到新版 F 值計算結果。
- T-016C 驗證：新增單元測試確認 `Primary * F!$B$3` 會正規化為 `F!B3` 並由啟用 F 表取值，結果為 10.2；後端 build 通過。
- T-016C 測試站：已發布 backend；`/api/version` 正常，F 表 API 未帶登入 token 時回 401。使用者可在登入後呼叫 `POST /api/v1/chemical-f-table/sync-default`，先送 `{ "apply": false }` 確認 dry-run，再送 `{ "apply": true, "versionCode": "F-20260930", "displayName": "F 表第一版" }` 套用。
- T-016D 驗證：前端 `npm run build -- --mode testhost` 通過；測試站首頁載入 `index-mBOvEGxD.js`，且該資產指向測試 API `172.16.110.27:8081`。畫面登入後測試仍需使用者操作確認，因我無法代用 AD 帳號登入。
- T-016D 修正驗證：F 表 migration 補上 EF migration id 後 backend build 通過並重新發布測試站；`/api/version` 正常。DB 表存在與登入後套用新版需由使用者在測試站頁面確認。
- T-016D 第二次修正驗證：補 SQL Server 自我修復建立 F 表三張表；backend build 通過並重新發布測試站；`/api/version` 正常。需使用者登入後重試「套用新版」確認。
- T-016D 第三次修正驗證：受影響項目查詢改為分段查詢避免 EF LINQ 轉譯失敗；backend build 與 F 表單元測試通過；已重新發布測試站 backend。
- T-016E 收尾驗證：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter ChemicalFTableServiceTests --no-restore -p:UseSharedCompilation=false` 通過；`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` 通過；`npm run build -- --mode testhost` 通過；測試站 `/api/version` 回 `environment=test`，首頁載入 `index-mBOvEGxD.js`。

## 小工作 9 實作與驗證結果
- 已在 `ai_docs/10_change_log.md` 記錄 CA 顯示保留正負號的變更目的。
- 已新增 `SpcEngineBaselineTests.Capability_Ca_ShouldKeepSignAndLeaveCpkPpkUnchanged`，驗證平均值低於 target 時 CA 為負，且 Cpk/Ppk 仍依既有公式計算。
- 已將 `ProcessCapabilityCalculator` 的 CA 計算由 `Math.Abs(mean - target) / halfWidth` 改為 `(mean - target) / halfWidth`。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore` 通過。
- 已修正既有 `AuthControllerTests` 使用舊 `AuthController` 建構式造成的測試專案編譯阻擋。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter SpcEngineBaselineTests --no-restore` 通過：6 passed。

## 小工作 10 實作與驗證結果
- 已在 `SeedData.Initialize` 新增 idempotent 的落塵監控主檔種子。
- 新增/更新內容：`DUST` 群組、`DUST_C` 落塵缺點數圖、`DUST_U` 落塵單位缺點數圖，資料類別皆為 `Attribute`。
- 此次不建立落塵量測資料，不處理 TransFiles Excel 匯入，不補前端完整落塵查詢畫面。
- 新增 `SeedData_ShouldProvideDustMonitoringCAndUChartTypes` 測試，驗證空資料庫初始化會建立 DUST 群組與 C/U chart type。
- 同步修正既有 `TestImportCustomSpc_SucceedsAndSeedsDatabase` 的藥液 scope 期望值為現行 `CHEM`。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter UnitTest1 --no-restore` 通過：2 passed。

## 小工作 11 實作與驗證結果
- 後端 `ControlScopeCodes` 新增 `DUST` 常數；SPC summary fallback 名稱補為「落塵監控」。
- 前端 `controlScope.js` 正式辨識 `DUST`，並將 `particle`、`dust_monitoring` 視為 DUST 別名。
- `SpcChartView.vue` 的 summary dimension map 與 mapping dimension fallback 補上 DUST，並加入 DUST 維度按鈕配色。
- 新增 `frontend/mes-spc-web/tests/control-scope.test.mjs`，保護 DUST 不會被誤歸到製程。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore` 通過。
- `node --test frontend/mes-spc-web/tests/control-scope.test.mjs` 通過：1 passed。
- `npm run build` 通過。
- 此次仍不含落塵 Excel 匯入、落塵量測資料寫入或實際範例檔驗證。

## 測試站發布結果
- 發布範圍：`D:/SPC/release/test/backend`、`D:/SPC/release/test/frontend`。
- 備份：`backend.backup-chemical-dust-20260929-213750`、`frontend.backup-chemical-dust-20260929-213750`。
- 發布時間：2026-09-29 21:38:53。
- 保留：後端 `appsettings.json`、`web.config`、`private-data`；前端 `web.config`。
- Smoke：`http://172.16.110.27:8081/api/version` 回 `{"version":"0.1.59","environment":"test"}`。
- Smoke：`http://172.16.110.27:8081/health` HTTP 200。
- Smoke：`http://172.16.110.27:8083/` HTTP 200 且含前端 app root。
- 注意：測試站 `SeedDatabase=false`，本次發布不會自動執行 `SeedData.Initialize`；目前執行環境無法連線 `PMR_SPC_TEST` 直接確認/套用 DUST 主檔資料。若測試站尚未有 `DUST` 群組，需另行授權受控資料套用。
- 正式站未發布；Portal 未發布。

## 小工作 12 測試庫 DUST 主檔確認/套用結果
- 目標：確認測試庫是否已有 `DUST` 群組與 `DUST_C`/`DUST_U` chart type，必要時套用。
- 結果：未完成資料庫確認/套用；未修改資料庫。
- 限制：測試站 API 查詢群組需登入，匿名呼叫回 HTTP 401。
- 限制：本機以 sqlcmd、PowerShell SqlClient 與既有 .NET 工具連線 `PMR_SPC_TEST` 皆未成功，無法直接查詢或寫入。
- 補救：已新增冪等 SQL `ensure-dust-master.sql`，只更新/建立 `ControlChartGroups.GroupCode = DUST` 與 `ControlChartTypes.ChartTypeCode in (DUST_C, DUST_U)`。
- 下一步：需由具測試庫連線權限的環境執行 SQL，或提供可登入測試站 API/DB 的受控方式後再確認；若仍由使用者端驗證，需提供可操作測試步驟與預期結果。
- 使用者端確認：2026-09-30 使用者回報測試庫已看到兩筆落塵監控資料，視為測試庫 `DUST_C`、`DUST_U` 主檔可見。

## 小工作 13 SPC 落塵 C/U 圖型計算結果
- 已讓 `AttributeChartCalculator` 接受 `DUST_C` 並輸出既有 `C_CHART` 結果。
- 已讓 `AttributeChartCalculator` 接受 `DUST_U` 並輸出既有 `U_CHART` 結果。
- 已讓 `SpcService.CalculateAttributeAsync` 的單筆統計值計算同步支援 `DUST_C`/`DUST_U`。
- 新增測試 `DustAttributeChartCodes_ShouldMapToCAndUCharts`。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter SpcEngineBaselineTests --no-restore` 通過：7 passed。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` 通過；原平行 build 因 `VBCSCompiler` 鎖定輸出檔失敗，重跑後成功。
- 已發布 SPC 測試站 backend；備份 `backend.backup-dust-chart-codes-20260930-080553`，備份不含受權限保護的 `private-data`。
- Smoke：`http://172.16.110.27:8081/api/version` 回 `{"version":"0.1.59","environment":"test"}`。
- Smoke：`http://172.16.110.27:8081/health` HTTP 200。
- 無法由我完成項目：測試站前端實資料圖表，因目前尚無落塵量測資料/落塵 Excel 匯入結果。
- 使用者可測方式：匯入或建立至少一筆 `DUST_C` 對應 PPC 的 AttributeMeasurement，欄位含 `DefectCount`；查詢 SPC 管制圖應顯示 `C_CHART` 點位。
- 使用者可測方式：匯入或建立至少一筆 `DUST_U` 對應 PPC 的 AttributeMeasurement，欄位含 `DefectCount` 與大於 0 的 `UnitCount`；查詢 SPC 管制圖應顯示 `U_CHART` 點位，點值為 `DefectCount / UnitCount`。

## 落塵 Excel 範例檔初步盤點
- 來源檔：`D:\SPC\docs\PD-3-257-04A-製造部落塵監控表單(黃光室) - 2026.xlsx`。
- 工作表：`PD-3-257-04A (0.51.0 5 10)`。
- 資料列：第 26 列起；B 欄為日期時間。
- 區域欄位：R1～R12；每區有 0.5um、1um、5um、10um。
- 本案取用欄位：0.5um、1um、5um、10um。
- 粒徑欄位地圖：R1=C/D/E/F、R2=G/H/I/J、R3=K/L/M/N、R4=O/P/Q/R、R5=S/T/U/V、R6=W/X/Y/Z、R7=AA/AB/AC/AD、R8=AE/AF/AG/AH、R9=AI/AJ/AK/AL、R10=AM/AN/AO/AP、R11=AQ/AR/AS/AT、R12=AU/AV/AW/AX。
- 已確認：R1～R12 為區域，與機台無關，可作為 SPC 管制項目名稱。
- 已確認：U-chart `UnitCount` 先固定 1；後續需提供可修改設定點。

## 小工作 14 TransFiles 落塵轉檔結果
- TransFiles 新增 `dust_reports.py`，集中管理落塵版型解析與 `DUST_UNIT_COUNT=1` 設定點。
- GUI 新增資料類型「落塵監控」，掃描檔名含「落塵」的 Excel。
- 輸出 Excel 工作表：`計數型資料匯入`。
- 輸出欄位：`管制類型`、`製程`、`機台`、`檢驗項目`、`總數`、`不良數`、`缺點數`、`單位數`、`日期`、`作業員`、`lot`、`樣本編號`。
- 實測來源：`D:\SPC\docs\PD-3-257-04A-製造部落塵監控表單(黃光室) - 2026.xlsx`。
- 實測輸出：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\TransFiles\批次轉換結果_落塵監控_計數型匯入檔_測試.xlsx`。
- 實測筆數：33,703 筆；其中 1um、5um、10um 各 8,426 筆，0.5um 為 8,425 筆。
- 單元測試：`python -m unittest test_batch_convert_gui.py` 通過 29 項。
- 未完成測試：未上傳/確認到 SPC，因本小工作只做轉檔，且測試庫仍需確認 `DUST` 製程與 `R#_1um`/`R#_5um` 管制項目。
- 使用者可測方式：在 TransFiles GUI 選「落塵監控」與來源資料夾，執行「開始轉換」，預期產出 `批次轉換結果_落塵監控_計數型匯入檔.xlsx`，表頭與上列輸出欄位一致，筆數約 33,703。

## 小工作 14A TransFiles 落塵預覽/確認匯入結果
- TransFiles「轉換並上傳 SPC 預覽」已支援「落塵監控」，上傳端點改用 `/v1/uploads/attribute/excel`。
- 落塵匯入檔 `總數` 與 `單位數` 皆填入 `DUST_UNIT_COUNT=1`，此設定集中於 `dust_reports.py` 供後續調整。
- SPC 後端補主檔流程已將 `DUST` 視為不需機台；若預覽缺少 `R#_0.5um`、`R#_1um`、`R#_5um`、`R#_10um` 管制項目，會建立 Attribute 品質特性與 `DUST_U` PPC。
- DUST seed 與 `ensure-dust-master.sql` 已改為 `RequiresMachine=0`，描述同步四種粒徑。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter "SeedData_ShouldProvideDustMonitoringCAndUChartTypes|SpcEngineBaselineTests" --no-restore -p:UseSharedCompilation=false` 通過：8 passed。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` 通過；有 NuGet 弱點來源離線警告，非編譯錯誤。
- `python -m unittest test_batch_convert_gui.py` 通過：29 passed。
- 實測抽查來源 Excel 轉換筆數：33,703；1um、5um、10um 各 8,426 筆，0.5um 8,425 筆。
- 已發布 SPC 測試站 backend；備份 `backend.backup-dust-attribute-upload-20260930-084101`。
- Smoke：`http://172.16.110.27:8081/api/version` 回 `{"version":"0.1.59","environment":"test"}`。
- Smoke：`http://172.16.110.27:8081/health` HTTP 200。
- 無法由我完成項目：需登入 SPC 測試站的帳密與 GUI 互動，因此未直接按下 TransFiles 的正式確認匯入。
- 使用者可測方式：開啟 TransFiles GUI，選「落塵監控」，來源資料夾放入 `PD-3-257-04A-製造部落塵監控表單(黃光室) - 2026.xlsx`，按「轉換並上傳 SPC 預覽」並登入測試站。
- 預期結果：預覽建立後補主檔重新驗證，錯誤數應為 0；對話框選擇確認匯入後，SPC 測試站可用 `DUST` 維度查詢 `R1_0.5um`、`R1_1um`、`R1_5um`、`R1_10um` 等 U-chart 點位。

## 小工作 14B TransFiles Portal SSO 登入修正
- 問題：使用者以 AD 帳密在 TransFiles 登入 SPC 測試站失敗。
- 根因：TransFiles 原呼叫 SPC `/api/v1/auth/login`；SPC 後端已停用本機密碼登入，此端點固定回 410。
- 修正：TransFiles 改沿用 Portal 正規 SSO；先登入 Portal 測試站，再呼叫 `/api/spc-launch` 取得 SPC token。
- `python -m unittest test_etch_upload.py test_batch_convert_gui.py` 通過：35 passed。
- 無法由我完成項目：需使用者真實 AD 帳密與 Portal 品保權限，未做真人登入。
- 使用者可測方式：重新開啟 TransFiles，選「落塵監控」，按「轉換並上傳 SPC 預覽」，在 Portal 登入視窗輸入 AD 帳號或工號與密碼。
- 預期結果：不再顯示 SPC 舊登入失敗；若帳號有品保與 SPC 權限，工具會完成 SPC 主檔同步並建立預覽。

## 小工作 14C TransFiles EXE 打包結果
- 已重新打包 `dist/BatchConvertTool.exe`，並覆蓋主執行檔 `藥液分析轉檔工具.exe`。
- 舊主執行檔備份：`藥液分析轉檔工具.exe.bak-before-dust-sso-20260930-104044`。
- `python -m unittest test_etch_upload.py test_batch_convert_gui.py` 通過：35 passed。
- PyInstaller 打包完成；因本機 Python/Tk 偵測訊息曾出現 tkinter 警告，已用新版 EXE 啟動存活檢查，5 秒未退出。
- 新版 EXE SHA256：`6A37BC7F2A34CA371D45D56216A2EEB3A8D53B31A6E0CAFA4F92DB7B7B74EB9E`。
- `dist/chemical_master_mapping.json` 已同步。
- 無法由我完成項目：真人 AD/Portal 登入與 SPC 預覽/確認匯入。
- 使用者可測方式：執行 `藥液分析轉檔工具.exe`，選「落塵監控」，按「轉換並上傳 SPC 預覽」，登入後預期建立 SPC Attribute 預覽。

## 小工作 14D SPC 預覽頁 SSO token 開啟修正
- 現象：使用者測試匯入時，SPC 預覽頁顯示「送出映射數據失敗：無法連線到後端 API」。
- 檢查：`http://172.16.110.27:8081/api/version` 回 200/test；`OPTIONS create-missing-mappings` 回 204，CORS 正常；未帶 token 呼叫寫入端點回 401，後端服務正常。
- 判斷：TransFiles 取得 SPC token 後只用於工具端 API 呼叫，開啟瀏覽器預覽頁時未把 token 帶給 SPC Web。
- 修正：開啟預覽頁改用 `SPC /portal-sso#token=...&target=...`，讓瀏覽器取得同一個 SPC token 後再進預覽頁。
- `python -m unittest test_etch_upload.py test_batch_convert_gui.py` 通過：36 passed。
- 新版主 EXE：`藥液分析轉檔工具.exe`。
- 備份：`藥液分析轉檔工具.exe.bak-before-preview-token-20260930-105813`。
- 新版 EXE SHA256：`B300530689F83741C85D691149808E7751281647A0FF1D08C2802FE96C53FE1C`。
- 啟動存活檢查：5 秒未退出。
- 使用者可測方式：重新執行主 EXE，完成落塵上傳後選擇開啟預覽批次；預期瀏覽器先進 `/portal-sso` 後跳到 `/uploads/{batchId}/preview`，不再出現後端 API 無法連線訊息。
