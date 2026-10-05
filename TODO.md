# SPC 架構改善 TODO

建立日期：2026-10-02
狀態：架構稽核完成，等待使用者逐項確認

## 執行規則

- 每次只執行一個 TASK。
- 開始前先確認該 TASK 的範圍與驗收條件。
- 新發想先進「工作池」並標註系統、風險、相依與驗收；未排入工作池前不插隊實作。
- 排序優先順序：阻擋正式/測試使用的錯誤 > 安全與權限隔離 > 資料正確性與可回復 > 使用者高頻操作 > 文件/文案 > 架構整理。
- 同時碰登入/權限、發布/IIS、資料庫 migration、公式計算或公告權限的小工作不可併行；先做範圍小且可獨立驗證者。
- 修改核心邏輯前先補測試，修改後執行對應測試。
- 完成後更新本檔、相關 specs、`CHANGELOG_CUSTOM.md` 及必要變更紀錄。
- 測試通過後只發布 SPC 測試站；正式站需另行授權。
- 未經確認不提前執行下一個 TASK。

## 目前小工作排序（2026-10-05）

### 第一順位：SPC 修正-1002 Excel 統計正確性與圖表需求

來源：`D:\SPC\docs\SPC 修正-1002.xlsx`；規格：`specs/20261005-spc-1002-workbook-plan/spec.md`。

原則：先處理統計正確性與可驗證資料，再處理呈現增強；疑似已完成項目先驗證，不重複改動。

#### SPC-TEST-20261005-TASK-001：製程總覽下拉與資料數口徑修正

狀態：完成；SPC 測試站 backend/frontend 已發布

來源：`D:\SPC\docs\SPC測試問題_20261005.xlsx`

問題：

- 製程的管制項目總覽「線別」下拉混入非線別項目，如製程、檢驗項目、總數、日期、作業員、lot、樣本編號。
- 總表「匯入資料數」與管制圖明細「管制點數」口徑不同，Xbar 類項目顯示 raw sample 數，與畫面管制點數不一致。

修改：

- 前端線別下拉只納入有 `processId`、`process` 與製程名稱/代碼的有效線別。
- 後端總表 `TotalCount` 僅在製程 `PROC` 改採 chart data 的管制點數；藥液 `CHEM` 維持 raw data 筆數，不影響藥液功能與計算。

驗證：

- 後端 build 0 warnings / 0 errors。
- 前端 `npm run build -- --mode testhost` 通過。
- SPC 測試站 backend/frontend 已發布；備份 `backend.backup-test-issues-20261005-144947`、`frontend.backup-test-issues-20261005-144947`。
- 2026-10-05 補充：依使用者提醒，`TotalCount` 口徑修正已收斂為只套用製程 `PROC`；藥液 `CHEM` 維持原 raw data 筆數。測試站 backend 已重新發布，備份 `backend.backup-proc-total-count-20261005-150540`。
- 2026-10-05 再修正：製程標準代碼為 `PROCESS`，已改用標準化 control scope 判斷，避免製程總覽仍回退 raw data 筆數；藥液 `CHEM` 維持不變。後端 build 通過並已發布測試站 backend，備份 `backend.backup-process-total-count-scope-170030`。
- 2026-10-05 OOS 再確認：製程總覽 `OosCount/OocCount` 同樣需用製圖管制點口徑；已調整為製程 `PROCESS` 使用 `chartData.points` 旗標計算，藥液 `CHEM` 維持 raw data 口徑。後端 build 通過並已發布測試站 backend，備份 `backend.backup-process-oos-scope-193639`。
- Smoke：`/api/version` 回 `environment=test`；前端首頁 HTTP 200；新 JS `index-Hc-aHFuY.js` 回 `application/javascript`，CSS 回 `text/css`。

#### SPC-1002-TASK-001：常態分布檢定 P-value 修正

狀態：完成；SPC 測試站 backend 已發布

理由：Excel 指出 raw data 實際 P-value `<0.05`，系統顯示 `0.0986`，屬核心統計正確性。

預計修改：

- 後端常態性檢定演算法、資料取樣或 P-value 回傳。
- 必要時調整前端直方圖檢定顯示文字；不先擴張到其他圖表功能。

測試：

- 以可重現 raw data 建立後端單元測試。
- 驗證 `<0.05` 案例、目前 `0.0986` 回歸案例、零變異/小樣本邊界。

確認結果：

- 測試站同一組資料 P-value 與 raw data 驗算一致。

完成紀錄：

- 修改：後端常態性檢定由一般 Jarque-Bera 改為 adjusted Jarque-Bera，修正小樣本 P-value 偏高。
- 測試：`NormalityTest` 4 passed；後端 build 0 warnings / 0 errors。
- 發布：SPC 測試站 backend 已發布，備份 `backend.backup-normality-20261005-123245`；正式站未發布。
- 限制：Excel 未附原始 raw data，因此以可重現的小樣本差異案例驗證；使用者仍可後續用真實 raw data 再對帳。

#### SPC-1002-TASK-002：管制圖預設界線關閉現況驗證

狀態：完成；現況驗證通過

理由：`CHANGELOG_CUSTOM.md` 已有 2026-09-25「管制界限預設關閉」，先驗證避免重複改動。

預計修改：

- 原則上不改程式；若測試站仍預設勾選，再另開小型修正。

測試：

- 製程/藥液管制圖初始載入、重新整理、切換圖表後，規格界限與管制界限皆預設未勾選。

確認結果：

- 使用者進入管制圖時界線預設關閉，手動勾選仍可顯示。

完成紀錄：

- 驗證：`frontend/mes-spc-web/src/views/SpcChartView.vue` 中 `showSpecLimits` 與 `showControlLimits` 初始值皆為 `false`，且畫面 checkbox 綁定同兩個狀態。
- 說明：`TrendChartView.vue` 的規格線初始值目前為 `true`，但 Excel 此項為「管制圖」；趨勢圖分布/直方圖需求已另列 `SPC-1002-TASK-006`，本項不混入。
- 本次未修改產品程式、未建置、未發布。

#### SPC-1002-TASK-003：Ca 呈現移除絕對值現況驗證/補修

狀態：完成；現況驗證通過

理由：`ai_docs/10_change_log.md` 已有 2026-09-29「CA 顯示保留正負號」目的，需確認目前程式/測試站是否完成。

預計修改：

- 若尚未完成，只調整 Ca 顯示/格式化或計算輸出；Cpk/Ppk 不跟著改。

測試：

- 平均值低於/高於 target 各一筆，Ca 顯示負值/正值。
- Cpk/Ppk 回歸不受影響。

確認結果：

- Ca 不再被絕對值化，正負號符合 mean 相對 target 的方向。

完成紀錄：

- 現況：後端 `ProcessCapabilityCalculator` 已用 `(mean - target) / halfWidth` 計算 Ca，未再取絕對值。
- 現況：前端 `SpcChartView.vue` 的能力指標列直接顯示 `chartResult.capability.ca`，`formatNumber` 只做小數格式化，會保留負號。
- 測試：`dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter Capability_Ca_ShouldKeepSignAndLeaveCpkPpkUnchanged --no-restore -p:UseSharedCompilation=false` 通過，1 passed。
- 發布：本項未修改功能程式，故不重新發布；沿用目前 SPC 測試站。

#### SPC-1002-TASK-004：班別管制圖合併呈現需求釐清與最小調整

狀態：完成；SPC 測試站 backend/frontend 已發布

理由：會碰時間序列、班別與開收線既有規則，需在統計核心修正後處理。

預計修改：

- 管制圖班別序列呈現、Tooltip、圖例或查詢組合邏輯。
- 不改歷史量測資料、不改班別/開收線資料結構。

測試：

- N1/N2 開收線、早/中班、非 N1/N2 線別各一組。
- 驗證同圖查看不同班別時，點位順序、MR 與異常規則不錯位。

確認結果：

- 使用者可在同一張管制圖查看班別資料，標籤與統計結果正確。

完成紀錄：

- 修改：Xbar-R/Xbar-S 子組點位補帶 `portalDailyDate`、`samplingPhase`、`samplingStage`，讓同一張管制圖合併顯示不同班別時，座標標籤、Tooltip 與點位詳細卡可顯示班別/取樣階段。
- 範圍：未修改資料庫、未改歷史量測資料、未改 `ChemicalStageChart` 排序、未改 MR/Xbar/Cpk/Ppk 統計公式。
- 測試：`XbarR_ShouldKeepChemicalShiftMetadataOnChartPoints` 與 `Capability_Ca_ShouldKeepSignAndLeaveCpkPpkUnchanged` 通過，2 passed。
- 建置：後端 build 0 warnings / 0 errors；前端 `npm run build -- --mode testhost` 通過。
- 發布：SPC 測試站 backend/frontend 已發布；備份 `backend.backup-shift-chart-20261005-130812`、`frontend.backup-shift-chart-20261005-130812`。正式站未發布。
- Smoke：`/api/version` 回 `environment=test`；前端首頁與 `index-x4DbX-TX.js` 資產 HTTP 200。

#### SPC-1002-TASK-005：raw data 下載（製程/藥液）

狀態：完成；SPC 測試站 backend/frontend 已發布

理由：可支援使用者自行驗算常態檢定與後續問題追溯，但屬新增功能。

預計修改：

- 新增或擴充 raw data 匯出 API。
- 管制圖/趨勢圖加入下載入口。
- 權限不得大於既有查詢頁面。

測試：

- 製程與藥液各下載一組，核對日期、線別、槽位、分析項目、班別/階段、原始值。
- 未登入 401、無權限 403。

確認結果：

- 使用者可下載目前查詢條件的 raw data 並用 Excel 驗算。

完成紀錄：

- 修改：新增 `GET /api/v1/spc/chart/raw-data` CSV 下載端點，沿用管制圖 `ppcId`、`uploadBatchId`、`batchNo`、`partId`、`startDate`、`endDate` 查詢條件與 93 天限制。
- 修改：管制圖頁新增「下載 raw data」按鈕；下載內容包含製程/檢驗項目、量測時間、日報日期、班別、取樣階段、原始值、批號、線別/槽位 ID、板面與 OOS/OOC 等欄位。
- 範圍：不改資料庫、不改量測資料、不放大查詢權限；未登入端點回 401。
- 驗證：後端 build 0 warnings / 0 errors；前端 `npm run build -- --mode testhost` 通過。
- 發布：SPC 測試站 backend/frontend 已發布；備份 `backend.backup-raw-data-download-20261005-135341`、`frontend.backup-raw-data-download-20261005-135341`。正式站未發布。
- Smoke：`/api/version` 回 `environment=test`；前端首頁與 `index-BEkeUYvV.js` 資產 HTTP 200；未登入 raw data 端點回 401。
- 待使用者登入確認：使用有資料的製程/藥液管制圖下載 CSV，核對內容是否符合現場欄位期待。

#### SPC-1002-TASK-006：趨勢圖增加直方圖等分布功能

狀態：完成；SPC 測試站 frontend 已發布

理由：屬呈現增強，需等常態檢定與 raw data 對帳可信後再做。

預計修改：

- 趨勢圖新增直方圖/常態分布相關區塊。
- 不畫管制界線、不套用規則管理、不顯示 OOC 規則判定。

測試：

- 趨勢圖仍維持 `TREND_CHART` 行為，不混入 `CONTROL_CHART` 規則。
- 直方圖、常態曲線、P-value 與 raw data 下載結果一致。

確認結果：

- 趨勢圖可看分布與常態檢定，但不出現管制線與規則管理。

完成紀錄：

- 規格：`specs/20261005-trend-histogram/spec.md`。
- 修改：趨勢圖頁新增 raw data 分布直方圖、常態分布曲線、P-value、偏態、峰度與常態判定；直方圖只標示 LSL/USL/Target/Mean，不顯示 UCL/LCL/CL，不套用 OOC 規則管理。
- 驗證：前端 `npm run build -- --mode testhost` 通過。
- 發布：SPC 測試站 frontend 已發布，備份 `frontend.backup-trend-histogram-203651`；首頁 HTTP 200，新版 JS `index-DVVeXuQK.js` 回 `application/javascript`。
- 待使用者登入確認：開啟有資料的趨勢圖項目，確認直方圖、常態曲線與 P-value 顯示。

### 第二順位：Portal 公告權限與公告格式（安全與高頻操作）

#### PORTAL-TASK-001：福利專區文案「輔助辦法」改「補助辦法」

狀態：完成；Portal 測試站已發布

理由：低風險文案變更，可快速完成並避免後續混淆。

預計修改：

- Portal Web 福利專區卡片、標題、相關提示文字。
- 若後端回傳顯示名稱或檔案說明含「輔助辦法」，同步改為「補助辦法」。

測試：

- Portal Web build。
- 開福利專區確認卡片與頁面顯示「補助辦法」。
- 確認 PDF 開啟/下載行為不變。

確認結果：

- 使用者登入 Portal，進福利專區，只看到「補助辦法」，不再看到「輔助辦法」。

完成紀錄：

- 規格：`D:\PmrPortal\specs\20261005-welfare-subsidy-wording\spec.md`。
- 修改：Portal Web 福利專區頁面文字、上傳成功訊息；Portal API welfare-assistance 回應訊息。
- 驗證：Portal API/Web build 通過；測試站 API `/health` 200，福利頁未登入 401。
- 發布：Portal 測試站 API/Web 已發布，備份 `welfare-subsidy-wording-20261005-095524`；正式站未發布。

#### PORTAL-TASK-002：新增人事角色與公告管理角色隔離

狀態：待規格

理由：公告管理屬權限隔離；需先做，避免不同角色互看/誤改公告。

預計修改：

- 新增人事角色（建議 role code：`human_resources`，顯示「人事」）。
- 公告管理入口允許 `admin`、`general_affairs`、`human_resources` 及未來授權公告角色。
- 公告資料需能辨識管理角色/擁有角色；非 admin 只能看、改、刪自己角色管理的公告。
- `admin` 可看全部公告管理。
- API 查詢、新增、修改、刪除、附件上傳/刪除都必須套同一隔離規則。

測試：

- 後端公告 controller 權限測試：admin 全看；總務只看總務公告；人事只看人事公告；一般使用者不可進管理。
- Portal Web build。
- 測試站用不同角色登入確認清單與操作邊界。

確認結果：

- 人事登入公告管理只看到人事公告。
- 總務登入只看到總務公告。
- admin 可看到全部公告。
- 一般使用者仍只能看已發布公告，不能管理。

#### PORTAL-TASK-003：公告發佈格式自動帶組織單位

狀態：待規格；相依 PORTAL-TASK-002

理由：牽涉 AD/Portal 使用者資料與公告內容規則，需在公告角色隔離後做。

預計修改：

- 發公告時依登入者 AD/Portal 使用者資料帶入組織單位，例如「資訊課」。
- 新公告主旨格式：`【組織單位名稱】公告主旨`。
- 公告內容維持使用者輸入，不自動覆蓋既有公告內容。
- 若 AD 無組織單位，回退 Portal 使用者部門；仍無資料時要求使用者確認或顯示「未設定單位」。

測試：

- 後端/前端測試不同帳號部門來源。
- 新增公告後清單與前台顯示標題含 `【資訊課】` 類格式。
- 編輯既有公告不得重複套兩次前綴。

確認結果：

- 使用 ihao_ting 或測試帳號發公告時，主旨自動帶入正確組織單位。

### 第三順位：SPC 藥液公式版本與核對效率（資料正確性）

#### SPC-TASK-001：藥液分析公式版本記錄與回復

狀態：待規格

理由：公式會影響計算結果，需先建立版本、稽核與回復，才適合做大量編輯頁。

預計修改：

- 釐清既有 `ChemicalAnalysisConfigJson`、F 表版本與公式來源。
- 新增或利用既有歷史表保存公式版本、啟用日期、修改人、修改原因與前後差異。
- 提供回復上一版或指定版本能力。
- 保留既有計算結果可追溯，不覆蓋歷史紀錄。

測試：

- 後端公式版本新增/修改/回復測試。
- 確認修改公式後新查詢使用新版，回復後使用舊版。
- 權限測試：只有授權角色可改公式。

確認結果：

- 在測試站修改一筆藥液公式，可看到版本紀錄，並可回復前一版。

#### SPC-TASK-002：藥液公式總覽與批次儲存頁

狀態：待規格；相依 SPC-TASK-001

理由：一次儲存多筆公式風險較高，須先有版本/回復保護。

預計修改：

- 新增頁面顯示所有線別、槽位、分析項目的藥液分析公式。
- 支援篩選、搜尋、差異標示、批次編輯與一次儲存。
- 儲存前顯示變更摘要；儲存時逐筆建立版本紀錄。
- 不影響現有單筆管制項目編輯入口。

測試：

- 前端表格/批次編輯測試。
- 後端批次儲存交易測試。
- 一次儲存多筆後，每筆皆有版本紀錄，可逐筆回復。

確認結果：

- 使用者可在一頁核對所有公式，修改多筆後一次儲存；錯誤時可回復。

### 第四順位：單一 IIS Site（環境與發布）

#### IIS-TASK-003～IIS-TASK-008：沿用 `specs/20261005-single-iis-site/tasks.md`

狀態：已完成 TASK-001/TASK-002；TASK-003 起待執行

理由：會碰發布/IIS/HTTPS，與 Portal/SPC 新功能開發不併行。公告與公式規格定案後，再安排測試站切換。

### 暫停併行：架構改善 TODO

`TASK-002`～`TASK-010` 架構改善暫不插隊；若與本批 Portal/SPC 工作碰到相同區域，先以本批業務需求的小範圍修改優先。

## TASK-001：設定與密鑰安全

狀態：已完成，待使用者確認

範圍：

- 清查 `appsettings.example.json`、測試／正式設定與 Git 歷史中的 JWT、SMTP、資料庫連線資訊。
- 輪替已暴露或疑似暴露的密鑰。
- 改用環境變數、受控秘密儲存或部署注入，不在版本庫保存可用密鑰。
- SMTP 設定 API 不回傳密碼，設定更新採安全且可回復方式。

已完成：SMTP GET 不回傳密碼；留白更新保留既有密碼；範例設定改為環境注入 placeholder；新增 2 個回歸測試。

驗證：後端 2 tests passed；前端 production build passed；範例 JSON 解析成功。

待運維：尚未自動輪替現有測試站／正式站實際密鑰，需另依發布環境注入並驗證。

## TASK-002：MES Sync 資料可靠性

狀態：待確認

範圍：

- 移除尚未實作卻將訊息標記為 `Processed` 的流程。
- 定義未知 MessageType、解析失敗、重試、死信與冪等鍵行為。
- 補上資料寫入交易與背景服務測試。

驗收：未支援或失敗訊息不會被誤標成功；可重試且不重複寫入；服務重啟後狀態一致。

## TASK-003：診斷端點與錯誤資訊隔離

狀態：待確認

範圍：

- 移除或限制 `SettingsController` 的任意檔案 `inspect-excel` 讀取能力。
- 確認 OpenAPI、Scalar、健康檢查及診斷端點的匿名／登入／Editor 邊界。
- production 錯誤回應不暴露 SQL、檔案路徑或 inner exception；詳細資訊只寫安全日誌。

驗收：Viewer 無法讀取任意伺服器檔案；未授權端點符合明確清單；錯誤回應不含內部細節。

## TASK-004：後端授權模型收斂

狀態：待確認

範圍：

- 將前端 permission 與後端 authorization policy 對齊。
- 逐一檢查主檔、匯入、設定、通知、校正、Migration 等 controller。
- 不再只依 HTTP method 判斷 Viewer 是否可執行業務操作。

驗收：Viewer、Editor、指定 permission 的 API 行為與 UI 一致；直接呼叫 API 也無法繞過權限。

## TASK-005：資料庫 migration 與啟動流程

狀態：待確認

範圍：

- 將 `Program.cs` 內手寫 schema 修補與資料初始化分離。
- 收斂為可追蹤 EF migration／受控 deployment job。
- 啟動時不吞掉 migration 或 schema 失敗；建立回復與維護模式策略。

驗收：全新資料庫、既有測試資料庫可重現升級；失敗會阻止錯誤版本啟動；migration history 與 schema 一致。

## TASK-006：發布流程與環境隔離

狀態：待確認

範圍：

- 將 test／production build 與 publish 指令分離。
- 正式發布必須明確指定環境、目標、版本與核准，不因測試發布連帶處理正式目錄。
- 發布前備份、health check、版本核對、回復及 app_offline 流程標準化。

驗收：測試發布不會寫入正式輸出；錯誤發布可回復；release manifest 可對應 commit、API、Web 與資料庫。

## TASK-007：SMTP、檔案與背景排程可靠性

狀態：待確認

範圍：

- SMTP 密碼遮罩／安全保存與原子設定更新。
- 報表排程加入分散式鎖、outbox 或等價防重送機制。
- 檔案附件、Email、Chat 發送失敗可追蹤、重試且不重複副作用。

驗收：多 instance 不重複寄送；設定更新中斷不破壞原設定；通知狀態可追蹤與重試。

## TASK-008：稽核與操作者追溯

狀態：待確認

範圍：

- `CreatedBy`／`UpdatedBy` 不再固定為 `System`。
- 統一從已驗證 claims 取得操作者，補足背景工作、Portal SSO、工具匯入的 actor 規則。
- 重要主檔、設定、匯入、刪除與通知操作保留可查稽核資料。

驗收：UI、API、背景工作及匯入流程的操作者可正確追溯；未驗證 actor 不可冒用。

## TASK-009：測試與可重建基線

狀態：待確認

範圍：

- 整理多組 .NET 測試專案、E2E、前端測試與缺少 fixture 的歷史測試。
- 建立單一可重現的 build／test 入口與測試分類。
- 盤點未追蹤 migration、功能檔案、發布產物及工作樹差異，建立可重建 Git 基線。

驗收：乾淨工作樹可重建 API、Web、測試與測試站包；測試報告能區分通過、失敗、跳過與環境限制。

## TASK-010：大型模組拆分與架構治理

狀態：待確認

範圍：

- 在安全、資料可靠性與發布基線穩定後，拆分 `UploadService`、`SpcService`、大型 controller 與大型 Vue view。
- 建立 application service、domain calculation、持久化、API DTO 與前端 feature boundary。
- 保持 API 契約、資料庫相容性與既有業務行為。

驗收：模組依責任可獨立測試；核心服務大小與依賴降低；完整回歸及效能基準通過。

## 本輪紀錄

- 已完成：閱讀根目錄、backend、frontend、tests、scripts、需求索引與專案設定。
- 已完成：建立 TASK-001～TASK-010 清單。
- 本輪未修改程式、資料庫或發布內容。
- 本輪測試：不適用；僅新增規劃文件，未執行程式修改。
- 下一步：等待使用者確認後，僅執行 TASK-001。
