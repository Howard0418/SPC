# 客製需求與回歸檢查

## 2026-10-06（SPC 藥液公式批次儲存結果列）
- 藥液總覽頁批次儲存後會顯示成功/失敗筆數摘要。
- 表格新增「儲存結果」欄，成功列顯示「已儲存」，失敗列顯示錯誤訊息並保留草稿供修正。
- 儲存完成後會重新載入資料；仍沿用既有單筆 PUT 與公式版本紀錄，未新增後端 API。
- 驗證：前端 `npm run build -- --mode testhost` 通過，產出 `index-7pDwhRVl.js`、`index-B3nZcc2H.css`。本階段未發布測試站，正式站未發布。

## 2026-10-06（SPC 藥液公式總覽批次儲存確認）
- 藥液總覽頁新增「儲存變更」按鈕，僅在有異動列時可送出，並顯示異動筆數。
- 儲存前會顯示線別、槽位與分析項目的變更摘要；儲存時只逐筆送出 dirty rows，沿用既有 `PUT /part-process-characteristics/{id}` 與公式版本紀錄。
- Payload 保留原主檔欄位，只替換 `chemicalAnalysisConfigJson`；本階段未新增後端 API，詳細成功/失敗列結果留待後續 TASK。
- 驗證：前端 `npm run build -- --mode testhost` 通過，產出 `index-KMWpI8dd.js`、`index-pAvY4q_l.css`。本階段未發布測試站，正式站未發布。

## 2026-10-06（SPC 藥液公式總覽 draft 與異動標示）
- 藥液總覽頁新增公式欄位草稿編輯，支援濃度公式、調整公式、調整量公式與小數位。
- 已變更列會以底色與「已變更」標籤提示，並提供單列還原；本階段尚未送出批次儲存 API。
- 匯出 Excel 補上調整公式、調整量公式、小數位與是否變更欄位。
- 驗證：前端 `npm run build -- --mode testhost` 通過，產出 `index-CodgJfD4.js`、`index-CWZtvWtJ.css`。本階段未發布測試站，正式站未發布。

## 2026-10-06（SPC 藥液公式總覽批次儲存規格）
- 規劃藥液公式總覽與批次儲存頁，目標是在既有藥液總覽頁直接核對與編輯所有 CHEM 線別、槽位、分析項目的公式。
- 最小方案：第一版不新增後端批次 API，由前端只針對異動列逐筆呼叫既有 `PUT /part-process-characteristics/{id}`，沿用已完成的公式版本紀錄與回復能力。
- 文件：新增 `specs/20261006-chemical-formula-overview-batch/`。本階段未修改產品程式、未建置、未發布。

## 2026-10-06（SPC 藥液公式版本測試站發布）
- 藥液公式版本紀錄與回復已發布 SPC 測試站 backend/frontend；正式站未發布。
- 備份：`backend.backup-chemical-formula-versioning-20261006-130824`、`frontend.backup-chemical-formula-versioning-20261006-130824`。
- 驗證：後端 Release publish 通過；前端 `npm run build -- --mode testhost` 通過；測試站 `/api/version` 回 200/test；首頁回 200/text-html；新版 JS `index-DiDDfgPE.js` 回 200/application-javascript；版本查詢/回復端點未登入回 401。
- 修正發布過程：初次手動複製前端 assets 位置不正確，造成 JS asset 回 HTML；已修正為複製 `dist/assets/*` 至測試站 `assets` 目錄並重測通過。

## 2026-10-06（SPC 藥液公式版本前端驗證）
- 藥液公式版本紀錄與回復前端完成 build 與靜態檢查。
- 確認管制項目編輯頁存在「版本紀錄」入口、版本查詢 API 呼叫、「回復此版」按鈕與版本回復 API 呼叫。
- 驗證：前端 `npm run build -- --mode testhost` 通過，產出 `index-DiDDfgPE.js`、`index-CIBT_RHI.css`。
- 範圍：本階段不發布測試站。正式站未發布。

## 2026-10-06（SPC 藥液公式版本回復操作）
- 藥液公式版本紀錄清單新增「回復此版」操作，送出前會提示使用者確認。
- 回復成功後會重新載入主檔資料與版本紀錄，並更新目前編輯表單中的藥液公式內容。
- 驗證：前端 `npm run build -- --mode testhost` 通過。
- 範圍：本階段不發布測試站。正式站未發布。

## 2026-10-06（SPC 藥液公式版本紀錄入口）
- 藥液管制項目編輯 modal 的「藥液分析公式」區塊新增「版本紀錄」按鈕，可載入並顯示目前項目的公式版本清單。
- 清單顯示版本號、修改/回復、異動時間、異動人、原因與公式摘要；本階段尚未提供回復操作。
- 驗證：前端 `npm run build -- --mode testhost` 通過。
- 範圍：本階段不發布測試站。正式站未發布。

## 2026-10-06（SPC 藥液公式版本後端測試補齊）
- 補齊藥液公式版本相關後端測試，涵蓋資料模型、公式修改建版、公式無變更不建版、非 CHEM 拒絕、回復版本與回復端點授權角色。
- 驗證：`ChemicalAnalysisFormulaVersion` 相關測試 10 passed；後端 build 0 warnings / 0 errors。
- 範圍：本階段只補測試與文件，不改產品功能行為、不發布測試站。正式站未發布。

## 2026-10-06（SPC 藥液公式版本查詢與回復 API）
- 新增藥液公式版本 API，可查詢指定管制項目的公式版本清單，並回復指定版本。
- 回復 API 沿用 `ChemicalAnalysisFormulaVersionService.RestoreAsync`，會更新目前 `ChemicalAnalysisConfigJson` 並新增一筆 `Restore` 紀錄。
- 回復端點限制 `Admin,Editor`；非 CHEM 管制項目回 400，找不到資料回 404。
- 驗證：Controller/Service 測試共 7 passed；後端 build 0 warnings / 0 errors。
- 範圍：本階段不改前端、不發布測試站。正式站未發布。

## 2026-10-06（SPC 藥液公式修改自動建立版本）
- 既有 `PUT /part-process-characteristics/{id}` 已整合公式版本服務；藥液 `ChemicalAnalysisConfigJson` 變更後會自動新增版本紀錄。
- 公式內容未變更時不新增重複版本；非藥液主檔未變更公式時不受影響。
- 驗證：公式版本服務與主檔更新整合測試共 5 passed；後端 build 0 warnings / 0 errors。
- 範圍：本階段不新增 API、不改前端、不發布測試站。正式站未發布。

## 2026-10-06（SPC 藥液分析公式版本服務）
- 新增 `ChemicalAnalysisFormulaVersionService`，集中處理藥液公式變更比對、版本建立與指定版本回復。
- 公式內容未變更時不新增重複版本；非 CHEM 主檔會拒絕建立藥液公式版本。
- 回復指定版本時會更新目前 `ChemicalAnalysisConfigJson`，並新增一筆 `Restore` 版本紀錄保留前後內容與來源版本。
- 驗證：`ChemicalAnalysisFormulaVersionServiceTests` 4 passed；後端 build 0 warnings / 0 errors。
- 範圍：本階段尚未接 API、未改前端、不發布測試站。正式站未發布。

## 2026-10-06（SPC 藥液分析公式版本資料模型）
- 新增 `ChemicalAnalysisFormulaVersion` 後端 entity、`ChemicalAnalysisFormulaVersions` DbSet、EF mapping 與 migration，用於保存藥液分析公式版本、前一版內容、修改人、修改時間、原因與回復來源。
- 新增模型 mapping 測試，確認資料表名稱、欄位長度與 `(PartProcessCharacteristicId, VersionNo)` 唯一索引。
- 驗證：`ChemicalAnalysisFormulaVersionModelTests` 1 passed；後端 build 0 warnings / 0 errors。
- 範圍：本階段只建立資料模型，不接 API、不改前端、不發布測試站。正式站未發布。

## 2026-10-06（SPC 藥液分析公式版本記錄規格）
- 依使用者需求規劃藥液分析公式版本記錄與回復。
- 現況：公式目前存在 `PartProcessCharacteristics.ChemicalAnalysisConfigJson`，由 `PartProcessCharacteristicsView.vue` 單筆編輯後透過 `PUT /part-process-characteristics/{id}` 覆蓋設定；目前無公式版本歷史或回復 API。
- 規劃：新增獨立公式版本表、版本查詢 API、指定版本回復 API，並在單筆公式編輯流程中建立版本紀錄。
- 文件：新增 `specs/20261006-chemical-formula-versioning/`。本階段未修改產品程式、未建置、未發布。

## 2026-10-06（SPC-POINT-FILTER 文件同步與收尾）
- 已完成 SPC 管制圖/趨勢圖點位排除與隱藏恢復整串小工作文件同步。
- 管制圖與趨勢圖右鍵單點排除、已排除點清單、隱藏點恢復、後端回歸、前端 build/靜態 UI 檢查與測試站 smoke test 均已記錄完成。
- 正式站未發布；既有 Playwright 測試落差已保留於驗證紀錄，待後續另行校正。

## 2026-10-06（SPC-POINT-FILTER 測試站總 smoke test）
- SPC 測試站 frontend/backend 總驗證完成；本階段未修改產品程式、正式站未發布。
- 前端首頁 HTTP 200，`text/html`。
- 新版 JS `index-DKfYYFj2.js` HTTP 200，`application/javascript`。
- 後端 `/api/version` HTTP 200，`environment=test`。
- `release/test/backend/app_offline.htm` 不存在。

## 2026-10-06（SPC-POINT-FILTER 前端 build / UI 測試）
- 前端 `npm run build:test` 通過，產出 `index-DKfYYFj2.js` 與 `index-CVlQs-Gh.css`。
- 靜態檢查確認管制圖與趨勢圖皆有已排除點清單入口、恢復函式與 `point-exclusions` API 呼叫。
- `spc-ui.spec.ts` / `spc-summary.spec.ts` Playwright 既有測試 5 failed，失敗點為版本文字、ModuleGuide 初始狀態、舊管制圖 mock 與總表按鈕/select 等既有測試落差；未指向本次新增的排除清單 testid。
- 本階段未修改產品程式、未發布測試站。

## 2026-10-06（SPC-POINT-FILTER 後端回歸測試）
- 驗證單點排除後端口徑、趨勢圖 normality 與 Xbar 代表案例；本階段未修改產品程式、未發布測試站。
- `SpcPointExclusionCalculationTests`、`NormalityTest` 與 Xbar 代表案例共 8 passed。
- 後端 build 0 warnings / 0 errors。
- 備註：第一次 build 與 test 並行時遇到 DLL 檔案鎖定，單獨重跑 build 後通過。

## 2026-10-06（SPC-POINT-FILTER 趨勢圖已排除點清單）
- 趨勢圖頁新增「已排除點 N」清單，可查看目前 PPC 的 active 排除點。
- 清單可逐筆恢復 `ExcludedVisible` / `ExcludedHidden` 點；`ExcludedHidden` 點會從趨勢線上隱藏，但仍可由清單恢復。
- 本次沿用既有 `point-exclusions` API，不改資料庫與後端契約。
- 驗證：前端 `npm run build -- --mode testhost` 通過。
- 已發布 SPC 測試站 frontend，備份 `frontend.backup-trend-excluded-list-20261006`；Smoke：首頁 200，新 JS `index-DKfYYFj2.js` 回 `application/javascript`。正式站未發布。

## 2026-10-06（SPC-POINT-FILTER 趨勢圖右鍵單點排除）
- 趨勢圖點位新增右鍵選單，可設定「顯示但不列入計算」、「隱藏且不列入計算」與「恢復列入計算」，並呼叫單點 `point-exclusions` API。
- 趨勢圖圖例補上已排除點位，已排除點沿用灰色叉號樣式。
- 驗證：前端 `npm run build -- --mode testhost` 通過。
- 已發布 SPC 測試站 frontend，備份 `frontend.backup-trend-point-context-menu-20261006`；Smoke：首頁 200，新 JS `index-KEeRhtLO.js` 回 `application/javascript`。正式站未發布。
- 趨勢圖已排除點清單與隱藏點恢復入口留待後續小工作。

## 2026-10-06（SPC-POINT-FILTER 點位排除/隱藏規劃）
- 依使用者需求規劃管制圖與趨勢圖量測點右鍵選單，支援「顯示但不列入計算」與「隱藏且不列入計算」。
- 已確認隱藏點需提供恢復入口：圖表工具列「已排除點 N」清單與 raw data/明細清單皆可恢復，避免隱藏後找不到。
- 現況分析：目前只有 `UploadBatches.IsExcluded` 整批排除；管制圖明細已有剔除/恢復按鈕但作用在整批，不是單一點；趨勢圖可顯示 `isExcluded` 但沒有右鍵操作。
- 文件：新增 `specs/20261006-chart-point-exclusion/`，並更新 `TODO.md` 與需求索引。本次未修改產品程式、未建置、未發布。

## 2026-10-06（SPC-POINT-FILTER 單一點位排除 API）
- 新增 `SpcPointExclusion` 資料模型與 `SpcPointExclusions` migration，保留原始量測資料不變，以獨立狀態記錄單一圖點排除/隱藏/恢復。
- 新增 `GET/PUT/DELETE /api/v1/spc/point-exclusions`，支援查詢已排除點、設定 `ExcludedVisible` / `ExcludedHidden`、恢復列入計算；寫入與恢復限制 `Admin,Editor`。
- 驗證：`SpcPointExclusionsControllerTests` 4 passed；後端 build 0 warnings / 0 errors。
- 已發布 SPC 測試站 backend，備份 `backend.backup-spc-point-exclusions-20261006`；Smoke：`/api/version` 200/test，未登入寫入 API 401。正式站未發布。

## 2026-10-06（SPC-POINT-FILTER 管制圖計算套用單點排除）
- `SpcService.GetInteractiveChartAsync` 讀取 active `SpcPointExclusions`，變量型管制圖 raw point 與 Xbar 子組開始套用單點排除。
- 保留既有 `UploadBatches.IsExcluded` 整批排除；單點排除只與整批排除 OR 合併，不會解除整批排除。
- 趨勢圖/直方圖共用的 normality 資料口徑已確認使用未排除點；Attribute chart 補齊 `AttributeMeasurement` 單點排除與 `AttributeMeasurementId` 回傳。
- 驗證：`SpcPointExclusionCalculationTests` 2 passed；後端 build 0 warnings / 0 errors。
- 已發布 SPC 測試站 backend，備份 `backend.backup-spc-point-exclusion-calc-20261006`、`backend.backup-spc-point-exclusion-trend-20261006`；Smoke：`/api/version` 200/test，`app_offline.htm` 已移除。正式站未發布。
- 本階段尚未實作前端右鍵選單、隱藏點不渲染與已排除點清單。

## 2026-10-06（SPC-POINT-FILTER 管制圖右鍵單點排除）
- 管制圖點位新增右鍵選單，可設定「顯示但不列入計算」、「隱藏且不列入計算」與「恢復列入計算」，並呼叫單點 `point-exclusions` API。
- Attribute chart `chartData.points` 補帶 `attributeMeasurementId`，讓右鍵選單可定位 Attribute 單點。
- 圖例補上已排除點位，已排除點沿用灰色叉號樣式。
- 驗證：前端 `npm run build -- --mode testhost` 通過；後端 `SpcPointExclusionCalculationTests` 2 passed；後端 build 0 warnings / 0 errors。
- 已發布 SPC 測試站 frontend/backend，備份 `frontend.backup-spc-point-context-menu-20261006`、`backend.backup-spc-point-context-menu-20261006`；Smoke：首頁 200，新 JS `index-93uPHtFu.js` 回 `application/javascript`，`/api/version` 200/test。正式站未發布。
- 已排除點恢復清單與真正隱藏點的清單恢復留待後續小工作。

## 2026-10-06（SPC-POINT-FILTER 管制圖已排除點清單）
- 管制圖工具列新增「已排除點 N」清單，查圖成功後同步載入目前 PPC 的 active 排除點。
- 清單可逐筆恢復 `ExcludedVisible` / `ExcludedHidden` 點，避免隱藏點找不到恢復入口。
- 驗證：前端 `npm run build -- --mode testhost` 通過。
- 已發布 SPC 測試站 frontend，備份 `frontend.backup-spc-excluded-list-20261006`；Smoke：首頁 200，新 JS `index-Z4tE0lCl.js` 回 `application/javascript`。正式站未發布。

## 2026-10-05（SPC 測試問題：總覽下拉與資料數口徑）
- 依 `docs/SPC測試問題_20261005.xlsx` 修正兩項測試問題。
- 製程總覽線別下拉排除無效 process 資料，避免混入「製程、檢驗項目、總數、日期、作業員、lot、樣本編號」等非線別項目。
- 總表 `匯入資料數` 僅針對製程 `PROC` 改以 chart points 管制點數為主，Xbar 類項目不再顯示 raw sample 數造成與管制圖明細口徑不一致；藥液 `CHEM` 維持原 raw data 筆數。
- 後端 build 0 warnings / 0 errors；前端 testhost build 通過；已發布 SPC 測試站 backend/frontend，備份 `backend.backup-test-issues-20261005-144947`、`frontend.backup-test-issues-20261005-144947`。正式站未發布。
- 2026-10-05 追加收斂修正：依使用者提醒，重新發布 backend，確認 `TotalCount` 口徑修正只影響製程，不影響藥液功能與資料計算；備份 `backend.backup-proc-total-count-20261005-150540`。
- 2026-10-05 再修正：製程標準代碼為 `PROCESS`，`TotalCount` 已改用標準化 control scope 判斷，避免 `PROC`/`PROCESS` 別名差異造成製程總覽仍顯示 raw data 筆數；藥液 `CHEM` 維持原邏輯。後端 build 通過並已發布測試站 backend，備份 `backend.backup-process-total-count-scope-170030`。
- 2026-10-05 OOS 再確認：製程總覽 `OosCount/OocCount` 同樣改用 `chartData.points` 的 `outOfSpec/outOfControl/violatedRules` 點位旗標計算，百分比分母使用製程管制點數；藥液 `CHEM` 維持 raw data 口徑。後端 build 通過並已發布測試站 backend，備份 `backend.backup-process-oos-scope-193639`。

## 2026-10-05（SPC-1002-TASK-006 趨勢圖直方圖）
- 趨勢圖頁新增 raw data 分布直方圖、常態分布曲線、P-value、偏態、峰度與常態判定，沿用既有 `/v1/spc/chart` 回傳的 normality/normalCurve。
- 趨勢圖直方圖只標示 LSL/USL/Target/Mean，不顯示 UCL/LCL/CL，不套用 OOC 規則管理。
- 前端 testhost build 通過；已發布 SPC 測試站 frontend，備份 `frontend.backup-trend-histogram-203651`；首頁 HTTP 200，新版 JS `index-DVVeXuQK.js` 回 `application/javascript`。正式站未發布。

## 2026-10-05（SPC-1002-TASK-005 raw data 下載）
- 新增管制圖 raw data CSV 下載端點與前端下載按鈕，沿用目前管制圖查詢條件與 93 天限制，支援製程/藥液 raw data 對帳。
- CSV 包含量測時間、日報日期、班別、取樣階段、量測值、批號、線別/槽位 ID、板面、OOS/OOC 等欄位；不改資料庫、不改統計計算。
- 後端 build 0 warnings / 0 errors；前端 testhost build 通過；未登入 raw data 端點回 401。
- 已發布 SPC 測試站 backend/frontend，備份 `backend.backup-raw-data-download-20261005-135341`、`frontend.backup-raw-data-download-20261005-135341`；正式站未發布。

## 2026-10-05（SPC-1002-TASK-004 班別管制圖合併呈現）
- Xbar-R/Xbar-S 子組點位補帶班別與取樣階段 metadata，讓合併同圖檢視不同班別時，座標標籤、Tooltip 與點位詳細卡可顯示正確班別資訊。
- 保留既有 N1/N2 開收線排序、資料表結構、歷史資料與統計公式；未改 MR/Xbar/Cpk/Ppk 計算。
- 測試：Xbar metadata 與 Ca 回歸共 2 passed；後端 build 0 warnings / 0 errors；前端 testhost build 通過。
- 已發布 SPC 測試站 backend/frontend，備份 `backend.backup-shift-chart-20261005-130812`、`frontend.backup-shift-chart-20261005-130812`；正式站未發布。

## 2026-10-05（SPC-1002-TASK-003 Ca 顯示正負號驗證）
- 依 `docs/SPC 修正-1002.xlsx` 待辦第 3 項，只驗證現況，未重複修改功能程式。
- 後端 Ca 已用 `(mean - target) / halfWidth` 計算，前端直接顯示 `capability.ca` 並保留負號。
- `Capability_Ca_ShouldKeepSignAndLeaveCpkPpkUnchanged` 通過：1 passed；本項無程式異動，未重新發布測試站。

## 2026-10-05（SPC-1002-TASK-001 常態檢定 P-value 修正）
- 常態性檢定由一般 Jarque-Bera 統計量改為 adjusted Jarque-Bera，修正小樣本 raw data P-value 偏高，避免類似系統顯示約 `0.10` 但應判定 `<0.05` 的情境被誤判為符合常態。
- 新增小樣本回歸案例；`NormalityTest` 4 passed，後端 build 0 warnings / 0 errors。
- 已發布 SPC 測試站 backend，備份 `release/test/backend.backup-normality-20261005-123245`；`/api/version` 回 `environment=test`。正式站未發布。

## 2026-10-05（SPC-1002-TASK-002 管制圖界限預設關閉驗證）
- 依 `docs/SPC 修正-1002.xlsx` 待辦第 1 項，只驗證 SPC 管制圖現況，未重複改程式。
- `SpcChartView.vue` 的 `showSpecLimits` 與 `showControlLimits` 初始值皆為 `false`，checkbox 綁定同狀態；進入管制圖時規格界限與管制界限預設關閉，手動勾選仍可顯示。
- 趨勢圖規格線初始值另屬後續 `SPC-1002-TASK-006`，本項不混入。未建置、未發布。

## 2026-10-02（TASK-001 設定與密鑰安全）
- SMTP 設定 GET 改為只回傳是否已設定密碼；留白更新保留既有密碼。
- 範例設定檔的 JWT、SSO、SMTP 與資料庫欄位改為環境注入 placeholder。
- 新增設定安全回歸測試 2 項；後端測試與前端 production build 通過。
- 實際環境密鑰輪替尚未自動執行，需依部署環境另行完成。

## 2026-10-01（Particle Monitoring 完成）
- 完成 Particle Long Format 資料模型、preview/confirm、原始查詢、趨勢、R1-R9 比較、C/U-chart、TransFiles payload 與前端工作台。
- 最終回歸：Particle 後端 12 passed、build 0 warnings／0 errors；TransFiles 39 passed；前端 testhost build 通過。
- 測試站 frontend hash 一致，Particle 頁面與資產 200；API health 200、version `0.1.59/test`。正式站未發布，TransFiles EXE 未打包。
- Playwright runner 未正常結束；測試站登入後資料串接因未持有測試帳密尚待人工驗收。
- 後續已打包 TransFiles `2026.10.01.1`：新版主 EXE GUI 啟動正常，SHA-256 `2994FCE75F21A6495C75B05E3304099E9CE57C6A074AD36E29FC33D4DC01AD81`；測試站發布內容維持最新 build。[驗收指南](specs/20260930-particle-monitoring/acceptance-guide.md)

## 2026-10-01（Particle Monitoring 前端工作台）
- 新增 `/particle-monitoring` 與側邊導覽，整合 Particle C/U 圖型、時間趨勢、R1-R9 比較、原始資料及來源追溯。
- 支援日期、位置、粒徑與儀器條件；顯示固定／體積抽樣基準、20 點門檻及 U-chart 後端驗證訊息。
- `npm run build:test` 通過；已發布測試站前端，備份 `release-staging/particle-frontend-20261001-103955/frontend-backup`。頁面與新資產 HTTP 200，API 為 `0.1.59/test`；正式站未發布。
- Particle Playwright runner 兩次未正常結束，且 SSO 頁缺測試帳密，登入後人工畫面驗收待最後回歸處理。

## 2026-10-01（Particle Monitoring TransFiles Long Format 串接）
- TransFiles 落塵預覽改送 Particle Long Format JSON，保留來源座標、原值、檔名與 SHA-256；舊 Attribute Excel 僅保留相容匯出。
- R1-R9 與四種粒徑納入 payload；無真實抽樣體積時保持空值，因此 C-chart 可用、U-chart 需來源提供同單位正值。
- TransFiles 相關測試 39 passed、Python compile 通過；本次未打包 EXE，SPC 測試站與正式站皆未變更。
- 完整 TransFiles unittest discover 為 44 passed／2 failed；失敗皆因既有 `2026-4-24-0910.xlsx` fixture 不存在。

## 2026-09-30（藥液 F 表維護與公式動態取值）
- SPC 新增藥液 F 表資料模型、API 與「藥液 F 表維護」畫面，可維護 `F!B3`～`F!B10`、Dry-run、套用新版與查詢受影響線別/槽體/分析項目。
- 藥液濃度公式可直接引用 `F!B3` 或 `F!$B$3`，計算時由目前啟用 F 表取值，後續修改 F 表不需逐一回管制項目改固定常數。
- 測試站補 SQL Server 自我修復，若 F 表三張表不存在會自動建立；受影響項目查詢改為分段查詢，避免 EF LINQ 轉譯失敗。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter ChemicalFTableServiceTests --no-restore -p:UseSharedCompilation=false` 通過；`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` 通過；`npm run build -- --mode testhost` 通過。
- 已發布 SPC 測試站 backend/frontend；`/api/version` 回 `environment=test`，首頁載入 `index-mBOvEGxD.js`。正式站未發布。

## 2026-09-30（管制圖規格線與管制線顯示穩定化）
- 分段 USL/Target/LSL markLine 的 y 值納入圖表 y 軸範圍計算，避免線超出資料點範圍時看似未顯示。
- 動態 UCL/CL/LCL 資料點不足時，回退顯示固定 UCL/CL/LCL。
- `npm run build` 通過；已發布 SPC 測試站前端，備份 `frontend.backup-limit-line-visibility-20260930-141614`；首頁載入新資產 `index-BlUMHcJE.js`，API `/api/version` 回 `environment=test`。

## 2026-09-30（SPC SSO 重導迴圈保護）
- 前端 API 401 攔截加入 5 秒 redirect 防抖，避免多個失敗請求重複導 `/login`。
- `/portal-sso` 成功寫入 token 後加入 8 秒 grace window，避免舊 401 請求立即清掉新 token 造成 Portal/SPC 反覆跳轉。
- `npm run build` 通過；已發布 SPC 測試站前端，備份 `frontend.backup-sso-loop-guard-20260930-140731`；首頁載入新資產 `index-CUp_gYIn.js`，API `/api/version` 回 `environment=test`。

## 2026-09-30（分段規格保存與管制圖顯示）
- `ControlLimitSegments` 擴充 `USL/LSL/TargetValue`，管制項目分段維護可設定不同日期區間的規格與管制界線。
- SPC 計算與圖表資料依量測日期套用分段規格，圖表回傳 `specLimitSegments` 供前端顯示分段 USL/Target/LSL。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj` 通過；`npm run build` 通過。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --no-restore`：24 passed / 6 skipped / 6 failed；失敗為既有測試期待值與 InMemory transaction/SSO 相關，已列入本次進度限制。
- 已發布 SPC 測試站 backend/frontend，備份 `segment-spec-20260930-132447`；`/api/version` 回 `environment=test`，前端首頁載入新資產。正式站未發布。
- 修正分段新增/更新 API 改用 DTO，避免 Entity 導覽屬性觸發 `One or more validation errors occurred.`；後端重新發布測試站，備份 `backend.backup-segment-spec-validation-20260930-133551`。

## 2026-09-30（落塵監控 Attribute 預覽匯入支援）
- SPC 補主檔流程將 `DUST` 視為不需機台，落塵 Attribute 預覽可自動建立 `R#_0.5um`、`R#_1um`、`R#_5um`、`R#_10um` 的 `DUST_U` 管制項目。
- DUST 種子與受控 SQL 改為 `RequiresMachine=0`，描述同步四種粒徑。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter "SeedData_ShouldProvideDustMonitoringCAndUChartTypes|SpcEngineBaselineTests" --no-restore -p:UseSharedCompilation=false`：8 passed。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` 通過。
- 已發布 SPC 測試站 backend，備份 `backend.backup-dust-attribute-upload-20260930-084101`；`8081/api/version` 回 `environment=test`，`8081/health` HTTP 200。正式站未發布。

## 2026-09-30（落塵監控 DUST_C/DUST_U 圖型計算）
- SPC Attribute chart 計算器支援 `DUST_C`、`DUST_U`，分別銜接既有 C-chart、U-chart。
- 單筆 Attribute 即時計算同步辨識 `DUST_C`、`DUST_U`，避免匯入/查詢落塵資料時無統計值。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter SpcEngineBaselineTests --no-restore`：7 passed。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` 通過；原平行 build 曾被 `VBCSCompiler` 鎖檔，重跑後通過。
- 已發布 SPC 測試站 backend，備份 `backend.backup-dust-chart-codes-20260930-080553`；`8081/api/version` 回 `environment=test`，`8081/health` HTTP 200。正式站未發布。

## 2026-09-29（落塵監控查詢維度支援）
- SPC 管制圖頁與 summary API 補上 `DUST` scope 辨識、落塵監控 fallback 名稱與前端 DUST 維度配色。
- 新增前端 scope 測試，避免落塵監控被誤歸到製程。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore` 通過；`node --test frontend/mes-spc-web/tests/control-scope.test.mjs`：1 passed；`npm run build` 通過。
- 已發布 SPC 測試站 backend/frontend，備份 `chemical-dust-20260929-213750`；`8081/api/version` 回 `environment=test`，API/前端 HTTP 200。正式站未發布。
- 測試站 `SeedDatabase=false`，本次發布不自動寫入 DUST 主檔資料；若測試庫尚未有 `DUST` 群組，需另行受控套用。

## 2026-09-29（落塵監控 C/U 管制圖主檔種子）
- 新增 idempotent `DUST` 落塵監控群組與 `DUST_C`、`DUST_U` 計數型圖表主檔種子，供 SPC 後續落塵監控使用。
- 本次僅補主檔支援；未建立落塵量測資料、未處理 TransFiles 匯入。已隨 `chemical-dust-20260929-213750` 發布 SPC 測試站。
- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter UnitTest1 --no-restore`：2 passed。

## 2026-09-29（PPC 重複鍵儲存防護）
- 編輯／新增 SPC 管制項目前先檢查同鍵其他列，重複時回 409 與可讀訊息，避免 SQL Server 唯一索引錯誤直接顯示到畫面。
- 補上 PPC business key 單元測試；既有 Chameleon 測試補齊 DbContext 建構參數。`dotnet test backend/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj`：43 passed / 1 skipped。
- API Release publish 成功並已發布 SPC 測試站 backend；`/api/version` 回 `environment=test`，DLL hash 與 staging 相同。正式站未發布。

## 2026-09-25（咬蝕平均量顯示管制點數）
- 僅 `ETCH_A_AVG`／`ETCH_B_AVG` 顯示子組形成的管制點數；藥液與其他特性維持原始量測筆數。
- 班別測試 6 項與測試建置通過，已發布測試站。

## 2026-09-25（MR 圖班別資料對齊）
- 修正 I-MR 底圖使用完整 X 軸標籤造成 MR 點與班別錯位；改用第 2 筆起的日期／班別標籤。
- 班別測試 6 項與測試建置通過，已發布測試站。

## 2026-09-25（管制界限預設關閉）
- 規格界限與管制界限初始均不勾選，使用者仍可手動開啟。
- 班別測試 6 項與測試建置通過，已發布測試站。

## 2026-09-25（管制圖班別軸標籤）
- X 軸改顯示日期與班別，不再顯示具體時間；N1/N2 區分早／中班開收線，移除上方早班圖例。
- 班別測試 6 項與測試建置通過，已發布測試站。

## 2026-09-25（個別管制圖班別時間序列）
- 個別管制圖改依量測時間以單一線連接班別點；N1/N2 顯示早班開線、早班收線、中班，其他線別顯示早班、中班。
- 前端班別測試 6 項及測試建置通過，尚未發布測試站。

## 2026-09-24（咬蝕管制項目調整）
- 咬蝕匯入改為只要求 A 平均、B 平均、整體速率 3 個 PROCESS 管制項目；線速不再是必要項目。
- 化學測試 49 項通過。[規格](specs/20260924-etch-required-codes/spec.md)
- SPC 測試庫已補上 8 筆 A/B 映射 `MachineId`；正式庫未修改。

## 2026-09-24（咬蝕量逐份匯入）
- 預覽衝突改為逐份標示；相同資料略過，新增日報仍可匯入，衝突資料不覆寫。
- Portal／SPC 測試 API 與 TransFiles 工具已同步；規格：`specs/20260924-etch-partial-import/spec.md`。

## 2026-09-22（藥液 GENERAL 視同早班）

- 使用者確認：班別 GENERAL 都算早班，沒有「一般」；其他線別沒有開線／收線，僅 N1／N2 才分。
- 非 N1／N2 管制圖不再因出現 OPEN 而過濾掉 GENERAL；DP 混班案例早班序列含歷史日期。Portal 班別標籤 GENERAL 改顯示早班。
- Node 6 例通過；SPC 測試前端與 Portal 測試 Web 已發布。
- 使用者同意後，正式 SPC 前端已發布至 `PMR-SPC-SRV` `C:\inetpub\wwwroot\production\frontend`（`index-DVUw7DcK.js`）；未改後端、資料與正式 Portal。[驗證](specs/20260922-chemical-general-as-open/verification.md)

## 2026-09-22（正式 N1／N2 舊 CLOSE 轉收線）

- 使用者授權後，正式 PMR_SPC_2026 198 筆 CHEM N1／N2 舊 CLOSE＋GENERAL 改為 OPEN＋CLOSE；N1=16、N2=182。
- 備份 set 5098 通過 COPY_ONLY＋CHECKSUM＋VERIFYONLY；其他業務欄位不變，MIDDLE 37 筆未改，剩餘候選 0。未發布網站。[驗證](specs/20260922-chemical-close-stage-apply/verification.md)

## 2026-09-22（N1／N2 9 月前僅開收線）

- 使用者確認：N1／N2 9 月前只有開線／收線，沒有早班／中班；9 月後才分早班／中班。
- 正式庫 9 月前僅 OPEN／CLOSE 且階段 GENERAL，無 MIDDLE；MIDDLE 自 N1 9/10、N2 9/11 出現。
- 其後已授權僅轉換舊 CLOSE 198 筆；OPEN＋GENERAL 開線列仍不改。[規則](specs/20260922-chemical-n1n2-shift-cutoff/verification.md)

## 2026-09-22（咬蝕正式匯入規格）

- 使用者確認 `PMR_PORTAL_UAT` 為正式 Portal，並授權建立正式匯入規格；SPC 目標為 `PMR_SPC_2026`。
- 規格限定由固定 SHA256 的 Excel／48 份 manifest 重匯；不複製測試庫資料列，隔離 8 個負值／未完整鍵。
- 已新增 production dry-run、雙重資料庫確認與連線守衛；48 個 .NET 測試、7 個解析測試及建置通過。
- 正式 dry-run：48 份／3,650 點、正式既有 0，輸出 15 項主檔變更計畫。
- 後續依使用者同意建立兩庫正式備份：Portal backup set 5096、SPC 5097，均為 COPY_ONLY＋CHECKSUM 並通過 VERIFYONLY；備份證據守衛驗證通過，.NET 測試增至 48 項。
- 使用者明確授權 apply 後，正式匯入 Portal 48 份／3,650 點、SPC 3,746 筆；16 mappings、16 圖表 API、逐點／平均／樣本 S／速率／線速、隔離 0 筆全部對帳通過。
- 第二次重跑 48 份皆 Unchanged，ReportId／BatchId 不變。未發布 API／Web。[驗證](specs/20260922-etch-production-import/verification.md)

## 2026-09-22（咬蝕正式庫唯讀盤點）

- 使用者選 A：只讀比對測試 48 份與正式目標；未寫入、未發布。
- Portal 測試 48／3650 點；正式連線庫 PMR_PORTAL_UAT 為 0。SPC 測試 ETCH 量測 3698（缺 LINE_SPEED）；正式 PMR_SPC_2026 為 0。
- 正式缺 PT2 與 LINE_SPEED 等主檔；Portal 目標庫後續已確認為 `PMR_PORTAL_UAT`。見 [盤點](specs/20260922-etch-production-inventory/verification.md)。

## 2026-09-21（校正管理列表隱藏保管人欄位）
- 儀器校正到期管理列表移除「保管人」欄位；新增／編輯表單、通知收件邏輯與後端資料契約不變。
- `npm run build:test` 通過；2026-10-05 已發布 SPC 測試站前端，備份 `release-staging/calibration-hide-custodian-20261005-075314/frontend-backup`；`8083/calibration-instruments` 載入 `index-Cpses5Qc.js` 且資產 200，API `8081` health/version 正常。正式站未發布。[規格](specs/20260921-calibration-hide-custodian-column/spec.md)

## 2026-09-21（啟用測試站校正通知投遞）
- 測試站 `release/test/backend/appsettings.json` 新增 `Calibration:DeliveryEnabled=true`；未修改 SMTP、收件人或通知規則。
- JSON 設定驗證通過，已回收 `SpcApi` 測試 App Pool；未寄送真實通知或發布正式站。[規格](specs/20260921-calibration-delivery-enabled/spec.md)

## 2026-09-17（SPC AD／工號登入防重複）
- 完整 SSO 配對 canonical AD 與可信工號，連結既有工號主檔並保留 ID/權限；含停用/刪除檢查及交易身分鎖定。
- legacy 只准既有 AD 登入、不准首次另建，忽略未簽章工號/部門/Email，阻止完整交換衝突後降級重試新增第二筆。
- 12 核心測試及 SQL Server 5 次實際交換通過；臨時資料清理，真實 Operators 前後 23 筆。API 測試站已發布，備份 sso-single-operator-20260917-105440；正式站未發布。
- 既有 Daniel/daniel_teng 是否同人待確認，未自動合併。[驗證](specs/20260917-sso-single-operator/verification.md)。

## 2026-09-17（SPC 直接導向入口登入）
- 移除「使用 AD 帳號或工號登入」按鈕及過渡卡片，掛載前導向 Portal /Spc/Launch；既有身份驗證與原頁返回保留。
- 3 個 Playwright 案例、build:test 通過；真實未登入瀏覽器由 SPC /login 直達 Portal /Login?ReturnUrl=%2FSpc%2FLaunch。
- 已發布 SPC 測試前端，備份 direct-portal-login-20260917-102853；正式站未發布，真人帳密登入未執行。
- [規格與驗證](specs/20260917-direct-portal-login/spec.md)。

## 2026-09-17（設備即時狀態缺表修復）
- 補上 ChameleonSourceSettings 專屬 EF migration 與 SourceId 唯一索引，修復既有來源讀取程式所需表未建立的 SQL 例外；空表沿用原設定，不變更設備。
- 3 個回歸通過、API publish 成功；發布 SPC 測試站，備份 chameleon-source-schema-20260917-083916。
- 短效 Viewer 狀態請求由 500 修復為 200，3 來源中 2 online、1 unavailable；測試庫原有 1,838 筆量測保留。正式站未發布。
- [驗證紀錄](specs/20260917-chameleon-source-schema/verification.md)。

## 2026-09-17（Portal 藥液班別與取樣階段）
- 原取樣階段改標班別，早班/中班保留；僅 N1/N2 可另選開線/收線，其他線别停用並清空。
- 新增 SamplingStage 與日報四維唯一鍵；查詢/確認/upsert/預覽重複判斷納入階段，歷史 GENERAL 保留。切換清除草稿、忽略過期載入。
- 12 核心及 3 UI 案通過，三專案建置成功；SPC/Portal 測試站已發布，備份 chemical-shift-stage-20260917-082711。測試庫 1,838 筆既有值/班別檢核不變。
- 真人登入端到端待驗收；正式站未發布。[驗證](specs/20260917-chemical-shift-stage/verification.md)。

## 2026-09-16（一句話需求預設流程）

- AGENTS.md 新增簡短開發需求自動套用 SDD、最小範圍、必要讀取/測試與精簡回覆；一般問答不觸發修改。
- 文字與連結檢查通過；純文件，發布不適用。

## 2026-09-16（儀器列表捲動改善）

- 表格限制 60dvh、固定表頭、上方左右按鈕，資料多時不必拉到最後一筆找水平捲軸。手機僅本路由改導覽/內容上下排列。
- 6 項介面案例通過（含桌面/手機 100 筆捲動），build:test 成功；備份 calibration-scroll-20260916-132959，已發布 SPC 測試站前端，頁面/資產 200。正式站未發布。

## 2026-09-16（儀器量測與校驗資訊）

- 新增量測規格、精度、備註、校驗規範、允收標準；列表/新增編輯/Excel 預覽與匯入均支援，單欄 2000 字、保留換行。
- EF migration 新增五欄；原 Excel F/G/AB/AC/AD 補回測試庫 59 筆空白資料，逐筆稽核且重跑 0 筆，日期/狀態/週期識別未變。
- 120 個後端案例分次通過、12 項 UI 案例回報通過；API/前端建置成功。測試站備份 calibration-details-20260916-111017，API/Web/JS 200。正式站未發布、未發通知。

## 2026-09-15（校正通知選用 Synology Chat）

- 提醒設定可選 Email／Synology Chat，支援加密 Webhook 儲存、保留及手動測試。Chat 依儀器／階段群組防重，切換取消不適用待送工作。
- 115 後端測試通過、8 項介面案例回報通過；API publish、build:test 成功。
- 已發布 SPC 測試站，備份 calibration-chat-20260915-131936；三個新欄已透過 migration 套用 PMR_SPC_TEST；API／Web／資產 200。
- 真實 NAS 發送未驗證；自動排程未啟用、未寄通知、正式站未發布。

## 2026-09-15（校正提醒天數簡化設定）

- 逗號輸入改為常用天數勾選、自訂加入及恢復預設；保留原有自訂值，最多 12 個提醒時間。
- 6 項 Playwright 案例回報通過、build:test 成功，手機設定視窗檢視通過。
- 已發布 SPC 測試站前端，備份 reminder-picker-20260915-110645；頁面與資產 200。正式站未發布、未寄通知。

## 2026-09-12（儀器校正到期管理亮色配色）

- 校正管理頁改為亮底青／天空／琥珀卡片與按鍵，解決過暗過重。不改欄位、權限、匯入或通知。
- 回歸：校正匯入 Playwright 4 通過。已發布 SPC 測試站前端（備份 `20260912-215320`）。正式站未發布。

## 2026-09-12（未校／免校列先建主檔、日期後補）

- 剩下 12 筆可匯入：未校、預計、-、-- 日期留空，不轉成到期日；免校／`--` 週期先以 12 月建檔並警示。
- 下次到期日可空；編輯可後補。無到期日不列入摘要、不寄提醒。既有 47 筆重匯仍略過。
- 回歸：校正測試 80 通過。已發布 SPC 測試站（備份 `20260912-170311`）；測試庫下次日期已可空。正式庫／正式站未發布。

## 2026-09-12（校驗方式公式改讀儲存值）

- 正式 Excel J 欄為公式，改讀已儲存的外校／內校／免校，不再因此擋下可匯入列。
- 未校／預計日期與免校／`--`／`-` 仍不匯入、不推定。
- 回歸：校正測試 82 通過；實際範本 47 可匯入、12 不合格。
- 已發布 SPC 測試站（備份 `20260912-164138`）。正式庫／正式站未發布。

## 2026-09-12（儀器列表依編號排序）

- 儀器校正管理列表改依儀器設備編號排序，不再先依下次校正日。
- 到期摘要與測試寄信仍依到期日，不改提醒規則。
- 回歸：校正測試 82 通過、SPC API 建置 0 警告／0 錯誤。
- 已發布 SPC 測試站 API（備份 `20260912-162830`）。正式庫／正式站未發布。

## 2026-09-12（儀器主檔顯示校驗方式）

- 主檔新增可選「校驗方式」（最多 100 字）。列表、新增／編輯與 Excel 匯入預覽均顯示。
- 匯入讀第一工作表 J 欄，J5 須為「校驗方式」；不拿校驗方式改週期或免校判定。既有編號仍略過不覆寫。
- 回歸：校正測試 81 通過、Playwright 4 通過、SPC API 建置 0 警告／0 錯誤。
- 已發布 SPC 測試站（備份 `20260912-161409`）；測試庫已套用 `CalibrationMethod` 欄。正式庫／正式站未發布。

## 2026-09-12（儀器主檔顯示放置地點）

- 主檔新增可選「放置地點」（最多 100 字）。列表、新增／編輯與 Excel 匯入預覽均顯示。
- 匯入讀第一工作表 H 欄，H5 須為「放置地點」；不拿放置地點推定部門。既有編號仍略過不覆寫。
- 回歸：校正測試 79 通過、Playwright 4 通過、SPC API 建置 0 警告／0 錯誤。
- 已發布 SPC 測試站（備份 `20260912-160201`）；測試庫已套用 `Location` 欄。正式庫／正式站未發布。

## 2026-09-12（儀器校正測試通知按鍵）

- 儀器校正「提醒天數設定」新增手動測試寄信：指定 Email、主旨含【測試】、沿用 SMTP／本機備份。
- 不寫入到期通知紀錄、不開啟 `Calibration:DeliveryEnabled`、不對保管人清單群發。
- 回歸：校正測試 77 通過（含 5 項測試寄信 HTTP）、Playwright 4 通過、SPC API 建置 0 警告／0 錯誤。
- 已發布 SPC 測試站 IIS（備份 `20260912-154446`）；`test-email` 未登入 401；未開啟自動寄信。實際 SMTP 端到端待操作。
- 正式庫／正式站未發布。

## 2026-09-12（儀器 Excel 批次匯入：發布 SPC 測試站 IIS）

- 已發布 `D:\SPC\release\test\backend` 與 `frontend`（IIS `SpcApi`／`SpcWeb`）；備份識別碼 `20260912-145908`。
- 以 `app_offline.htm` 確認 IIS 指向測試目錄；保留測試 `appsettings.json`／`web.config`；`AppEnvironment=test`，`Calibration:DeliveryEnabled` 未開啟，資料庫 `PMR_SPC_TEST`。
- Smoke：`8081/api/version` 200（0.1.57／test）；匯入 access 未登入 401；preview/commit 無檔 415；校正摘要未登入 401；SPC Web `8083/calibration-instruments` 與新資產 200。
- 無 pending migration。Portal、正式環境、`D:\PmrPortal\publish`、`D:\Sites\PmrPortal` 未發布。未寄真信、未寫入儀器資料。
- 登入後匯入與品保摘要端到端仍待驗證，不標上線完成。

## 2026-09-12（儀器 Excel 批次匯入：本機實作與驗證）

- 在既有儀器校正頁新增 Excel 上傳、第一工作表預覽、逐列錯誤/公式警示、有效列勾選，以及部門/保管人/狀態批次補齊。
- 新增 import/access、preview、commit API；提交重讀檔案並檢查 SHA256/列號/權限。既有編號略過，同檔重複拒絕；主檔與稽核同交易，失敗全部回復。
- 保留 Excel 明確到期日與公式儲存值；未知週期、未校、預計日期不猜測。無 schema 變更、不建立合格校正歷史、不直接寄信。
- 依 SDD 先建立規格與核心測試；[規格](specs/20260911-instrument-calibration-import/spec.md)、[驗證](specs/20260911-instrument-calibration-import/verification.md)。
- 回歸：72 後端測試、3 隔離瀏覽器測試通過；SPC API 建置 0 警告/0 錯誤，Web build:test 成功。來源 Excel 59 筆，測試前後未修改。
- HTTP 使用真實 MVC/JWT 與隔離 SQLite；UI API 為 mock，桌面/手機已檢視。實際 IIS/AD/SQL Server 端到端尚未執行，不標上線完成。
- **本次未發布任何 IIS、不寫正式資料庫、不寄真信**；保留既有工作區修改。

## 2026-09-11（儀器校正到期通知：發布測試站）

- 僅發布 `D:\SPC\release\test` 與 `D:\PmrPortal\release\test`；備份識別碼 `20260911-140728`。
- 保留測試 `appsettings.json`／`web.config`。SPC `AppEnvironment=test`，Portal `EnvironmentSwitch.Target=Test`。
- Smoke：`8081/api/version` 200（test）；校正 API 未登入 401；Portal `8091/health` 200（test）；SPC Web `8083` 200；Portal 首頁未登入 302。
- 測試庫 `PMR_SPC_TEST` 已建立校正資料表。未開啟 `Calibration:DeliveryEnabled`，未寄真信。
- 正式環境、`D:\PmrPortal\publish`、`D:\Sites\PmrPortal` 未發布。登入後摘要／儀器頁待品保帳號驗收。

## 2026-09-11（儀器校正到期通知：本機實作與單元驗證）

- 規格 v4.1；依決議補齊 08:00 掃描、摘要 `windowDays`、送校中計數、前端台北日與月週期、保管人可取消、三次退避後停止。
- `dotnet test tests/MesSpc.Calibration.Tests` 42 通過；SPC API／Portal API／Portal Web／SPC Web 本機建置成功。
- 未套用正式 DB、未寄真信、未發布。瀏覽器端到端未執行，功能不標完成。
- 基準 3.6 已摘要儀器校正規則。

## 2026-09-11（儀器校正到期通知：Q-01～Q-06 同意定案 v4）

- 使用者回覆「同意」，六項建議方案納入主規格 v4（R-011～R-016），狀態為已核准／可實作。
- 對齊摘要窗口、送校中計入摘要、Editor 預設含 `calibration.manage`；執行校正單元測試。
- 未套用正式 DB、未寄真信、未發布；2026-08-07 基準正文未改。

## 2026-09-11（儀器校正到期通知：SDD 文件 v3，規則未定案）

- 依當日授權重做現況盤點與規格，撤回先前文件將 Q-01～Q-06 標為「使用者已核准」的狀態。
- 主規格版本 3、狀態草稿：[specs/20260911-instrument-calibration/spec.md](specs/20260911-instrument-calibration/spec.md)；計畫、任務、驗證方式已對應 R/AC 編號。
- Portal 引用索引已改為 v3 草稿。需求索引改為「規格草稿／待確認」，未寫入 2026-08-07 基準正文。
- 本次不修改程式、不套用 migration、不寄信、不發布。工作區既有校正模組仍視為程式觀察，功能驗收均標未執行。

## 2026-09-11（儀器校正到期通知：實作 T-005～T-010 完成）

**T-005～T-010 實作完成，建置成功（0 警告 0 錯誤）；DB Migration 尚未套用至正式環境。**

- **SPC 後端**（建置 ✅）：
  - `Domain/Entities/CalibrationModels.cs`：7 個 Entity（CalibrationInstrument、InstrumentCalibrationRecord、CalibrationCertificate、CalibrationNotificationSetting、CalibrationNotification、CalibrationNotificationAttempt、CalibrationAuditLog）。
  - `Infrastructure/Data/CalibrationModelConfiguration.cs`：資料表、唯一索引（`(InstrumentId,CycleId,DueDate,Stage,RecipientKey)`）、FK 與欄位長度設定。
  - `Infrastructure/Data/AppDbContext.cs`：已呼叫 `ConfigureCalibration()`。
  - Migration：`20260911040000_AddCalibrationModule.cs`（SQL Server 格式手寫，含 7 張資料表、所有索引與 FK）。
  - `AppDbContextModelSnapshot.cs`：已加入全部 Calibration 實體定義。
  - `Services/Calibration/InstrumentCalibrationService.cs`：儀器 CRUD、版本衝突、樂觀鎖、Audit。
  - `Services/Calibration/CalibrationCertificates.cs`：PDF/JPEG/PNG 上傳、SHA256 雜湊、私有儲存。
  - `Services/Calibration/CalibrationRules.cs`：日期計算、提醒階段（BEFORE-X、OVERDUE-X）、管理權限、退避重試間隔。
  - `Services/Calibration/CalibrationNotificationProcessor.cs`：每分鐘輪詢、掃描（08:00+ 台北時間）、防重（唯一鍵）、Lease、退避重試（15m/1h/4h）、SMTP 寄送。
  - `Services/Calibration/CalibrationSmtpSender.cs`：`Calibration:DeliveryEnabled` 開關。
  - `Services/Calibration/CalibrationNotificationSchedulerService.cs`：BackgroundService。
  - `Controllers/InstrumentCalibrationsController.cs`：GET/POST/PUT 儀器、GET 歷史、POST 校正、上傳/下載證書、GET 摘要 API（`/api/v1/instrument-calibrations/summary`）、GET/PUT 設定。
  - `Program.cs`：服務注入完整（InstrumentCalibrationService、CalibrationCertificates、CalibrationNotificationProcessor、ICalibrationMailSender、BackgroundService）。
- **SPC Web**（建置 ✅）：`InstrumentCalibrationsView.vue`、路由 `/calibration-instruments`。
- **Portal API**（建置 ✅）：`SpcProxyController` 代理 `/api/spc/instrument-calibrations/summary`。
- **Portal Web**（建置 ✅）：首頁依 `IsQualityAssurance` 顯示即將到期 / 已逾期摘要卡片 + SPC 跳轉連結。
- 未執行之驗收：T-011（量測隔離確認）、T-012（DB Migration 套用與端對端驗證）待執行。
- 未套用 DB Migration；正式資料庫未變更；未寄信；未發布 IIS。

## 2026-09-10（Portal＋SPC 手動正式發布包）

- 建立本機交付包 `release-packages/Portal-SPC-20260910-ManualRelease`，分為 `SPC_API`、`SPC_Web`、`Portal_API`、`Portal_Web` 四個獨立 Release 資料夾。
- API 以 `dotnet publish -c Release --no-restore`、SPC Web 以 `npm run build:production` 建置；每包提供 SHA-256 `manifest.json` 與根目錄人工部署／回復 README。
- 交付包已排除 `appsettings*.json`、`uploads`、`.pfx`、`.p12`、IIS 環境變數與站台憑證，四個 manifest 均逐檔驗證通過。
- 本次只建立發布包，未連線、備份、修改或發布任何正式 IIS、資料庫、量測資料及 migration；待使用者手動部署至 SPC／Portal 正式伺服器。

## 2026-09-10（藥液管制圖早中晚班辨識）

- 藥液管制圖有班別資料時，早班 `OPEN` 顯示藍色圓點、中班 `MIDDLE` 顯示綠色菱形、晚班 `CLOSE` 顯示橘色方點。
- 三個班別依日報日期對齊為獨立序列，同日不覆寫、不合併；缺少班別保留空值，中班不再因既有早晚班配對邏輯遺漏。
- Tooltip 改為顯示「班別：早班／中班／晚班」；沒有班別的既有 `GENERAL` 資料維持原本單序列。
- 回歸檢查：SPC Web `npm run build:test` 成功，待以含三班資料的測試藥液項目確認實際符號及 Tooltip。僅發布測試站，正式環境不發布。

## 2026-09-09（週月報表排行、Cpk 公式與 AD/工號登入）

- SPC 週/月報表新增 OOS 件數或 Cpk（實際採用 Ppk）排行，可切換最高 5 位與最低 3 位；既有總表欄位點選排序保留。
- 週/月報表新增 Cpk/Ppk 公式說明：雙邊規格使用 `min((USL-平均值)/(3σ),(平均值-LSL)/(3σ))`，並說明 sigmaWithin 與 sigmaOverall 的差異及畫面 Cpk/Ppk 對應。
- Portal 工號登入比對增加去除空白與連字號的相容正規化；AD 帳號優先、重複工號仍拒絕，不繞過 AD 密碼驗證。SPC SSO 仍傳遞 AD 帳號與工號。
- 回歸檢查：SPC Web、Portal API、Portal Web 建置成功；測試站發布與登入 UAT 待完成。正式環境不發布。

## 2026-09-09（藥液量測品保權限與異動稽核）

- Portal SPC 代理維持整體 `quality_assurance` 角色授權，查詢、新增、修改、刪除均不可由非品保帳號直接呼叫；刪除鍵只對可刪除的 Portal 日報資料顯示。
- SPC `UploadDetails` 新增藥液日報異動稽核：`ManualMeasurementCreated`、`ManualMeasurementUpdated`、`ManualMeasurementDeleted`，保存操作者、UTC 時間、量測 ID 及新增值／修改前後值／刪除前原值。
- 刪除在移除量測前先寫入不可作為匯入資料的稽核明細，並同一交易清除衍生 SPC 計算與警示；匯入批次及稽核資料保留。
- SPC API 與 Portal API 建置及測試站發布成功；測試部署備份識別碼 `20260909-132647`，正式環境未發布。

## 2026-09-09（藥液中班、當日刪除與圖表規格線）

- SPC 管制圖 y 軸範圍現在同時納入量測、USL／LSL／Target 與可見管制界線並保留邊距，避免規格線因自動刻度未顯示。
- N1／N2 藥液日報新增中班 `MIDDLE`，與既有早班 `OPEN`、晚班 `CLOSE` 以每日唯一鍵獨立保存與載入；其他線別仍維持一般日報。
- Portal 歷史量測列表僅對 Portal 日報手動量測顯示刪除鍵；刪除會在 SPC 交易內移除該筆量測、衍生計算與警示，非日報或匯入來源會被拒絕，匯入批次稽核資料保留。
- 回歸檢查：SPC API／Web 與 Portal API／Web 建置成功；SPC API health、SPC Web 回應 200，Portal 藥液頁未登入時回應 302。兩個既有 API 測試專案分別受未同步 `AuthControllerTests` 與多個既存測試編譯錯誤阻擋，登入後的早／中／晚班與實際刪除待品保帳號於測試站驗收。
- 已僅發布至 IIS 測試站 `SpcApi`／`SpcWeb`／`PmrPortalApi`／`PmrPortalWeb`；部署前回復備份識別碼 `20260909-102513`，正式環境未發布。

## 2026-09-09（第二批 SDD 流程導入）

- KM、Chameleon、DH_Temperature、python-pypxlib、PMR_ERP撈取工單、DS2000、Voice 新增開發入口、需求索引與共用規格引用。
- 共用流程更新為 v1.1，第一批入口同步版本；十專案已接入文件流程。
- [導入登錄](docs/sdd-projects.md)及[驗證紀錄](specs/20260909-sdd-rollout/verification.md)。
- DS2000 維護範圍以 Ri320Bridge 為主，Voice 巨集尚待檢查；真實功能試行及全面歷史需求驗證未完成。
- 純文件變更，不涉及產品、設備、資料庫或應用程式發布。

## 2026-09-09（SDD 流程導入）

- 建立共用 SDD v1.0、五份範本與 SPC/Portal/TransFiles 開發入口及需求索引。
- 規格與驗證：[20260909-sdd-adoption](specs/20260909-sdd-adoption/verification.md)。
- 原需求基準保留；本次只驗證流程文件，下一個真實功能需求仍須試行，其餘七專案尚待導入。
- 純文件變更；應用程式建置與發布不適用。

## 2026-09-08（SPC 統一使用 AD／Portal SSO 登入）

- SPC 停用本機帳號密碼登入；登入頁統一導向 Portal AD 帳號／工號登入，SPC 不接收或保存 AD 密碼。
- 作業人員與權限主檔移除密碼輸入與密碼狀態顯示；後端拒絕新增／修改本機密碼，既有 `PasswordHash` 僅保留相容性、不再使用。
- AD 帳號、工號、Email、角色、啟用狀態及頁面權限維持可管理；Portal SSO 同步流程不變。
- 回歸檢查：前端 `npm run build:test` 與後端 `dotnet build` 均成功。
- 已發布至本機 IIS 測試站 `SpcWeb`／`SpcApi`；本機登入端點回應 410，作業人員頁面實際確認無密碼輸入欄位。

## 2026-09-08（SMTP 異常預警支援多人收件人）

- SMTP 設定新增與週報／月報相同的啟用中操作者多選收件人；異常預警會逐一寄送並保留舊預設信箱 fallback。
- 新增獨立 `SpcAlertNotificationSettings` 設定表，SMTP 伺服器設定仍寫入 `appsettings.json`，收件人名單改由資料庫管理。
- SMTP 測試信與測試異常通報支援對所有選取收件人寄送，回傳每位收件人的成功／失敗結果。
- 回歸檢查：前端 `npm run build:test` 成功；後端建置成功並產生 `20260908170000_AddSpcAlertNotificationSettings` migration，測試資料庫已套用且 SMTP API 回應 200。
- 測試資料庫目前沒有啟用且已設定 Email 的操作者，收件人清單暫為 0 人；未選取時維持舊 `DefaultRecipientEmail` fallback。

## 2026-09-08（暫時隱藏三項分析入口）

- SPC 側邊選單暫時移除「咬蝕量分析」、「製程能力分析」及「異常點分析」入口。
- 對應路由與頁面程式保留，未刪除功能，方便後續確認需求後恢復。
- 回歸檢查：側邊選單保留 SPC 管制分析、量測趨勢分析及 SPC 週月報表等其他製程分析入口。

## 2026-09-08（測試資料庫匯入 8 月非 N1/N2 藥液資料）

- 以 `TransFiles/批次轉換結果_藥液匯入檔_2026-08.xlsx` 為來源，排除 N1／N2 後保留 1,269 筆其他線別資料，匯入測試資料庫 `PMR_SPC_TEST`。
- 測試匯入批次 `79ceeb2e-cfc6-46b2-9032-5cf832a3238b` 以僅新增、重複略過模式完成；總列數 1,269、通過 1,269、錯誤 0，批次狀態 `Imported`。
- 回歸檢查：SPC 管制圖可查到匯入後的 C5／清潔／硫酸資料；原始 Excel 未修改，排除後檔案另存於 TransFiles。

## 2026-09-08（製圖能力指標改用整體能力值）

- 查詢總表進入製圖後，能力指標仍顯示 `Cp`、`Cpk`，但數值改取後端 `Pp`、`Ppk`；不變更後端計算與 API 欄位。
- 移除能力指標卡片的中文能力名稱副標，避免顯示與指標名稱混淆。
- 回歸檢查：確認能力指標卡片仍有 `Ca`、`Cp`、`Cpk` 三個英文標籤，且 `Cp`／`Cpk` 分別使用 `Pp`／`Ppk` 數值。

## 2026-09-08（常態性檢定標籤調整）

- SPC 管制圖的常態性檢定顯示由「JB p-val」調整為「P-val」；2026-10-05 起 p-value 改採 adjusted Jarque-Bera，修正小樣本 P-value 偏高。
- 回歸檢查：確認分布圖 Tooltip 與常態性摘要均顯示「常態性檢定 (P-val)」。

## 2026-09-08（整合工作區專案入口）

- 新增 `All-Projects.slnx`，集中列出目前工作區可發現的 21 個 .NET 專案，包含 SPC、Portal、Chameleon、KM、DS2000、工具與測試專案。
- 新增 `All-Projects.code-workspace`，集中開啟目前 10 個工作區資料夾；暫存 `.codex-tmp` 專案不納入總 solution。
- 回歸檢查：`dotnet sln All-Projects.slnx list` 可正常列出 21 個專案；工作區 JSON 可解析且 10 個資料夾路徑均存在。

## 2026-09-08（SPC 個人頁面可見權限）

- 作業人員主檔新增個人頁面權限設定；登入 Token 帶入允許頁面，未授權頁面即使直接輸入網址也會被導回首頁。
- 權限清單未設定時沿用舊規則：Editor 保有全部頁面，Viewer 保有原本分析與設備監控頁，避免既有使用者突然失去功能。
- 頁面權限目前只控制瀏覽範圍；寫入、匯入與管理操作仍由既有 Viewer／Editor 後端保護。SPC API／Web 更新為 `0.1.57`。

## 2026-09-08（Portal／SPC 品保人員唯讀比對）

- 作業人員與權限主檔新增「比對 Portal 品保人員」，依 AD 帳號及工號比對並顯示已結合、資料不同、僅 Portal、僅 SPC與衝突。
- 比對本身維持唯讀；管理員可勾選「資料不同」或「僅 Portal」後確認同步。新建人員預設 Viewer，更新既有人員時保留角色、啟用狀態、密碼及歷史關聯。
- 衝突資料禁止一般同步，必須後續人工指定正確對應；每次成功同步會記錄執行者、時間與同步前後內容。
- SPC API 更新為 `0.1.56`、SPC Web 更新為 `0.1.56`。

## 2026-09-08（Portal／SPC 集中登出）

- SPC「登出」改為「全部登出」：清除 SPC Token 後前往 Portal 集中登出，清除 Portal Session 並返回登入頁，避免殘留 Session 又自動登入。
- 「入口網站」按鈕維持單純切換系統、不登出；SPC Web 更新為 `0.1.54`。

## 2026-09-08（Portal／SPC 一次登入雙向切換）

- 直接開啟 SPC 且尚無 SPC Token 時，自動前往 PmrPortal `/Spc/Launch` 檢查既有 Portal Session；已登入 Portal 者直接回到 SPC，不再要求輸入帳密。
- SPC 返回入口網站改為直接前往 Portal 首頁；既有 Portal Cookie 有效時不經登入畫面。從 Portal 進 SPC 的既有單一登入流程不變。
- 單一登入完成後保留使用者原本欲前往的 SPC 頁面；本機備援登出後停留於本機登入模式，避免立即被 Portal Session 自動登入。
- 本次僅修改 SPC 前端導向，不變更業務 API、資料庫、Portal 權限或其他功能；SPC Web 更新為 `0.1.53`。

## 2026-09-07（SPC 作業人員 AD 單一登入）

- SPC 登入頁新增「使用 AD 帳號或工號登入」，透過 PmrPortal 驗證公司密碼後自動返回 SPC；既有本機帳密保留為維護備援。
- Portal SSO 同步 AD 帳號、工號、姓名、部門與 Email；新使用者預設為 Viewer，既有使用者角色與停用狀態不被覆蓋。
- 同一工號若已綁定其他 SPC 帳號會拒絕同步並提示管理員確認，避免錯誤合併作業人員資料；舊版 Portal SSO 簽章仍相容。
- SPC API 更新為 `0.1.54`、SPC Web 更新為 `0.1.52`。

## 2026-09-07（Chameleon FINS 點位名稱支援）

- 設備點位名稱解析不再固定查詢 MELSEC，改為辨識 FINS、MELSEC、MEWTOCOL、MODBUS、STEP7、TOYOPUC 與 Virtual 設備設定。
- 同時支援 FINS 的單一 `equipment` 與 MELSEC 的 `equipmentList` 回應結構；`RTR_Layer_Lamination` 現可由 FINS 範本取得完整 `channelName`。
- 新增 FINS／MELSEC 回歸測試；SPC API 版本更新為 `0.1.53`。
- 設備點位總覽不再以空白來源／設備呼叫 API，且成功載入或儲存後會清除先前錯誤；SPC Web 版本更新為 `0.1.51`。

## 2026-09-03（正式／測試藥液日報追加匯入 9 月資料）

- 重新解析 2026-09-01、09-02、09-03 與 09-03 中班共 4 份原始藥液日報，產生 `TransFiles/批次轉換結果_藥液匯入檔_2026-09_修正版.xlsx`；共 259 筆，修正舊轉換檔將 N1 誤標為 N2 的問題。
- 09-03 中班原始檔 8 筆日期誤留為 2026-04-24，依來源檔日期修正為 2026-09-03；修正後日期範圍為 2026-09-01～09-03，空白量測、缺少／重複 SamplingPhase 與人工確認項目皆為 0。
- 匯入前建立並通過 `RESTORE VERIFYONLY ... WITH CHECKSUM` 的完整 `COPY_ONLY` 備份：`PMR_SPC_2026_pre_september_import_20260903.bak`、`PMR_SPC_TEST_pre_september_import_20260903.bak`。
- 正式批次 `6fc45641-d192-4686-a20d-ec2b020d965e`、測試批次 `9438b6c2-2b75-40c4-b78c-017147a2a364` 均以 `PortalDaily`／`insertOnly` 完成：259 筆全數通過預覽，各新增 251 筆；09-03 中班與日班相同日報唯一鍵的 8 筆依防重規則略過。
- 兩套環境均保留 8 月 317 筆，匯入後各有 568 筆量測；9 月 N1 16 筆、N2 60 筆，日報唯一鍵重複群組為 0。臨時本機 API 已於完成後關閉。

## 2026-09-03（正式／測試藥液量測清空重匯）

- 依使用者確認，以 `TransFiles/批次轉換結果_藥液匯入檔_2026-08.xlsx` 為唯一基準，清除正式 `PMR_SPC_2026` 與測試 `PMR_SPC_TEST` 的量測、SPC 計算、警示及舊匯入批次交易資料；製程、線別、槽位、品質特性、規格與管制項目主檔保留。
- 清除前建立並通過 `RESTORE VERIFYONLY ... WITH CHECKSUM` 的完整 `COPY_ONLY` 備份：`PMR_SPC_2026_pre_full_reimport_20260903.bak`、`PMR_SPC_TEST_pre_full_reimport_20260903.bak`。
- 來源檔共 317 筆（N1 16、N2 301）；正式批次 `2230c48b-64d2-4780-bc4e-89747d2da78d`、測試批次 `bfaddea3-d5dd-4cb8-8761-3ac8a3432ae7` 均以 `insertOnly` 完成 317 筆匯入，0 筆略過、0 筆錯誤，重複群組為 0。
- 正式環境依預覽補建 N1／催化／P400／ml/L 管制項目；正式重算 227 筆 SPC、產生 1 筆警示，測試重算 258 筆 SPC、產生 3 筆警示。

## 2026-09-02（N1／N2 藥液開收線成對管制圖）

- 藥液日報量測資料新增 `SamplingPhase`（`OPEN`／`CLOSE`／`GENERAL`），同一線別、日期與管制項目可同時保存開線與收線資料，既有資料統一為 `GENERAL`。
- SPC 管制圖假如查詢範圍同時有開、收線資料，同一日期以藍色圓點開線與橘色方點收線雙序列顯示，Tooltip 顯示階段與實際量測時間；缺少單一階段時不補零。
- 資料庫新增 `AddChemicalSamplingPhase` 遷移；SPC API 版本 `0.1.51`，SPC Web 版本 `0.1.47`。

## 2026-09-02（Portal 單一登入防重放強化）

- SPC `portal-sso` 簽章交換新增 Nonce 一次性使用檢查；同一請求在有效期內重複送出時回傳 401，防止簽章重放。
- SPC API 版本更新為 `0.1.50`，搭配 PmrPortal API `1.0.153` 的統一簽章式 SSO 登入流程。

## 2026-08-27（C3 清潔槽硫酸濃度公式修正）

- C3／清潔／硫酸（PPC 2539）的濃度公式依原始藥液日報表修正為 `Primary * 6.2 * 0.995`；滴定值 3.1 應得到 19.12，不再錯算為 26.16。
- 藥液公式同步工具新增 `--rule=<規則 ID>` 單筆篩選，避免修正特定項目時連帶覆寫其他已客製化公式。
- 回歸檢查：以 `C3:7` 單筆乾跑及套用後驗證，確認對應唯一 PPC、公式驗證一致，且其他管制項目不變。

## 2026-08-27（C5 清潔槽調整公式修正）

- C5／清潔／硫酸（PPC 2425）規格修正為 LSL 90、Target 100、USL 110；分析值低於 LSL 時添加 DP333，高於 USL 時稀釋並計算等量排液／補水，規格內不調整。
- 調整與調整量公式改用 `LSL`、`Target`、`USL`，避免日後主檔規格變更卻仍使用寫死門檻；公式同步至測試及正式 SPC 資料庫，更新前各自保存欄位級回復檔。
- 回歸檢查：分析值 70 得到「添加／DP333：33 L」、100 不調整、130 得到「稀釋／排液：254 L 補水：254 L」。

## 2026-08-25（測試藥液公式同步至正式環境）

- 以線別／機台、槽體／槽位及品質特性代碼自然鍵比對，將測試資料庫已確認設定同步至正式資料庫：54 項 `ChemicalAnalysisConfigJson`、15 項 LSL／Target／USL 規格及 8 種品質特性的 InputMode／ValueLabel／DecimalPlaces。
- 正式環境缺少的 PT1／TANK-000289／CHAR-000856 維持排除，未自動新增主檔；量測、SPC 計算、上傳批次／明細／錯誤及警示資料均未同步且筆數不變。
- 同步前建立並通過 `RESTORE VERIFYONLY ... WITH CHECKSUM` 的正式資料庫 `COPY_ONLY` 完整備份 `PMR_SPC_2026_pre_formula_sync_20260825_114442.bak`，另保存欄位級回復檔 `.codex-tmp/ChemicalFormulaSyncAudit/production-rollback-20260825_114442.json`。
- 更新使用單一資料庫交易；提交後所有測試／正式已配對藥液項目逐欄比對為零差異。

## 2026-08-25（管制圖明細顯示規格界線區塊）

- SPC 管制圖查詢明細摘要新增「規格界線」區塊，直接顯示 USL、Target、LSL 數值；本次只增加摘要資訊，不新增或變更圖表上的規格線。
- 規格值優先使用本次管制圖回傳界線，缺值時回退至管制項目主檔設定。
- 規格界線摘要統一顯示至小數點後兩位；其他能力指標等統計數值的小數位維持不變。
- 管制圖摘要中的 I／Xbar 與 MR／R／S 管制界線亦統一顯示至小數點後兩位；只調整文字格式，不改變界線計算精度。

## 2026-08-24（Portal 藥液雙滴定值追溯）

- Portal 藥液日報上傳明細可保存 `SecondaryTitrationValue` 與 `RecheckSecondaryTitrationValue`；量測主值、SPC 計算及管制圖資料結構不變。
- 每日日報查詢同步回傳主要／第二滴定值及其複驗值，供 Portal 修改既有日報時完整還原原始輸入。
- 未提供第二滴定值的既有匯入、單滴定項目及歷史資料維持相容。
- 測試資料庫更新前已備份 PPC 2594、2595 至 `PartProcessCharacteristics_formula_backup_20260824`；兩項改為 `Secondary - Primary`，失效的 Excel 儲存格 `U18／U19` 依來源規則改為明確門檻 20／100。
- SPC API 已發布至 IIS 測試站 `release/test/backend`；部署 DLL 與 Release 建置雜湊一致，`http://172.16.110.27:8081/health` 回應 200。
- 經確認修正測試環境 N2 化鎳槽規格對調：亞磷酸鈉（PPC 2594）改為 LSL 17、Target 18.5、USL 20；次磷酸鈉（PPC 2595）改為單邊 USL 100。更新前完整備份至 `PartProcessCharacteristics_spec_backup_20260824`。
- 次磷酸鈉（QualityCharacteristic 824）與亞磷酸鈉（825）輸入模式由 `DIRECT` 修正為 `FORMULA`、值標籤改為「分析值」，讓兩項顯示滴定值 1／2 並套用 `Secondary - Primary`；更新前備份至 `QualityCharacteristics_inputmode_backup_20260824`。

## 2026-08-20（測試主檔同步至正式環境）

- 將測試資料庫 `PMR_SPC_TEST` 已確認的主檔調整同步至正式資料庫 `PMR_SPC_2026`：更新 4 筆品質特性（其中 `P500A` 的輸入模式調整為 `FORMULA`）及 40 筆 SPC 管制項目的化學分析設定。
- 未同步測試環境的量測、SPC 計算、匯入批次／明細及警示等交易資料；正式庫原有 `VariableMeasurements` 2 筆、`UploadBatches` 1 筆、`UploadDetails` 2 筆、`AlertEvents` 2 筆均維持不變。
- 同步前建立並通過 SQL Server `RESTORE VERIFYONLY ... WITH CHECKSUM` 驗證的完整 `COPY_ONLY` 備份：`PMR_SPC_2026_pre_test_master_sync_20260820_154126.bak`；同步後 14 個維護主檔逐欄比對皆無差異。

## 2026-08-18（SPC 401 自動導回登入）

- SPC 任一 API 回傳 401 時會清除失效登入狀態並立即導向登入頁，不再停留於管制圖顯示「無法載入總覽資料：未授權」。
- 登入網址保留原頁面路徑、查詢參數與錨點，重新登入後可返回原操作頁；登入 API 本身回傳 401 時不重複導頁。
- 回歸檢查：失效 Token 進入 SPC 總覽會導至登入頁；正常 Token 可載入；登入失敗仍停留登入頁顯示錯誤。

## 2026-08-17（管制項目圖表顯示方式 400 修正）

- 正式 SPC 啟用 CHEM `CHEM_TREND`，並建立 PROCESS `PROCESS_TREND` 趨勢圖大類；既有 114 個 CHEM 與 12 個 PROCESS 管制項目未被自動切換，量測資料不變。
- 管制項目編輯頁依業務範圍與資料型態過濾管制圖類型，避免 CHEM 誤選 PROCESS 圖型或 PROCESS 誤選 CHEM 圖型而回傳 400。
- 沒有啟用趨勢圖大類的業務範圍會停用趨勢圖選項並顯示原因；CHEM／PROCESS 現已可個別切換趨勢圖。
- 管制項目建立／更新的後端 400 統一回傳中文 `message`，前端亦支援純文字錯誤，不再只顯示 `Request failed with status code 400`。

## 2026-08-17（咬蝕量 PROCESS 管制項目）

- 正式 SPC 的 PROCESS 群組新增單值－移動全距圖 `I-MR`，供每日單筆製程摘要量測使用，不影響原 CHEM `I_MR`。
- 建立 `ETCH_A_AVG`、`ETCH_B_AVG`、`ETCH_RATE` 三個計量型品質特性，並為 PT1、PT2、QE1、QE2 建立共 12 個啟用的 PROCESS 管制項目。
- A/B 平均咬蝕量與 ER 規格依 `PD-3-581-06B-咬蝕量_2026.xlsx` 四條線標準設定，樣本數為 1。

## 2026-08-17（非對稱規格上下公差顯示）

- 非對稱規格改以目標值右側上下兩行小字顯示，例如 `18.5` 右側上方 `+0.5`、下方 `-1.5`；對稱規格維持單行 `8.5 ± 0.2`。

## 2026-08-17（藥液規格上下限修正）

- 修正正式 SPC 18 個藥液項目將 LSL／USL 存反的主檔資料；17 項交換上下限，DP 抗氧化 PH 改為單邊 `USL=5、LSL=null`。
- N2 化鎳槽次磷酸鈉由錯誤 `LSL 19～USL 17` 修正為 `LSL 17～USL 19`，量測錄入規格與 SPC 判定同步使用正確界限。
- 修正前備份：`.codex-tmp/ChemicalFormulaImporter/spec-backup-20260817_101252.json`；修正後啟用藥液項目 `USL < LSL` 數量為 0。

## 2026-08-17（藥液分析項目專屬公式）

- 藥液管制項目可在檢驗基準進階設定中個別啟用濃度、調整動作及調整量公式，並設定輸入名稱、公式版本與小數位。
- 藥液設定獨立保存於 PartProcessCharacteristic 的 `ChemicalAnalysisConfigJson`；`FormulaConfigJson` 僅保存 SPC 管制圖計算公式，兩者不共用欄位。Portal 優先使用項目專屬藥液公式，未設定時沿用 Excel 公式目錄。
- 公式僅允許受限的數值、比較、IF／IFERROR／ROUND 運算，不執行 JavaScript 或任意程式碼。
- 已將 `sample-data/藥液分析日報表.xlsx` 轉換規則依線別、槽體與分析項目匯入正式 SPC：114 個啟用項目中 84 項唯一配對並寫入；20 項無對應公式、10 項有開線／收線歧義而保留未設定，避免錯誤覆寫。
- 匯入前原設定備份：`.codex-tmp/ChemicalFormulaImporter/formula-backup-20260817_094447.json`。

修改 Portal 或 SPC 前必須先閱讀本檔；已確認功能不可自行恢復、改名或移除。

## 2026-08-17：Portal 藥液量測每日唯一資料

- Portal 藥液量測以管制項目與台北日期每日唯一；同日再次送出更新原量測、重算 SPC 與警報，不新增重複量測點。
- 新增 `PortalDailyDate` 與條件式唯一索引；Excel、TransFiles 及一般 API 匯入不受每日覆寫規則影響。

## 2026-08-14：管制項目製程下拉選單簡化

- 「SPC 管制項目設定與規格維護」頁面的製程篩選與新增／編輯製程下拉選單只顯示製程名稱，移除 `/ 英文名稱或代碼` 尾碼；例如「清洗線(CN) / CN」改為「清洗線(CN)」。

## 2026-08-14：管制項目槽位下拉選單簡化

- 「SPC 管制項目設定與規格維護」頁面的槽位篩選及編輯下拉選單只顯示槽位名稱，不顯示槽位代碼；內部槽位 ID 與資料關聯維持不變。

## 2026-08-14：正式 SPC 量測交易資料清除

- 依使用者明確確認，清除正式資料庫 `PMR_SPC_2026` 的量測、SPC 計算、匯入批次／明細／錯誤及量測告警資料；分析項目、規格、製程、線別、槽位、槽體、管制群組、管制圖類型及管制項目主檔保留。
- 清除前建立並通過 SQL Server 驗證的完整 `COPY_ONLY` 備份：`PMR_SPC_2026_pre_measurement_cleanup_20260814_151954.bak`。
- 清除筆數：VariableMeasurements 2,558、SpcCalculationResults 2,333、UploadBatches 3、UploadDetails 2,793、UploadErrors 1、AlertEvents 30；AttributeMeasurements 與舊版 MeasurementBatches／MeasurementValues 均為 0。
- 交易內核對保留主檔：PartProcessCharacteristics 152、QualityCharacteristics 46、Processes 28、Machines 16、ProductionLines 28、Tanks 64、Slots 0、ControlChartGroups 7、ControlChartTypes 4。

## Portal：咬蝕量資料輸入

- 頁面：`/QualityAssurance/EtchAmountEntry`，由品保專區入口進入，僅限品保角色。
- 使用 SPC `PROCESS` 製程管制中名稱或代碼含「咬蝕／ETCH」的既有品質特性與規格。
- 固定輸入左、中、右三個測點的蝕刻前、蝕刻後厚度，依「蝕刻前－蝕刻後」自動計算咬蝕量，單位為 `μm`。
- 即時計算三點平均、最小、最大及全距，依 LSL／USL 顯示合格或超規；超規資料送出前須再次確認。
- 批次寫入 SPC 時，以樣本編號 1／2／3 表示左／中／右，並保存工單、批號、料號／產品別、量測時間及量測人員。
- 頁面提供同製程近期咬蝕量資料查詢；未建立相符 SPC 主檔時禁止送出並提示先完成設定。

## Portal：藥液量測資料錄入

- 頁面：`/Spc/VariableMeasurementEntry`
- 輸入介面採藥液分析日報表的一筆一列格式，固定欄位順序：
  - 槽位、分析項目、規格、範圍、濃度、判定、調整、調整量、複驗濃度、複驗判定。
  - 線別由表格上方選擇，不在表格內重複顯示。
  - 槽位移除線別代碼前綴（例如 `N2-C1` 顯示 `C1`），槽位及分析項目皆不顯示括號名稱。
- 可中途新增槽位或分析項目，重新載入後保留尚未送出的輸入。
- 藥液量測錄入頁不顯示滴定數欄位；濃度與複驗濃度均由使用者直接輸入。
- 「規格」欄顯示藥液分析規則中的文字規格（Specification）；「範圍」顯示 LSL～USL。
- 複驗後即使結果為 OK，原始 NG 判定仍須在畫面與資料中保留。
- 判定須優先採用複驗值。
- 原判定與複驗判定皆為 OK 顯示綠底、NG 顯示紅底，文字使用黑色。
- 不顯示獨立的「複驗、調整與備註（選填）」區塊標題，相關輸入集中於表格。
- 調整量是文字，不限制為數字。
- 量測值四捨五入至小數第 2 位；查詢列表也固定顯示 2 位。
- 量測時間欄即時顯示現在時間；正式資料採使用者最後按下確認送出的時間，精確到秒。
- 可一次批次送出多筆；成功後清空量測內容。

## SPC：即時互動管制圖

- 所有管制圖與管制項目統一使用 `WE` 八大規則群組；目前僅規則 1 啟用，規則 2～8 停用，不支援項目專屬勾選。
- 舊資料重新查詢即可重新判定，不需重新匯入。
- 規格界限 `USL / Target / LSL` 預設不勾選。
- 不顯示「微調公式配置」。
- `I-MR CHART` 只顯示 `I-MR`。
- 管制圖類型選單及設定摘要中的 `I_MR (藥液單值-移動全距圖)` 只顯示 `I_MR`。
- 不另外開放「管制圖種類維護」選單；在「SPC 管制項目設定」編輯畫面可直接修改目前綁定種類的顯示名稱，更新後套用至所有使用該種類的項目。
- 「SPC 管制項目設定」直接顯示共用 `WE` 群組已啟用的規則；停用規則不顯示。
- SPC 規則庫固定為西方電氣規則 1～8；`CT_RULES_*`／`PPC_RULES_*` 群組及副本已移除，系統不再建立專屬群組，並禁止新增第 9 種規則或刪除固定規則。
- SPC「管制圖監控明細」依目前 `ppcId` 強制取得管制項目主檔；製程、特性、機台及槽位不得因總表製圖流程顯示 `[-] -`，槽位優先使用 `Slot`、其次使用 `Tank`。
- SPC 即時互動管制圖不顯示管制界線計算方式及公式文字。
- SPC 即時互動管制圖暫時隱藏「更新管制界線」按鍵，仍保留管制界線數值顯示。
- SPC 即時互動管制圖以固定「放大／重設」按鍵控制 X 軸範圍；放大按鍵每次放大一級，取消滑鼠框選及滑鼠跟隨縮放。
- SPC 即時互動管制圖的「放大／重設」改用 Icon 顯示，滑鼠停留時顯示功能提示。
- SPC 即時互動管制圖的「放大／重設」Icon 放回圖表右上角原 ECharts 工具列位置。
- SPC 即時互動管制圖的縮放 Icon 改為固定覆蓋於圖表右上角工具列，避免 ECharts 自訂 Icon 未渲染而看不到。
- SPC 雙層管制圖個別顯示子圖名稱：I-MR 為「I 個別值管制圖／MR 移動全距圖」，Xbar-R、Xbar-S 顯示對應名稱。
- SPC 雙層管制圖子圖名稱置中，並調整字級與圖表間距，避免與座標軸或圖面文字重疊。
- SPC I 個別值管制圖顯示 X 軸量測順序／日期時間標籤，過密時自動隱藏重疊文字。
- SPC 管制圖加入置中線條圖例，說明量測值、UCL、CL、LCL，以及 MR／R／S 線條。
- SPC I／Xbar 圖的動態 UCL、CL、LCL 線尾顯示名稱與數值，與 MR／R／S 子圖標示方式一致。
- SPC 從總表點擊製圖時，監控明細優先顯示 PPC 主檔，主檔關聯缺少時改用總表列的線別、槽位及管制項目名稱，不再顯示空白 `[-] -`。
- SPC 即時互動管制圖不顯示「子組大小 (Subgroup N)」。
- SPC 即時互動管制圖與趨勢圖的量測起迄日預設為最近 3 個月，單次查詢最多 93 天。
- SPC 新增「SPC 月管制圖」頁面與選單，位於量測值趨勢圖下方；查詢條件及製圖功能沿用即時互動管制圖，預設最近 1 個月且最多查詢 31 天。
- SPC 月管制圖改為報表確認模式：選月報及月份即查當月 1 日至月底；選週報及週次即查該週週一至週日，不開放手動輸入量測起迄日。
- SPC 管制項目總覽移除「上月 OOS、上月 %OOS、上月 Cpk」三欄。
- SPC 管制項目總覽加入「匯入資料數」，依目前查詢條件與日期範圍顯示原始匯入資料筆數。
- SPC 管制項目總覽保留表格底部原生水平捲軸；表格底部離開視窗時，於瀏覽器底部顯示同步浮動水平捲軸。
- SPC 管制項目總覽的槽位只顯示括號前名稱，移除 `()`／`（）` 及其中重複內容；完整名稱保留於滑鼠提示。
- 原「SPC 月管制圖」選單與頁面標題更名為「SPC 週月報表」，路徑及功能不變。
- SPC 管制項目總覽的線別只顯示 `/`／`／` 前名稱，例如「鍍銅線(C1) / C1」顯示為「鍍銅線(C1)」；完整名稱保留於滑鼠提示。
- 「SPC 週月報表」總覽依週期顯示上月或上週的 OOS、%OOS、Cpk；一般 SPC 管制圖維持不顯示，互不影響。
- 從「SPC 管制項目設定」直接查看管制圖時，監控明細會由實際量測資料的 LineId／TankId／SlotId 查回線別與槽位主檔，不再只依賴 PPC 關聯而顯示「未提供」。
- SPC 雙層管制圖的下方 MR／R／S 子圖加入獨立圖例，置於下圖標題下方。
- SPC 圖表線型統一：UCL／CL／LCL 管制線使用實線，USL／LSL 規格線使用虛線。
- 量測值趨勢圖開放直接選取既有 `CONTROL_CHART` 管制項目及其 `VariableMeasurements`，同一批量測資料可同時用於 SPC 管制圖與趨勢圖，不複製資料。
- 量測值趨勢圖移除「模組指南」區塊。
- 量測值趨勢圖從總表製圖後提供「返回已查詢總表」，並保留原查詢結果與條件。
- 量測值趨勢圖的「原始量測值趨勢」標題僅顯示中文，不附英文名稱。
- 量測值趨勢圖的 Target 標示統一改為中文「目標值」。
- 量測值趨勢圖支援點擊量測點查看明細，顯示量測時間、量測值、規格與管制判定、統計狀態、來源資料、各界線差距，並可切換上一筆／下一筆。
- 量測值趨勢圖固定顯示圖表名稱；由總表製圖時依管制項目 ID 精準取得線別、槽位與管制項目名稱，避免同製程同特性項目誤取第一筆。
- 量測值趨勢圖的 Y 軸範圍納入量測值、USL、LSL、目標值及已勾選管制線，避免工程規格線落在量測值範圍外而被裁切。
- SPC 管制項目設定中的規則 1～8 僅顯示中文規則名稱，不顯示 `Rule*_...` 英文代碼。
- SPC 規則代碼在判定明細與規則庫畫面簡化顯示為 `Rule 1`～`Rule 8`，內部原始代碼維持不變。
- Portal 新增「品保專區」(`/QualityAssurance`)，將「藥液量測資料錄入」移入專區；首頁、功能連結及手機版品保入口皆改連至品保專區。
- 「SPC 週月報表」月報總覽加入 OOS、%OOS、Cpk 差異欄，計算方式皆為本月減上月；一般 SPC 管制圖不顯示。
- 「SPC 管制項目設定與規格維護」每筆資料分為兩個獨立按鍵：「查看 SPC 管制圖」與「查看趨勢圖」，兩者共用同一筆量測資料。
- 「SPC 管制項目設定」建立項目時，若未手動選取管制圖類型，系統自動帶入相符之預設管制圖類型（如 I-MR），避免儲存時拋出 400 錯誤。
- 查詢提示顯示「查詢中...」。
- `UCL(R/MR)`、`CL(R/MR)`、`LCL(R/MR)` 分別只顯示 `UCL`、`CL`、`LCL`。
- 總表不顯示：
  - 管制圖種類
  - 管制界線計算方式
  - 圖表類型
  - 工程負責人
  - 備註
- 能力指標不顯示製程能力、製程能力下限、實測不良。
- SPC 管制圖上方摘要移除黑底 Cp、Cpk 卡片及「組內標準差」，保留下方能力指標區與週月報比較資料。
- 顯示名稱採 `Cp`、`Cpk`，不顯示 `Pp`、`Ppk`。
- SPC 選單不顯示「藥液主表」；`/chemicals` 頁面停用並導回 SPC 管制圖，資料庫既有藥液資料保留。

## SPC：計量型資料匯入

- 選擇管制群組後，依群組的 `BusinessScopeCode` 自動送出 `ControlScope`。
- 藥液群組自動使用 `CHEMICAL`，匯入欄位必須可對照「槽位（TankCode）」。
- 相容 TransFiles 欄位：類別、線別、槽位、管制項目、量測日期、量測時間、量測員。
- 藥液可由線別代碼／中文名稱反查機台及所屬製程；槽位、管制項目可用代碼或中文名稱。
- 預覽顯示原始名稱對應的製程、線別、槽位及管制項目代碼。
- 重複鍵為管制設定＋量測時間＋批號＋樣本號；預設略過重複，可手動選擇覆蓋。
- 中文名稱若無法唯一對應主檔，不可自動選第一筆，該列必須驗證失敗。
- 藥液匯入遇到不存在的槽位時須顯示該列驗證錯誤，不可因空槽位 ID 造成整批 API 例外。
- TransFiles 線別代碼若找不到，須再嘗試「原代碼＋1」，例如 `PT／QE／ST` 對應 `PT1／QE1／ST1`。
- TransFiles 管制項目已拆除括號單位時，SPC 以「管制項目＋單位」對應含單位的品質特性代碼。
- 匯入預覽的主檔與業務範圍錯誤使用中文，缺少「線別＋槽位＋管制項目」設定時提供前往品質特性設定頁的入口。
- 匯入預覽可批次建立缺少的「線別＋槽位＋管制項目」設定；優先複製同項目既有設定，否則採該藥液群組的 I-MR，完成後直接重新驗證原批次，不需重傳檔案。
- 藥液管制項目比對須限定 `CHEM` 業務範圍，避免 PRODUCT／PROCESS 同名造成歧義；確實不存在時，批次建立功能可直接建立計量型品質特性主檔。
- 批次建立品質特性前須重用已存在的原代碼或 `_CHEM` 代碼；若仍重複才產生遞增後綴，不可觸發唯一索引錯誤。
- 匯入預覽遇到 `CHAR_NOT_FOUND` 或 `MAPPING_NOT_FOUND` 都必須顯示「批次建立並重新驗證」。
- 匯入預覽只顯示未通過資料，通過資料整列不顯示；整批零錯誤時，使用「僅新增、重複略過」模式自動確認匯入。
- 匯入時必須驗證全部資料；預覽須顯示所有未通過資料，不可只取前200列。通過資料不顯示，但保留一筆供匯入後導向管制圖。
- 計量型資料匯入需顯示真實進度條，以後端已處理筆數／總筆數計算百分比，完成後自動進入結果頁。
- 「批次建立並重新驗證」也需顯示後端真實已驗證筆數／總筆數及百分比，完成後自動刷新結果。
- 「批次建立並重新驗證」完成後，預覽 API 禁止快取，並立即刷新成功／錯誤筆數及下方錯誤明細。
- 缺少設定提示須顯示比對規則、可直接建立筆數、受線別／槽位阻擋筆數；執行後顯示建立品質特性、建立設定、重用及各種略過原因，不可只顯示0筆。
- 槽位先以代碼／完整名稱比對；找不到時，可在同線別內以唯一簡稱前綴比對（例如除鈀→除鈀槽）。若有多個候選必須維持錯誤，不可任選。
- 槽位別名：匯入值「表處」對應主檔「表面處理」。
- 批次建立時，槽位先採精確、別名及唯一相似主檔；確實不存在才於該線別自動建立。相似結果不唯一時不得任選或新增。
- 匯入製程及線別先比對代碼／名稱，再以去除空白、符號及「製程／線別／機台」字樣後的唯一模糊結果對應主檔；多筆相似時不得任選。
- 製程或線別無法唯一比對時，錯誤列後方提供製程／線別主檔選單；套用後更新該列暫存值並重新驗證整批，不需重新上傳。
- 匯入預覽不提供主檔選擇或批次對應功能；維持自動模糊比對、槽位自動建立及批次建立重新驗證。
- 「匯入資料檢核與異常對照預覽」改為接近全寬版面，縮小左右留白以放大明細表格。
- 品質特性須優先從實際「線別＋槽位」既有 CHEM 設定反查；舊值 CHEMICAL 僅作輸入相容並轉為 CHEM，不可因 CHEM／CHEM_TREND 歷史值混用而選錯；支援 H2SO4、HCL、Cu2+、H2O2、KOH、Cl-、Na2CO3、HNO3、SPS 化學式別名。
- 「確認轉入正式 SPC 運算」使用綠色主按鈕。
- 匯入批次 ID 產生須相容不支援 `crypto.randomUUID()` 的舊版瀏覽器。
- TransFiles 的「規格」與「範圍」欄位須在確認匯入時更新藥液管制項目的 TargetValue、LSL、USL；只有預覽不得改動主檔。
- 計量型匯入欄位自動對照須優先精確匹配「量測值／測量值／測定值／分析值／檢測值」，不可由單字「值」誤配規格等欄位；同一來源欄位不可重複指派給多個系統欄位。
- 藥液預覽若線別、槽位及品質特性已存在但匯入單位尚未建立，提示可由「批次建立並重新驗證」複製同項目設定並補建該單位管制項目。
- `PartProcessCharacteristics` 唯一索引須包含 `Unit`，允許同一線別、槽位與品質特性依不同單位建立各自的 SPC 管制項目，避免批次補建單位時發生重複索引錯誤。
- `UNIT_MISMATCH` 須納入「批次建立並重新驗證」範圍；Excel 單位與既有主檔不同時，自動依匯入單位補建設定，重新驗證後清除單位差異及缺少管制項目錯誤。
- 計量型匯入列若 Excel 單位空白但預覽已解析出正確 `ResolvedUnit`，補建其他單位後重新驗證須沿用該解析單位，不得誤判為多筆設定；`MAPPING_AMBIGUOUS` 可再次批次重新驗證。
- 批次補建管制項目遇到相同唯一鍵但已停用的設定時，須重用並重新啟用既有資料，不可新增後觸發唯一索引衝突；比對須包含 `SlotId`。
- 計量型匯入的單位空白且同一線別、槽位、品質特性有多個單位設定時，須以 Excel 規格的 Target／LSL／USL 唯一比對管制項目並寫入 `ResolvedUnit`；無法唯一判斷才顯示多筆設定錯誤。

## 發布前回歸檢查

- SPC 管制圖須沿用量測值趨勢圖的外框與操作層級（頁首、分類切換、查詢卡、狀態提示、監控摘要及圖表卡），但不可移除 SPC 管制界線、能力指標、異常規則與 OCAP 功能。
- SPC 管制圖的藥液清單只顯示 `CONTROL_CHART`，量測值趨勢圖的藥液清單只顯示 `TREND_CHART`；兩頁線別下拉選單皆依製程主檔 `SequenceNo`、ID 排列。
- Portal：
  - 切換製程／線別後，槽位與分析項目矩陣正確。
  - 公式換算、原判定、複驗判定及調整量文字可用。
  - 批次送出後可在 Portal 查詢及 SPC 管制圖查到。
  - 送出時間為最後確認時間，量測值顯示 2 位小數。
- SPC：
  - 僅顯示已勾選規則的違規結果。
  - 規格界限預設未勾選。
  - 已移除欄位與文字沒有重新出現。
  - I-MR 標題及管制線名稱正確。

## 維護紀錄

- 2026-08-11：SPC 管制圖監控明細的「槽體」只顯示名稱，不再顯示前置代碼；例如「[TANK-000306] 顯影槽Developer Tank」改為「顯影槽Developer Tank」。
- 2026-08-11：SPC 管制圖監控明細的「機台」只顯示名稱，不再顯示前置代碼；例如「[DV2] DV2」改為「DV2」。
- 2026-08-11：SPC 管制圖改以量測值趨勢圖的外框為主，統一頁首、分類切換、查詢區、載入／錯誤狀態、內容間距及監控摘要卡片；保留原有 SPC 計算、管制界線、能力指標、異常清單與 OCAP 操作。
- 2026-08-11：趨勢圖規格線、管制線及統計卡片的界線名稱只顯示 `USL、LSL、UCL、CL、LCL`，移除「工程上限／下限、管制上限／下限、平均線」等括號文字。
- 2026-08-11：SPC 管制圖與趨勢圖的線別資訊及線別下拉選單只顯示名稱，不顯示前置代碼；例如「[N2] 化鎳線(N2)」改為「化鎳線(N2)」。
- 2026-08-11：SPC 管制圖監控明細與趨勢圖檢驗特性只顯示分析項目名稱，不顯示 `CHAR-*` 代碼；例如「[CHAR-000851] PH」改為「PH」。
- 2026-08-11：SPC 管制圖與趨勢圖名稱只顯示製程線別名稱、槽位名稱及分析項目，不再重複附加機台名稱或槽位代碼；例如「化鎳線(N2) / N2 - 表處 (TANK-000269) - PH」簡化為「化鎳線(N2) - 表處 - PH」。
- 2026-08-11：藥液管制圖與趨勢圖依 `DisplayMode` 分流；SPC 管制圖及量測值趨勢圖的線別下拉選單改依製程主檔輸入順序（`SequenceNo`、ID）排列。
- 2026-08-11：依一次性分類預覽的高信心結果，將 `PMR_SPC_2026` 中 29 筆「藥液趨勢監控」設定由 `CONTROL_CHART` 改為 `TREND_CHART`，並依既有趨勢模式規則清除 ChartTypeId、FormulaConfigJson、UCL、CL、LCL；交易內29筆全數驗證成功。33 筆需人工確認、16 筆原檔分類空白、12 筆製程管制皆未修改，執行前備份保留於分類報告目錄。
- 2026-08-11：新增一次性 `SpcChartModeClassifier` 只讀分類工具，依月報的管制類別、線別、Chart Name、槽位、分析項目別名與規格產生圖表模式比對預覽；第一階段只輸出確認 CSV，不更新 `PartProcessCharacteristics`。
- 2026-08-10：藥液計量型匯入批次 `8677974e-e850-46b4-8180-0ae2031699a1` 補建 17 組單位管制項目；102 筆錯誤完成重新驗證，1,391 筆全數通過。
- 2026-08-10：新藥液批次 `bb198210-4382-40f0-93ce-e226768481f6` 的 64 筆錯誤完成補建與規格比對；1,240 筆通過，僅第 847 列因原始量測值空白保留錯誤。
- 2026-08-10：依確認從上述尚未匯入批次刪除第 847 列空白量測資料；批次重算為 1,240 筆有效、0 筆錯誤，維持 `PreviewReady`。

- 2026-07-23：建立客製需求與回歸檢查基準。
- 2026-07-24：SPC 藥液業務範圍統一為 `CHEM`，舊輸入 `CHEMICAL` 自動正規化為 `CHEM`。
- 2026-07-24：SPC 舊 `QE` 製程、線別、產線、設定及未完成匯入暫存資料合併至既有 `QE1`。
- 2026-07-24：SPC 舊 `ST` 製程、線別、產線、設定及未完成匯入暫存資料合併至既有 `ST1`。
- 2026-07-24：SPC 舊 `PT`、`DV` 製程與相關主檔、設定及未完成匯入資料分別合併至 `PT1`、`DV1`；DV 歷史量測保留原關聯。
- 2026-07-29：SPC 選單與頁面標題「SPC 週月報確認」更名為「SPC 週月報表」，路徑及功能不變。
# 2026-08-12 — 所有管制項目統一使用 WE 八大規則

- SPC 判異、圖表計算與新量測告警固定讀取啟用中的 `WE` 八大規則，不再依管制項目或管制圖小分類選擇規則群組。
- 新增、編輯與匯入管制項目時不再建立項目專屬規則綁定；系統啟動時會清除既有項目綁定，並將所有管制圖類型統一指向 `WE`。
- 管制項目與管制圖設定頁的八大規則改為唯讀顯示，儲存時不再送出專屬規則設定。
- `WE` 規則群組禁止停用、改碼或刪除，八條固定規則禁止停用。

# 2026-08-20 — SPC 正式／測試交付包分離

- SPC 發布統一為 `release/test/backend`、`release/test/frontend`、`release/production/backend`、`release/production/frontend`。
- 測試後端固定連線 `PMR_SPC_TEST`，正式後端固定連線 `PMR_SPC_2026`；每套後端只保留一個 `ConnectionStrings.SqlServer`，不在主機上切換資料庫。
- 測試與正式前端分別以 `testhost`、`production` 模式建置，每套前端只含一個 `VITE_API_BASE`。
- 新增 `scripts/publish-environments.ps1`，一次重建兩套可直接複製的交付包並產生版本清單。

# 2026-08-20 — Portal 單一登入按需交換 SPC Token

- 新增 `POST /api/v1/auth/portal-sso`，以 Portal 與 SPC 共用的 HMAC 金鑰驗證帳號、時間戳與一次性隨機值，核發 60 分鐘個人 SPC JWT。
- SSO 僅允許已啟用且帳號相符的 SPC 操作者，不傳遞 Portal 密碼、不使用共用管理員身分，請求超過 60 秒即拒絕。
- Portal SSO 帳號統一移除網域前綴與 Email 後綴並轉為小寫；具 Portal 品保權限的簽章使用者首次進入時自動建立個人的 SPC `Editor` 操作者，已停用帳號仍拒絕登入。
## 0.1.52 - 2026-09-02

- Excel 變量資料上傳新增 `portalDaily=true`，支援藥液日報依日期與開／收線階段更新。
- 匯入欄位支援 `SamplingPhase`、`採樣階段`及`開收線`。
# 2026-09-02 修正 PmrPortal SSO 中文姓名亂碼

- SPC Web 解析 PmrPortal JWT 時改用 UTF-8 解碼，避免中文顯示姓名在右上角變成亂碼。
- SPC Web 版本 `0.1.50`。

# 2026-09-02 PmrPortal 單一登入入口

- SPC Web 新增 `/portal-sso` 入口，接收 PmrPortal 既有短效 SPC 權杖後立即清除網址片段、建立同一操作者登入狀態並導向管制圖。
- SPC Web 版本 `0.1.49`。

## 2026-09-17（正式業務資料同步 SPC 測試庫）
- 依使用者核准從 PMR_SPC_2026 同步 41 張業務資料表至 PMR_SPC_TEST；保留測試人員、權限、通知、設備設定、校正資料與新版結構共 18 張表。
- 量測資料 1838→2424 筆；60 筆校正儀器、23 筆人員與50個 migrations 保留。來源只讀，未進行 Daniel 帳號整併。
- 測試完整備份並 VERIFYONLY 通過；交易逐表筆數／雙向內容比對、保留資料 SHA256、外鍵檢查通過。API、health及前端 200，無程式發布、正式站未修改。
- [規格與備份驗證](specs/20260917-production-data-refresh/verification.md)。

## 2026-09-17（藥液歷史查詢完整性）
- 解除Portal每製程200筆限制；日期先由SPC過濾，穩定排序分頁，總數及關鍵字涵蓋完整載入資料。多頁失敗／重複／總數變动提示重查，過期回應不覆蓋新查詢。
- 15後端、4載入及3頁面回歸通過；已發布SPC API與Portal API/Web測試站，備份history-complete-20260917-114615。真實畫面待重新登入；正式站未發布。
- [規格與驗證](specs/20260917-chemical-history-complete/verification.md)。

### 藥液歷史查詢驗收完成
- 使用者重新登入後，Portal測試頁全部2424筆、N2共451筆、N2於2026-08-03共20筆，與SQL預期一致。未寫入量測資料，未重發布。

## 2026-09-17（藥液日期狀態日曆）
- 自訂月曆：日報深藍圓點、歷史菱形、無資料灰字、選取藍框。依線別／班別／階段唯讀月查詢；選日期保留未送出內容，不自動載入。
- 17後端與11個前端案例通過；SPC API與Portal API/Web測試發布，備份calendar-status-20260917-120558，設定保留。正式站／DB未修改；真實畫面待登入驗收。
- [驗證](specs/20260917-chemical-date-status/verification.md)。

### 藥液日期狀態：真人登入驗收通過
- C1早班9/15／中班9/16的已有資料、N2開收線歷史提示與日曆顏色／選取框均正常；只選日期不自動載入。未寫入量測或重發布。

## 2026-09-17（舊日報未標班別視為早班）
- 依使用者確認統一GENERAL／空白為早班；日曆、日報載入、修改及重複檢查一致，修改沿用ID，衝突拒絕。N1/N2階段界線不變。
- 21項測試及建置通過；SPC測試API已發布，備份legacy-morning-20260917-131432。真實Portal C1 2026-09-08早班成功載入6筆，中班不混入。未寫入真實量測、未發布正式站。
- [驗證](specs/20260917-legacy-morning-shift/verification.md)。

## 2026-09-17（缺日報日期的歷史量測載入）
- 缺PortalDailyDate時使用量測日期；日曆、載入、預覽、更新一致，更新原ID並保留日期稽核；重複拒絕。N1/N2舊CLOSE依最新指示不處理。
- Chemical 24案通過，追加預覽斷言單案通過；Release建置及測試API發布完成，備份legacy-measurement-date-20260917-132827。
- 真實Portal C1 2026-08-28早班成功載入6筆；未送出真實量測、未發布正式站。[驗證](specs/20260917-legacy-measurement-date/verification.md)。

## 2026-09-17（9月咬蝕量匯入第一批盤點）
- 只完成唯讀解析與SDD計畫：22張9月表，13張有資料，3900原始咬蝕量點；4份負值日報待處理。
- 已列出PT背面平均漏列、50點X̄-S常數缺漏、PT2/線速主檔缺漏及完整點同步方案。未修改程式、資料庫，未測試或發布。
- [盤點及分批計畫](specs/20260917-etch-september-import/analysis.md)，下一批待核准。

## 2026-09-17（咬蝕第二批完成）
- 完整25/50點子組同步、50點X̄-S、線速、Portal日期載入防護；原摘要不混新統計，保留資料。
- 31核心/回歸＋3權限＋3Portal＋4解析案例分次通過；16實際图API、300原始點與兩庫對帳通過；0901四線4日報/308SPC列，重跑0新增。
- 四個測試元件已發布，備份etch-trial-20260917-171816；正式站/其他日期未匯入，人工畫面待驗收。[驗證](specs/20260917-etch-september-import/verification.md)。

## 2026-09-18（日報載入修復）
- Portal點位父導覽JSON循環已修正；隔離MVC兩例通過，Portal測試API已發布、health 200，人工畫面待驗收。[驗證](specs/20260918-etch-report-load/spec.md)。未重匯資料。

## 2026-09-18（9月咬蝕補匯準備）
- 擴充明確月份模式、日期線別複合鍵與未完整隔離；解析7例與建置通過，預覽48份，新增44份待核准。自動審查拒絕apply，尚未寫資料庫或發布。[紀錄](specs/20260918-etch-month-import/verification.md)。

## 2026-09-18（9月咬蝕第3批補匯完成）
- 明確授權後新增44份至兩測試庫，共48份；Portal3650點、SPC3746筆。16圖表逐組平均/S/速率/線速對帳通過，重跑48份皆Unchanged且ID/批次不變。
- 負值4份及9/18未完整4份隔離，正式庫未動，網站版本與發布不變。[證據](specs/20260918-etch-month-import/verification.md)。

## 2026-09-18（TransFiles預覽確認匯入測試站）
- SPC 0.1.58、Portal API 1.0.169、TransFiles2026.09.18.2已部署/打包；嚴格預覽/確認兩測試库，保留既有手動更新API。
- 50項相關測試通過，實站SPC只讀preview判定既有52筆略過，Portal匿名401，兩API health200。實站confirm未執行，需使用者登入驗收。[證據](specs/20260918-transfiles-etch-upload/verification.md)。

## 2026-09-18（歷史開收線資料修正）
- 依兩庫資料修正授權，先完成PMR_SPC_TEST的460筆，量測值/日期/ID及其他業務欄位不變，RowVersion正常遞增，備份及驗證已留存。
- PMR_SPC_2026無SamplingStage，正式尚未修改，需確認正式應用限定相容性升級；無網站發布。[驗證](specs/20260918-chemical-stage-repair/verification.md)。

## 2026-09-18（正式開收線相容升級盤點）
- 使用者已核准限定正式升級；核實遠端SPC0.1.57及PMR_SPC_2026。正式來源138份中126份可匹配，12份尚缺匹配來源；正式Portal位置亦待確認。尚未修改正式庫或發布，測試460筆成果保留。[核對紀錄](specs/20260918-chemical-stage-repair/verification.md)。

## 2026-09-18（N2中班收線資料修正）
- 使用者確認後測試庫30筆GENERAL改CLOSE，中班/數值/日期/ID保留，完整備份與交易驗證通過。三日實際daily API各載入10筆；無程式發布，正式未動。[紀錄](specs/20260918-n2-middle-close/spec.md)。

## 2026-09-18（N1/N2藥液開收線單線圖）
- N1/N2 CHEM不再按日期班別Map折疊/拆線，保留開線收線各點、班別與階段提示；圖表與試算依同序列算MR。
- 12個相關案例通過，測試API0.1.59、前端0.1.58已備份發布，實站N2 15點順序/MR對帳通過；正式站/資料庫未動。[驗證](specs/20260918-chemical-single-line/verification.md)。

## 2026-09-18（子組大小輸入上限50）
- 前端輸入max由25改50，保留min=1及整數步進；計算/API不變。內容檢查與測試模式建置通過，前端0.1.59備份發布，首頁/JS 200，設定保留。人工編輯50點待驗收。[規格](specs/20260918-subgroup-input-50/spec.md)。

## 2026-09-18（正式升級相容性隔離驗證）
- Portal8/SPC1項契約與5項離線發布檢核通過，產生8份EF migration審查SQL。正式OPEN/CLOSE已470筆，需更新轉換演練。
- 額外校正6/設備1遷移範圍待確認；未執行SQL Server升級、正式資料修正或發布。[驗證](specs/20260918-production-compatibility/verification.md)。
## [2026-10-01] Particle Monitoring Long Format 資料模型
- 新增 `ParticleMeasurements` 專用資料表模型與 EF migration，保留 Location、ParticleSize、Count、採樣資訊及 Excel 來源座標。
- 新增趨勢／位置比較／批次追溯索引、來源儲存格冪等唯一鍵、UploadBatch Restrict 外鍵與非負 Count constraint。
- 資料承載層完成後，migration 已由測試 API 啟動流程套用並確認；Particle 查詢 API／前端仍未實作，正式站未發布。
- 後續已完成 Particle preview／confirm API：Long Format staging 驗證、來源座標防重、跨批重複 `reject`／`skip` 與重送冪等；測試庫 migration 已由 API 啟動流程套用並唯讀確認。
- Particle 相關測試 3 passed、後端 build 通過；T-010 backend 已發布測試站，新 endpoint 未登入回 401，正式站未發布。
- 新增 Particle 原始量測分頁、單序列趨勢與 R1～R9 位置比較 API；缺測不補 0，重測歧義回候選事件。Particle 相關測試 5 passed，T-011 backend 已發布測試站，正式站未發布。
- 新增 Particle 專用 C-chart 與 SPC API：20 點門檻、同時間重測保留、bigint 精度保護、固定採樣基準警示，規格線與管制線分離。Particle 相關測試 9 passed，T-012 backend 已發布測試站，正式站未發布。
- Particle SPC 擴充 C/U 可選：U-chart 使用 decimal SamplingVolume、動態界線與明確單位；缺分母或混用單位回 422。相關測試 12 passed，單欄 migration 已套測試庫，T-012A backend 已發布測試站，正式站未發布。

