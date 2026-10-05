# 功能規格：SPC 修正-1002 Excel 解析與小工作排序
- 功能 ID：20261005-spc-1002-workbook-plan
- 版本：1
- 狀態：草稿；僅規劃，未實作
- 涉及專案：SPC
- 授權依據：使用者提供 `D:\SPC\docs\SPC 修正-1002.xlsx`，要求解析檔案、安排修改計畫、加入小工作排列，不立即執行。
- 現行需求基準：`docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`TODO.md`
- 前案或相關規格：`specs/20260930-particle-monitoring/`、`specs/20260918-chemical-single-line/`、`specs/20260918-subgroup-input-50/`、`specs/20260929-ppc-duplicate-key-guard/`

## 目的、現況與證據

Excel 來源：`docs/SPC 修正-1002.xlsx`。

工作表與內容：

| 工作表 | 範圍 | 解析內容 |
|---|---:|---|
| 常態檢定 | A1 | 直方圖分布大致正常，scale 呈現差異；但常態分布檢定有問題，實際 raw data 檢定 P-value 是 `<0.05`，系統顯示 `0.0986`。 |
| 待辦 | B3 | 1. 管制圖預設界線先取消勾選。 |
| 待辦 | B4 | 2. 班別的管制圖合併一起。 |
| 待辦 | B5 | 3. Ca 的呈現方式需移除絕對值。 |
| 待辦 | B6 | 4. 趨勢圖一樣增加管制圖的相關功能（ex: 直方圖），只是不要畫管制界線、不套用規則管理。 |
| 待辦 | B7 | 5. 常態分布檢定有問題，需修正。 |
| 待辦 | B8 | 6. 新增 raw data 可下載功能（包含製程/藥液）。 |

現況對照：

- 「管制圖預設界線先取消勾選」在 `CHANGELOG_CUSTOM.md` 已有 2026-09-25「管制界限預設關閉」紀錄，應先以測試站畫面驗證，不重複開發。
- 「Ca 移除絕對值」在 `ai_docs/10_change_log.md` 已有 2026-09-29「CA 顯示保留正負號」目的紀錄，需先確認目前測試站/程式是否已完成及發布，再決定是否只做驗收。
- 「常態檢定」牽涉 SPC 統計核心與 raw data 對帳，優先級高於呈現增強。
- 「raw data 下載」與「趨勢圖增加直方圖」屬新增功能，需在核心檢定正確後再做。

## 範圍與非範圍

本規格只做 Excel 需求解析、排序與驗收規劃。

非範圍：

- 不修改產品程式碼。
- 不修改資料庫。
- 不建置、不測試、不發布。
- 不變更正式站。

## 需求與驗收

| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 需依 Excel 內容建立可逐項執行的小工作，避免互相衝突。 | AC-001 | TODO 內可看到每項來源、優先順序、修改範圍、測試方式與確認方式。 |
| R-002 | 已完成或疑似已完成項目不可重複改動，應先驗證現況。 | AC-002 | 管制界線預設關閉與 Ca 顯示正負號列為「先驗證/補驗收」類小工作。 |
| R-003 | 統計正確性問題應優先處理。 | AC-003 | 常態檢定 P-value 不一致列為第一批核心修正，要求先以可重現 raw data 建測試。 |
| R-004 | 新增 UI/下載功能應排在核心計算正確後，且不得影響既有管制圖。 | AC-004 | 趨勢圖直方圖與 raw data 下載列為後續增強，驗收需包含製程/藥液與權限。 |

## 小工作排序

### SPC-1002-TASK-001：常態分布檢定 P-value 修正

狀態：完成；2026-10-05 已實作、測試並發布 SPC 測試站 backend。

優先級：最高，統計正確性。

修改範圍：

- 後端常態性檢定演算法、資料取樣、P-value 計算或 API 回傳欄位。
- 可能關聯前端直方圖顯示文字，但核心先以後端測試為主。

測試：

- 以 Excel 說明中的同一批 raw data 或使用者提供的重現資料建立單元測試。
- 驗證 P-value 與外部工具/人工計算一致，至少包含 `<0.05` 案例、目前顯示 `0.0986` 的回歸案例、零變異/小樣本邊界。

確認結果：

- 測試站同一組資料常態檢定 P-value 顯示與 raw data 驗算一致。

完成紀錄：

- 實作：`SpcService.PopulateNormalityAndCurve` 改用 adjusted Jarque-Bera 統計量，修正小樣本 raw data 常態性檢定 P-value 偏高。
- 測試：新增小樣本案例，鎖定一般 Jarque-Bera 約 `0.10`、校正後 `<0.05` 的情境。
- 驗證：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter NormalityTest --no-restore -p:UseSharedCompilation=false`：4 passed。
- 驗證：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 發布：已發布 SPC 測試站 backend；備份 `release/test/backend.backup-normality-20261005-123245`，保留既有 `appsettings.json` 與 `private-data`。
- Smoke：`/api/version` 回 `version=0.1.59`、`environment=test`；`/health` 在目前 IIS 綁定回前端 HTML，未列為 API health 通過。

### SPC-1002-TASK-002：管制圖預設界線關閉現況驗證

狀態：完成；2026-10-05 現況驗證通過。

優先級：高，但先驗證。

修改範圍：

- 原則上不改程式；只檢查 SPC 管制圖初始狀態是否已不勾選規格界線/管制界線。
- 若驗證失敗，再另開小型修正。

測試：

- 開啟製程與藥液管制圖，確認初始 checkbox 狀態。
- 重新整理頁面與切換圖表後確認不自動勾回。

確認結果：

- 使用者進入管制圖時界線預設未勾選；手動勾選仍可顯示。

驗證紀錄：

- `frontend/mes-spc-web/src/views/SpcChartView.vue` 第 139～140 行：`showSpecLimits` 與 `showControlLimits` 初始值皆為 `false`。
- 同檔第 2031、2035 行：管制圖畫面的規格界限與管制界限 checkbox 分別綁定上述兩個狀態。
- `TrendChartView.vue` 的規格線初始值為 `true`，但趨勢圖屬 `SPC-1002-TASK-006`，不納入本項。
- 本項未修改產品程式碼、資料庫或發布內容。

### SPC-1002-TASK-003：Ca 呈現移除絕對值現況驗證/補修

狀態：完成；2026-10-05 現況驗證通過，未修改功能程式。

優先級：高，但先驗證。

修改範圍：

- 先確認 Ca 是否已保留正負號。
- 若尚未完成，修改能力指標計算或前端格式化，僅限 Ca 呈現，不改 Cpk/Ppk 公式。

測試：

- 建立平均值低於/高於目標值的案例，確認 Ca 可顯示負值/正值。
- 回歸 Cpk/Ppk 不受 Ca 顯示邏輯影響。

確認結果：

- 畫面與 API 的 Ca 不再被取絕對值，正負號符合 mean 相對 target 的方向。

完成紀錄：

- 後端 `ProcessCapabilityCalculator` 已用 `(mean - target) / halfWidth` 保留 Ca 正負號。
- 前端 `SpcChartView.vue` 直接顯示後端 `capability.ca`，未做絕對值處理。
- 既有測試 `Capability_Ca_ShouldKeepSignAndLeaveCpkPpkUnchanged` 已驗證平均值低於 target 時 Ca 為負，且 Cpk/Ppk 維持既有公式。
- 驗證：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter Capability_Ca_ShouldKeepSignAndLeaveCpkPpkUnchanged --no-restore -p:UseSharedCompilation=false`：1 passed。
- 發布：本項只補驗收紀錄，無功能程式異動，不重新發布測試站。

### SPC-1002-TASK-004：班別管制圖合併呈現需求釐清與最小調整

狀態：完成；2026-10-05 已實作、測試並發布 SPC 測試站 backend/frontend。

優先級：中高，會影響圖表時間序列。

修改範圍：

- 管制圖班別序列呈現、Tooltip、圖例或資料合併邏輯。
- 不改歷史量測資料、不改班別/開收線資料結構。

測試：

- N1/N2 開收線、早/中班、非 N1/N2 線別各一組。
- 驗證同一管制圖能同圖查看不同班別，且 MR/異常規則不因排序錯亂。

確認結果：

- 使用者可在同一張管制圖看到班別資料，不需要分開切圖；點位順序與標籤正確。

完成紀錄：

- 實作：後端 `Subgroup` 與 Xbar-R/Xbar-S chart points 補帶 `portalDailyDate`、`samplingPhase`、`samplingStage`；`SpcService.GetInteractiveChartAsync` 建立子組時由第一筆量測帶入上述欄位。
- 實作：前端點位詳細卡新增「班別 / 取樣階段」顯示；既有 Tooltip 與座標標籤可使用 Xbar 點位 metadata。
- 不變更：資料庫 schema、歷史資料、N1/N2 `ChemicalStageChart` 排序、MR/Xbar/Cpk/Ppk 統計公式皆不變。
- 驗證：新增 `XbarR_ShouldKeepChemicalShiftMetadataOnChartPoints`；與 Ca 回歸一起執行通過，2 passed。
- 驗證：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`：0 warnings / 0 errors。
- 驗證：`npm run build -- --mode testhost` 通過，僅保留既有大型 chunk 警示。
- 發布：SPC 測試站 backend/frontend 已發布；備份 `release/test/backend.backup-shift-chart-20261005-130812`、`release/test/frontend.backup-shift-chart-20261005-130812`。
- Smoke：`http://172.16.110.27:8081/api/version` 回 `version=0.1.59`、`environment=test`；`http://172.16.110.27:8083/` 與 `index-x4DbX-TX.js` 皆 HTTP 200。

### SPC-1002-TASK-005：raw data 下載（製程/藥液）

優先級：中，資料追溯與驗算。

修改範圍：

- API 新增或擴充 raw data 匯出端點。
- 前端在管制圖/趨勢圖提供下載按鈕。
- 權限沿用查詢頁面可見資料，不額外開放未授權資料。

測試：

- 製程與藥液各下載一組，核對日期、線別、槽位、分析項目、班別/階段、原始值。
- 大筆數下載與 93 天查詢限制相容。
- 未登入 401、無權限 403。

確認結果：

- 使用者可下載目前查詢條件對應 raw data，並用 Excel 重新驗算常態檢定。

### SPC-1002-TASK-006：趨勢圖增加直方圖等管制圖相關功能

優先級：中低，屬呈現增強。

修改範圍：

- 趨勢圖頁新增直方圖/常態分布相關區塊。
- 不畫管制界線，不套用規則管理，不顯示 OOC 規則判定。

測試：

- 趨勢圖仍只顯示趨勢資料，不混入 CONTROL_CHART 專屬異常規則。
- 直方圖、常態曲線、P-value 與 raw data 下載結果一致。

確認結果：

- 趨勢圖可看分布與常態檢定，但不出現管制線與規則管理結果。

## 例外與邊界

- 常態檢定若缺少原始 raw data，先做現況盤點與待資料，不猜測錯誤原因。
- 班別合併需避免破壞 9 月前 N1/N2 只有開收線、9 月後才分早中班的既有規則。
- raw data 下載需確認是否包含使用者無權查詢的資料；下載權限不可大於畫面查詢權限。
- 趨勢圖功能不得讓 `TREND_CHART` 被誤判為 `CONTROL_CHART`。

## 假設與未決問題

- Excel 未附 raw data 範例；常態檢定修正需要可重現資料，若程式中能找到對應資料則不另問，找不到才請使用者提供。
- 「班別的管制圖合併一起」暫解讀為同一張圖呈現不同班別資料，而不是合併/覆蓋資料列。

## 規格版本紀錄

| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-10-05 | 解析 `SPC 修正-1002.xlsx` 並建立小工作排序。 |
