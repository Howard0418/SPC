## [2026-10-21] SEC-FOLLOWUP-006 回歸基準與fixture修正
- Pkcs修改前後相同6項失敗；僅修正符合已確認WE/SSO/結構化錯誤基準的過時測試與缺失主檔，不改產品邏輯或放寬授權。

## [2026-10-21] SEC-FOLLOWUP-006 Pkcs相依安全修補
- 目的：直接鎖定Pkcs10.0.13移除SqlClient傳遞9.0.4警告；只改參照與API版號0.1.68，無計算/資料庫/權限變更。
- 驗證：API/化學回歸、Release build/publish與deps核對，測試站受控發布；正式維護仍依18:00與前置規則。

## [2026-10-14] - SPC-ETCH-PREVIEW-500-DIAGNOSTICS 咬蝕量預覽 500 診斷修正
- 目的：TransFiles 咬蝕量預覽遇 SPC 拒絕時須顯示可修正原因，避免只看到 HTTP 500。
- 範圍：SPC `EtchReportSyncService` 主檔與既有批次缺漏錯誤處理、咬蝕測試；不修改計算公式、不匯入正式資料、不改 TransFiles 解析。
- 驗證：EtchTests、backend build；通過後刷新 SPC 測試站 backend。

## [2026-10-14] - SPC-CHEM-N1N2-CLOSE-LABEL N1/N2 CLOSE 顯示修正
- 目的：修正 SPC 管制圖中 N1/N2 藥液 `CLOSE` 被舊班別顯示邏輯標成「晚班」的問題，讓開收線情境顯示為「早班收線」。
- 範圍：前端 `chemicalChart` 標籤工具、`SpcChartView` tooltip 與點位明細、對應前端測試；不修改後端資料、資料庫、TransFiles 輸出或正式資料。
- 驗證：先補前端回歸測試，再執行相關 node test 與 frontend build；通過後刷新 SPC 測試站 frontend。
- 追加：依使用者要求將 N1/N2 藥液開收線標籤縮短為 `早(開)`、`早(收)`、`中(開)`、`中(收)`，同步 X 軸、tooltip 與點位明細。

## [2026-10-14] - SPC-IMPORT-VIA-TRANSFILES-ONLY 全部小工作
- 目的：完成 SPC 改由 TransFiles 直接匯入的支援工作，SPC 網站不再引導 Excel 上傳，改為批次查詢、錯誤追蹤、新資料提示與製圖確認。
- 範圍：直接匯入契約文件、Uploads 查詢 API、匯入批次查詢頁、首頁新資料提示、驗證與操作說明。不新增資料表、不匯入資料、不操作正式環境。
- 驗證：新增/更新 UploadService 測試，執行相關後端測試、後端 build 與前端 build；通過後刷新 SPC 測試站 backend/frontend。

## [2026-10-14] - SPC-CHEM-OCT-IMPORT-TASK-001 10 月藥液匯入預檢報告
- 目的：讓藥液 Excel 匯入預覽可清楚顯示檔案、工作表、Excel 列號、錯誤欄位、錯誤代碼與修正建議，協助定位 10 月資料無法匯入原因。
- 範圍：後端上傳預覽 metadata 與錯誤 DTO、前端匯入預覽表格；不新增資料表、不確認匯入、不修改正式資料。
- 驗證：先補 UploadService 預覽錯誤報告測試，再執行相關測試、後端 build 與前端 build；通過後發布 SPC 測試站。

## [2026-10-14] - SPC-CONTROL-LINE-FORMULA-LABEL 管制線計算公式文案釐清
- 目的：將 SPC 管制項目進階設定中的「公式配置」改為「管制線計算公式」，避免與常態檢定方法混淆。
- 範圍：前端 `PartProcessCharacteristicsView.vue` 文案與說明；不修改 API、資料庫、JSON 契約、常態檢定或管制線計算邏輯。
- 驗證：前端 build；通過後刷新 SPC 測試站 frontend。

## [2026-10-14] - SPC-PPC-EDIT-UI-SIMPLIFICATION 管制項目編輯畫面直覺化
- 目的：整理 SPC 管制項目新增／編輯抽屜，移除快速／進階模式造成的重複欄位，讓基本資料、規格、圖表、藥液公式與進階 JSON 分區操作。
- 範圍：前端 `PartProcessCharacteristicsView.vue` 版面與文案；統一「槽位」顯示。不修改 API、資料庫、計算、權限或儲存 payload。
- 驗證：前端 build；通過後刷新 SPC 測試站 frontend。

## [2026-10-14] - SPC-NORMALITY-METHOD-SWITCH 常態檢定方法切換
- 目的：讓使用者可在 SPC 管制圖與量測趨勢圖切換 Adjusted Jarque-Bera 與 Anderson-Darling 常態檢定，比對 p-value 與判定。
- 範圍：後端 `/api/v1/spc/chart` 新增 `normalityMethod` 參數與 Anderson-Darling 計算；前端新增常態檢定方法選項。不修改資料庫、不改 raw data、管制界線或能力指標。
- 驗證：先補 NormalityTest，再實作並執行後端測試/build 與前端 build。

## [2026-10-13] - SPC-HISTOGRAM-1012-TASK-001 直方圖 scale/bin 改善
- 目的：改善小變異資料的直方圖 X 軸與 bins，避免遠端規格/管制界線壓縮主要量測分布。
- 範圍：前端管制圖與趨勢圖直方圖共用 bins 工具；後端 normal curve 縮放改用量測值範圍估算 bin width。不改 SPC 檢定方法、不改 raw data 匯出欄位。
- 驗證：前端 histogram 測試 2 passed；NormalityTest 6 passed；後端 build 通過；前端 build 通過。

## [2026-10-13] - SPC-NORMALITY-1012-TASK-001 常態檢定方法差異揭露
- 目的：確認 SPC 與 Minitab 常態檢定差異來源，避免不同檢定方法的 p-value 被直接比較。
- 範圍：後端 `NormalityTestResult` 增加方法說明，管制圖與趨勢圖顯示 SPC 採 Adjusted Jarque-Bera、Minitab 常用 Anderson-Darling 的差異；不替換統計方法。
- 驗證：NormalityTest 5 passed；後端 build 通過；前端 build 通過；已發布 SPC 測試站 backend/frontend 並完成 HTTPS smoke。

## [2026-10-13] - SPC-ETCH-X-TASK-002 咬蝕 X- 線別排除實作
- 目的：避免 TransFiles 固定區塊解析時將 `X-PT-01`、`X-QE-02` 等不匯入 SPC 的線別誤產生為 SPC reports。
- 範圍：TransFiles `etch_reports.py` 讀取第 2 列原始線別標題，`X-` 前綴線別加入 `excluded` 並不產生 `reports`；JSON/Excel 輸出保留排除摘要。不修改 Portal/SPC API、不打包 EXE。
- 驗證：TransFiles 咬蝕相關 unittest 11 passed；py_compile 通過；真實來源唯讀抽取 reports 114、excluded 8。

## [2026-10-13] - SPC-ETCH-X-TASK-001 咬蝕 X- 線別規格與測試案例
- 目的：將使用者補齊的 `X-` 線別不匯入 SPC 規則落成可實作契約，避免後續把 `X-PT-01` 去掉前綴後誤匯入。
- 範圍：更新咬蝕 X- 規格、跨 TransFiles/Portal/SPC 契約、測試案例矩陣與回復方式；不修改程式、不匯入資料、不發布。
- 驗證：文件內容檢查完成；建置、測試與發布不適用。

## [2026-10-13] - SPC-RAWDATA-1012-TASK-003 Raw Data 規格資訊補欄
- 目的：讓 raw data CSV 下載內容可直接辨識對應規格資訊，補上 LSL/Target/USL/Unit，避免使用者下載後無法判讀量測值與規格關係。
- 範圍：後端 `RawDataCsvExporter` 欄位與測試；不調整 raw data 查詢條件、不修改前端、不處理常態檢定或直方圖。
- 驗證：RawDataCsvExporter 測試 3 passed；後端 build 通過；已發布 SPC 測試站 backend 並完成 HTTPS smoke。

## [2026-10-13] - SPC-RAWDATA-1012-TASK-002 Raw Data Value 日期誤判與項目資料修正
- 目的：修正 raw data CSV `Value` 欄與日期欄共用格式化造成數值被誤判為日期的問題，並避免非目標 PPC 資料混入下載結果。
- 範圍：後端 CSV 組裝抽出 `RawDataCsvExporter`；日期欄位才格式化，`Value` 保留原始 invariant 字串；補 RawDataCsvExporter 單元測試。不補規格資訊欄位。
- 驗證：RawDataCsvExporter 測試 2 passed；後端 build 通過；已發布 SPC 測試站 backend 並完成 HTTPS smoke。

## [2026-10-12] - SPC-RAWDATA-1012-TASK-001 All 總表後 Raw Data 下載上下文修正
- 目的：修正 SPC 使用者先查詢 All 總表後，選定單一管制項目再下載 raw data 時，前端未穩定解析 active ppcId 而提示需先查詢管制圖的問題。
- 範圍：前端 `SpcChartView` raw data 下載上下文解析與對應單元測試；不調整 CSV 欄位內容、不修正 Value 日期誤判、不補規格資訊，後兩者留待後續 Task。
- 驗證：預計新增 node 單元測試保護 `ppcId`、總表列 `partProcessCharacteristicId`、舊欄位 `ppcId` 與 mapping fallback。

## [2026-10-12] - IIS-TASK-007 Portal SSO 完成
- 目的：完成單一 IIS Site T-007 真人登入、Portal SSO、JWT 與 HTTPS 單站回跳驗證。
- 範圍：Portal API 測試站 `SpcSso:WebBaseUrl` 改為 `https://172.16.110.27:8901` 並重啟 AppPool；SPC 測試前端以 `VITE_APP_ENV=test` 重建刷新。不操作正式站。
- 驗證：Portal 登入後回跳 `https://172.16.110.27:8901/portal-sso`，SPC 取得 JWT，使用者為 Editor；API smoke 皆 200，Network 無 `http://`，環境不一致警示消失。

## [2026-10-12] - IIS-TASK-007 JWT/403 補驗
- 目的：續補單一 IIS Site T-007 中可自動驗證的 JWT、401 與 Viewer 403，縮小剩餘外部阻擋範圍。
- 範圍：使用測試站設定產生短效測試 JWT 進行 HTTPS API smoke；僅更新文件，不修改程式、不操作 IIS、不發布正式站。
- 驗證：Portal 測試登入頁可載入但 `demo/demo123` 不可用；未登入寫入 API 401，Viewer JWT 寫入 API 403，Editor JWT 進入 controller validation 回 400；Portal directory 比對服務另回 502/Portal 401。
- 限制：真人登入與 Portal SSO 仍需可用測試帳號。

## [2026-10-12] - IIS-TASK-006 HTTPS binding 補驗
- 目的：補上 SPC 單一測試站 HTTPS binding/cert 與 mixed content 驗證，使用者提供 Portal HTTPS 入口 `https://172.16.110.27:8900/Login`。
- 範圍：新增 `SpcSingleTest` 的 `https/172.16.110.27:8901:` binding，沿用既有測試憑證；production `VITE_PORTAL_URL` 改為 HTTPS Portal URL，同步 `.env.example`，重建並刷新 `release/test/frontend`。不操作正式站。
- 驗證：IIS 與 frontend 皆已備份；Playwright/Chromium HTTPS 首頁/子路由/API version smoke 通過，Network 無 `http://` mixed content；`npm run build` 通過。
- 限制：真人登入、Portal SSO、JWT 與 Viewer 403 仍待測試帳號。

## [2026-10-12] - IIS-TASK-007 HTTP smoke 補驗
- 目的：補驗單一 IIS Site 在現有 HTTP 測試單站可完成的 smoke，並移除測試站 active bundle 殘留的舊 `8081` API base 風險。
- 範圍：重建 SPC Web production 前端並刷新 `release/test/frontend`；同步單一 IIS Site 規格、驗證紀錄、需求索引、TODO 與變更紀錄。不修改後端程式、不操作正式站。
- 驗證：`npm run build` 通過；資產檢查無 `172.16.110.27:8081/api`、`localhost:` 或 `/api/api`；`8084/api/version` 200/test，首頁與三個 Vue 子路由 200，active JS 200 `text/javascript`，未登入受保護 API 401。
- 限制：HTTPS mixed content、真人登入、Portal SSO、JWT 與 Viewer 403 仍待外部條件。

## [2026-10-12] - Portal 測試生日祝賀按鍵完成
- 目的：同步 `PORTAL-BIRTHDAY-REPLAN-TASK-004` 完成狀態，讓 SPC 工作池反映 Portal 已新增人事生日祝賀即時預覽。
- 範圍：更新 SPC 工作池、需求索引與客製變更紀錄；Portal 實作位於 `D:\PmrPortal\specs\20261012-birthday-preview-button\`。
- 驗證：Portal Web build 通過；Portal 測試站 Web 已發布，正式站未發布。
- 補修：圖片上傳與預覽顯示已於 PmrPortal 修正；支援常見 Content-Type 變體，並將 `/uploads/...` 轉為 Portal API 完整圖片網址。API 測試 7 passed，Portal 測試站 API/Web 已發布。
- 版型：生日賀卡已依參考圖改為購物網站廣告式置中彈窗；Portal Web build 通過，測試站 Web 已發布。
- 微調：生日賀卡背景透明、右上角浮動關閉按鍵、圖片置中放大；Portal Web build 通過，測試站 Web 已發布。
- 回饋修正：已移除生日賀卡透明外框與圖片陰影；Portal Web build 通過，測試站 Web 已發布。
- 儲存修正：PmrPortal 已將 `feature_settings.message` 放寬為 `nvarchar(max)` 並補齊 EF migration metadata，避免 240 字祝詞 JSON 超過舊 500 字限制造成儲存 500；生日設定 API 測試 9 passed，Portal API build 通過，測試站 API 已發布，測試 DB 已確認欄位與 migration history。
- 顯示模式：PmrPortal 生日通知設定新增 `DisplayMode`，可選只顯示圖片或顯示賀詞 + 圖片；設定頁預覽與首頁生日通知共用設定。生日設定 API 測試 10 passed，Portal API/Web build 通過，測試站 API/Web 已發布。

## [2026-10-12] - Portal 測試生日祝賀按鍵規劃
- 目的：依使用者要求先規劃 Portal 生日通知設定頁的測試生日祝賀按鍵，方便人事不需指定時間即可確認生日祝賀版面與顯示效果。
- 範圍：新增規格並將 `PORTAL-BIRTHDAY-REPLAN-TASK-004` 加入 `TODO.md`；不修改 Portal 程式、不建置、不發布。
- 驗證：文件內容檢查完成；工作池已加入新小工作並標示排序、影響與不立即實作。

## [2026-10-12] - ARCH-TASK-010 大型模組拆分與架構治理
- 目的：建立大型模組盤點與拆分治理基線，避免直接拆分 `SpcController`、`UploadService`、`SpcService` 等核心檔案時缺少固定門檻、順序與驗證要求。
- 範圍：新增大型模組盤點腳本與架構治理文件；不修改應用程式、不拆核心類別、不發布。
- 驗證：大型模組盤點腳本門檻 700 可抓出 11 個超標模組並回傳非 0；門檻 10000 回傳 0；治理文件關鍵字檢查通過。工具與文件型小工作，發布不適用。

## [2026-10-12] - ARCH-TASK-009 測試與可重建基線
- 目的：新增可重跑的 SPC 基線驗證入口，將後端測試、後端 build、前端 build 與可選 smoke 依固定順序執行，降低平行執行造成 DLL 鎖或驗證口徑不一致的風險。
- 範圍：新增 `tools/testing/Invoke-SpcBaseline.ps1` 與規格/驗證文件；不修改應用程式、不發布、不清資料。
- 驗證：基線腳本後端測試/build 模式通過；build+smoke 模式通過。工具與文件型小工作，發布不適用。

## [2026-10-12] - ARCH-TASK-008 稽核與操作者追溯
- 目的：先建立共用操作者解析規則，降低各 controller 對 `Identity.Name`、username claims 與 fallback 解析不一致造成的稽核追溯風險。
- 範圍：新增 `AuditActorResolver`，調整 SPC 點位備註/排除 `UpdatedBy` 使用共用解析；不新增 schema、不修改登入/JWT 發行、不改校正 audit log。
- 驗證：`UserAccessTests|SpcPointRemarksControllerTests` 15 passed；後端 build 0 errors；已發布 SPC 測試站 backend，`/api/health`、`/api/version`、首頁 smoke 皆 200。

## [2026-10-12] - ARCH-TASK-007 SMTP、檔案與背景排程可靠性
- 目的：修正 SPC 報表背景排程未檢查 SMTP 寄送結果即更新 LastSent 的可靠性風險，避免全部寄送失敗後同週/月不再重試。
- 範圍：先以測試保護週報/月報排程處理，再抽出可測 processor；不新增 schema、不修改 SMTP 設定 UI、不操作正式站。
- 驗證：`SpcReportScheduleProcessorTests` 3 passed；後端 build 0 warnings / 0 errors；已發布 SPC 測試站 backend，`/api/health`、`/api/version`、首頁 smoke 皆 200。

## [2026-10-12] - ARCH-TASK-006 發布流程與環境隔離
- 目的：將 release 工具中正式環境與 Portal 路徑阻擋規則抽為共用 guard，避免 apply 與 backup/restore 工具安全邊界不一致。
- 範圍：新增 `ReleaseTargetGuard.psm1` 與自動化測試，兩支 release 工具改用共用 guard；不操作正式站、不發布、不刪除資料。
- 驗證：`ReleaseTargetGuard.Tests.ps1` 通過；兩支 release 工具的 Production/Portal 拒絕與 Sandbox WhatIf 載入驗證通過。

## [2026-10-11] - ARCH-TASK-005 資料庫 migration 與啟動流程
- 目的：先將資料庫啟動流程的 provider 正規化與 EnsureCreated/Migrate 決策抽離成可測試規則，降低日後整理 migration 與相容性 SQL 時的誤判風險。
- 範圍：新增 `DatabaseStartupPlanner` 與測試，`Program.cs` 改用啟動計畫；不新增 migration、不改相容性 SQL、不操作正式資料庫。
- 驗證：`DatabaseStartupPlannerTests` 8 passed；後端 build 0 warnings / 0 errors；已發布 SPC 測試站 backend，`/api/health`、`/api/version` 與首頁 smoke 通過。

## [2026-10-11] - ARCH-TASK-004 後端授權模型收斂
- 目的：收斂校正管理相關後端授權判斷，避免 controller 各自解析角色、permissions 與 Operators 回查，降低權限判斷不一致風險。
- 範圍：新增共用授權存取服務與測試，調整校正儀器與匯入 controller 呼叫共用服務；不修改登入流程、JWT 格式或資料庫 schema。
- 驗證：授權相關測試 11 passed；後端 build 0 warnings / 0 errors；已發布 SPC 測試站 backend，`/api/health`、`/api/version` 與首頁 smoke 通過。

## [2026-10-11] - COMPLETION-REMAINING-WORK-REMINDER 完成後剩餘小工作提醒規則
- 目的：依使用者要求，讓每個小工作完成後的最終回覆固定提醒 `TODO.md` 尚有多少未完成小工作。
- 範圍：更新 `AGENTS.md` 小工作完成規則，新增本次文件型規格並同步需求索引、TODO 與 CHANGELOG；不修改程式、不發布。
- 驗證：文件內容檢查完成。

## [2026-10-10] - ARCH-TASK-002 MES Sync 資料可靠性
- 目的：避免 `MesSyncProcessorService` 在尚未實作 MessageType 處理或 Payload 無效時仍將 MES 訊息標示為 `Processed`，造成資料漏同步卻難以追查。
- 範圍：新增可單測的 MES Sync 批次處理器與 handler 介面，背景服務改呼叫批次處理器；不新增 migration、不改 MES/Portal/TransFiles。
- 驗證：`MesSyncMessageBatchProcessorTests` 3 passed；後端 build 0 warnings / 0 errors；已發布 SPC 測試站 backend，`/api/health`、`/api/version` 與首頁 smoke 通過。

## [2026-10-10] - ARCH-TASK-003 診斷端點與錯誤資訊隔離
- 目的：收斂 SPC API 未處理例外與診斷端點，避免 Production/Test 對外回傳 exception message、inner exception 或 SQL 細節，同時提供最小 `/api/health`。
- 範圍：新增錯誤回應工廠與測試、調整 `Program.cs` 全域 exception handler、加入匿名 health endpoint；不改資料庫 schema、不做正式站發布。
- 驗證：`ApiErrorResponseFactoryTests` 3 passed；後端 build 0 warnings / 0 errors；已發布 SPC 測試站 backend，`/api/health`、`/api/version` 與首頁 smoke 通過。

## [2026-10-10] - TODO-COMPLETED-ITEMS-CLEANUP-20261010 已完成項目整理
- 目的：讓 `TODO.md` 未完成排序區只保留未完成、待執行、待確認或外部阻擋項目。
- 範圍：新增 `specs/20261010-todo-completed-items-cleanup/spec.md`，整理 `TODO.md`，同步 `docs/requirements.md` 與 `CHANGELOG_CUSTOM.md`；不修改程式、不發布。
- 驗證：文件內容與關鍵字檢查完成；未修改程式、未發布。

## [2026-10-10] - SPC-CHEM-OVERVIEW-VERSION-TASK-001 HTTP smoke 補驗
- 目的：補驗線別分析項目總覽公式版本參照文件功能的 SPC 測試站 HTTP smoke，確認先前連線拒絕已恢復。
- 範圍：只更新 `specs/20261007-chemical-overview-formula-versioning/`、`TODO.md`、`docs/requirements.md` 與 `CHANGELOG_CUSTOM.md`；不修改程式、不發布。
- 驗證：`8084/api/version`、`8084/`、既有 `8081/api/version`、`8083/` 均 200；單站前端 JS 包含 `chemicalAnalysisFormulaVersionReason` 且不含 `/api/api`。

## [2026-10-10] - IIS-TASK-008 單一 IIS Site 文件同步
- 目的：同步單一 IIS Site 目前驗證狀態，明確區分 HTTP 測試單站已完成項目、HTTPS binding/cert 外部阻擋項目與登入/SSO/JWT smoke 待測條件。
- 範圍：更新 `specs/20261005-single-iis-site/`、`TODO.md`、`docs/requirements.md` 與 `CHANGELOG_CUSTOM.md`；不操作 IIS、不發布、不修改程式。
- 驗證：文件內容、連結與阻擋狀態檢查完成；T-006/T-007 外部阻擋與待測項仍明確保留，未操作 IIS、未發布。

## [2026-10-10] - RELEASE-TOOL-TASK-005 正式機使用手冊與授權檢核
- 目的：完成正式機更新工具的操作手冊、參數範本、前置檢查、回復步驟、證據格式與核准清單，作為未來正式站授權執行前的文件基準。
- 範圍：新增 RELEASE-TOOL-TASK-005 規格與正式機更新手冊，並同步 TODO、需求索引與客製變更紀錄；本階段不操作正式機、不連線正式 IIS、不發布測試站或正式站。
- 驗證：文件內容、連結與安全邊界檢查完成；未連線正式機、未操作 IIS、未發布測試站或正式站。

## [2026-10-09] - SPC-CHART-POINT-REMARK-TASK-001 管制圖量測點備註
- 目的：讓 SPC 管制圖與趨勢圖的可定位量測點可新增、編輯與清空備註，且備註不影響 SPC 計算、OOC/OOS/Cpk 或既有點位排除狀態。
- 範圍：新增 `SpcPointRemark`、`SpcPointRemarks` EF mapping/migration、`point-remarks` API、管制圖/趨勢圖右鍵備註 UI 與 controller 測試；清空備註採軟停用，不實體刪除資料列。
- 驗證：測試資料使用測試名字與測試備註；`SpcPointRemarksControllerTests|SpcPointExclusionsControllerTests` 8 passed；後端 build 0 warnings / 0 errors；前端 `npm run build` 通過，僅保留既有 chunk size warning。已發布 SPC 測試站 backend/frontend 檔案；HTTP smoke：`8084/api/version` 200/test、首頁 200、新前端 JS 200、未登入 `point-remarks` 401。

## [2026-10-09] - SPC-CHEM-OVERVIEW-VERSION-TASK-001 總覽頁公式版本參照文件部分實作
- 目的：讓線別分析項目總覽修改藥液公式時，將參照文件/修改依據寫入既有 `ChemicalAnalysisFormulaVersions.Reason`，不建立第二套版本邏輯。
- 範圍：`ChemicalAnalysisOverviewView.vue`、`PartProcessCharacteristic` 請求欄位、PPC 更新流程與公式版本測試；不新增資料表、不改公式 JSON 格式、不修改 F 表版本。
- 驗證：靜態檢查確認 `chemicalAnalysisFormulaVersionReason` 由前端送出並由後端傳給版本服務；測試資料使用測試名字。後端針對性測試 11 passed；後端 build 0 warnings / 0 errors；前端 `npm run build` 通過，僅保留既有 chunk size warning。已發布 SPC 測試站 backend/frontend 檔案；HTTP smoke 因 `8084`、既有 `8081/8083` 皆連線被拒未通過，判定為測試 IIS/網路層阻擋。

## [2026-10-07] - SPC-FTABLE-VERSION-TASK-001 F 表版本記錄與回復
- 目的：讓 F 表套用新版或回復舊版時保留可查詢、可追溯、可回復的版本歷程。
- 範圍：新增 F 表歷程 entity/migration、service 記錄與回復流程、API、F 表維護頁版本清單與回復操作、service 測試；不修改藥液公式版本與其他 SPC 計算邏輯。
- 驗證：`ChemicalFTableServiceTests` 3/3 通過，後端 build 與前端 production build 通過；已發布 SPC 測試站 backend/frontend，未登入 F 表版本 API 回 401。

## [2026-10-07] - SPC-CL-RECALC-TASK-001 標準差方法切換清除舊管制線
- 目的：修正 Xbar 計算方法在「樣本標準差」與「系統標準公式」切換後，舊固定 UCL/CL/LCL 繼續覆蓋新統計界線的問題。
- 範圍：後端 `PartProcessCharacteristicsController.Update` 與主檔維護測試；不修改 DB schema、不重算既有正式資料。
- 驗證：新增單測通過、後端 build 通過；已發布 SPC 測試站 backend，`8084/api/version` 回 200/test，未登入寫入 API 回 401。

## [2026-10-07] - SPC F 表版本記錄與管制界線重算規劃
- 目的：依使用者需求，將 F 表版本記錄/回復與標準差方法切換後管制界線重算問題加入小工作池。
- 範圍：新增 `specs/20261007-spc-ftable-control-limit-replan/`，更新 `TODO.md`、`docs/requirements.md` 與 `CHANGELOG_CUSTOM.md`；不修改 SPC 程式、資料庫或 IIS。
- 排序：先處理 `SPC-CL-RECALC-TASK-001`，再處理 `SPC-FTABLE-VERSION-TASK-001`。

## [2026-10-07] - Portal 生日通知調整與團保取消規劃
- 目的：依使用者新需求，取消團保專區後續小工作，將生日資料維護改至權限管理出生年月日欄位，並規劃人事專區生日祝詞與祝賀圖片設定頁。
- 範圍：新增 `specs/20261007-portal-birthday-replan/`，更新 `TODO.md`、`docs/requirements.md` 與 `CHANGELOG_CUSTOM.md`；不修改 Portal 程式、資料庫、IIS 或團保資料夾。
- 驗證：工作池新增三個 Portal 生日調整小工作，團保專區標示取消且不列入未完成開發。

## [2026-10-07] - IIS-TASK-006 測試單一 IIS Site 部分完成
- 目的：在不影響既有 `SpcApi:8081`、`SpcWeb:8083` 的前提下，建立 SPC 單一 IIS 測試站並驗證 Vue 與 `/api` 同站服務。
- 範圍：新增 IIS Site/AppPool `SpcSingleTest`，binding `http/172.16.110.27:8084:`，PhysicalPath `D:\SPC\release\test\backend`；將 `release/test/frontend` 更新為 production build 使用相對 `/api`。
- 驗證：`http://172.16.110.27:8084/api/version` 200/test，首頁 200，`/spc`、`/calibration-instruments`、`/particle-monitoring` 200，新 JS 200；前端資產無舊 API base 與 `/api/api`。
- 限制：尚未有 HTTPS binding/cert，AC-007 mixed content 未能驗證；T-006 不標示 DONE。
- 2026-10-09 補充：使用者確認先將 HTTPS binding/cert 視為外部環境阻擋；不操作正式站、不建立未授權憑證，後續先推進可在現有 HTTP 單站或既有測試環境完成的 smoke。
- 2026-10-09 T-007 部分驗證：測試站前端 assets 離線檢查無固定 SPC API IP、localhost 或 `/api/api`；本輪未使用真人帳密或真實姓名。HTTP 實站請求因本機 helper 建立程序失敗未重跑，HTTPS mixed content 隨 T-006 阻擋。

## [2026-10-07] - IIS-TASK-005 單一 IIS 打包驗證
- 目的：確認前端 production build、後端 Release publish 與 sibling frontend static file fallback 可組成單一 IIS Site 發布包；不切換實際 IIS、不修改業務功能。
- 範圍：更新 `specs/20261005-single-iis-site/` 驗證紀錄與任務狀態；staging package 位於 `release-staging/single-iis-t005/`，不納入提交。
- 驗證：`dotnet build --no-restore`、`npm run build`、`dotnet publish -c Release --no-restore` 通過；本機 staging `/api/version`、`/`、`/spc`、`/calibration-instruments`、`/particle-monitoring`、新版 JS 資產均 200。
- 限制：實際測試站 IIS 單站設定、登入/JWT/401/403/mixed content smoke 留待 T-006/T-007。

## [2026-10-07] - IIS-TASK-004 修正手動匯出 URL
- 目的：修正 CPK 匯出 URL 手動組字串時重複加 `/api` 的風險，支援 T-003 的相對 API base。
- 範圍：僅調整 `SpcQueryView.vue` 匯出 URL 組法；不調整 IIS、後端 route 或其他 API 呼叫。

## [2026-10-07] - IIS-TASK-003 前端 API base 改為相對路徑
- 目的：支援 SPC 單一 IIS Site 部署，讓前端預設呼叫同 origin `/api`，移除 production fallback 的固定 IP/port。
- 範圍：僅調整 `frontend/mes-spc-web/src/api/client.js` 與 `.env.example` 的 API base 預設；不處理 T-004 手動組 URL、不修改 IIS 設定與後端 route。現行測試站仍為 Web/API 分離，testhost 發布暫保留既有 API base，待 T-006 單一 IIS Site 設定後再切換。

## [2026-10-07] - PORTAL-TASK-003 工作池狀態同步
- 目的：使用者確認準備執行公告主旨自動帶組織單位時，核對發現 PmrPortal 已有完成規格與測試站 API 發布紀錄；同步移除 SPC `TODO.md` 未完成項目。本階段只整理文件。

## [2026-10-07] - 確認設定與密鑰安全小工作結案
- 目的：依使用者確認，將已完成但待確認的 `TASK-001：設定與密鑰安全` 自 `TODO.md` 未完成工作池移除；本階段只整理文件。

## [2026-10-07] - TODO 只顯示未完成小工作
- 目的：依使用者要求移除 `TODO.md` 已全數完成的小工作，後續工作池只顯示未完成、待執行或待確認項目；完成紀錄仍保留於 changelog 與 specs。

## [2026-10-07] - 正式機更新工具、全專案 AI+BDD 與 KM 教育訓練系統規劃
- 目的：依使用者要求規劃正式機程式碼更新工具、全專案 AI+BDD 導入，以及 KM 教育訓練系統；本階段只做系統設計、執行計畫與工作池排序，不實作、不操作正式機。

## [2026-10-07] - Portal 生日/團保與 SPC 咬蝕 X- 小工作規劃
- 目的：依使用者新需求，將 Portal 生日快樂通知、團保專區，以及 SPC 咬蝕量 X- / 不生產資料不列入 SPC 排入小工作池；本階段只做發想、排序與文件，不修改功能程式。

## [2026-10-07] - SPC AI + BDD 開發模式導入
- 目的：在既有 SDD 流程上補齊 SPC 主專案 BDD 驗收與 AI Coding 協作規則，建立 `features/` 入口與流程規格；本階段只改文件與目錄，不修改業務功能。

## [2026-10-07] - KM TASK-000 SDD + BDD 開發框架初始化
- 目的：依使用者要求將 KM 專案導入 SDD + BDD + AI Coding 工作模式，建立本地入口、規格、BDD、架構、進度與測試目錄；本階段只做文件框架，不修改業務功能。

## [2026-10-07] - SPC 正式機發布
- 目的：依使用者授權更新 SPC 正式機，重建 production 交付包、修正正式 IIS 路徑指向 production backend/frontend，並完成正式 API/Web smoke test。

## [2026-10-06] - SPC-TASK-002-TASK-005 藥液公式批次儲存結果列
- 目的：批次儲存後顯示成功/失敗列結果；成功列重新載入資料，失敗列保留草稿與錯誤訊息供修正。

## [2026-10-06] - SPC-TASK-002-TASK-004 藥液公式總覽批次儲存確認
- 目的：在藥液總覽頁新增批次儲存確認，且只送出有異動的公式列；沿用既有單筆 PUT 與公式版本紀錄，不新增後端 API。

## [2026-10-06] - SPC-TASK-002-TASK-003 藥液公式總覽 draft 與異動標示
- 目的：在既有藥液總覽頁建立公式欄位 draft、dirty row 判斷與列變更標示；本階段不送出批次儲存、不新增後端 API。

## [2026-10-06] - SPC-TASK-001-TASK-006 藥液公式版本查詢與回復 API
- 目的：新增藥液公式版本查詢與指定版本回復 API；回復沿用版本服務並限制 Editor/Admin，本階段不改前端。

## [2026-10-06] - SPC-TASK-001-TASK-005 主檔更新自動記錄藥液公式版本
- 目的：整合既有 `PUT /part-process-characteristics/{id}`，在 CHEM 藥液公式變更時自動建立版本紀錄；本階段不新增 API、不改前端。

## [2026-10-06] - SPC-TASK-001-TASK-004 藥液公式版本服務
- 目的：新增藥液分析公式版本服務，負責公式變更比對、版本建立與指定版本回復；本階段不接 API、不改前端。

## [2026-10-06] - SPC-TASK-001-TASK-003 藥液公式版本資料模型
- 目的：建立藥液分析公式版本紀錄的後端 entity、DbSet、EF mapping 與 migration；本階段只建立資料模型，不接 API、不改前端。

## [2026-10-06] - SPC-TASK-001 藥液分析公式版本記錄規格
- 目的：釐清藥液分析公式現況，建立版本記錄與回復功能的規格、計畫與任務；本階段只改文件，不修改產品程式。

## [2026-10-06] - SPC-POINT-FILTER-TASK-015 文件同步與收尾
- 目的：同步 CHANGELOG_CUSTOM.md、TODO、需求索引與規格驗證紀錄，將 SPC 點位排除/隱藏恢復測試站階段收尾；本階段只改文件。

## [2026-10-06] - SPC-POINT-FILTER-TASK-014 測試站總 smoke test
- 目的：確認目前 SPC 測試站 frontend/backend 已可正常回應，且新版前端 JS MIME 正確；本階段只驗證不改程式、不發布正式站。

## [2026-10-06] - SPC-POINT-FILTER-TASK-013 前端 build / UI 測試
- 目的：確認管制圖與趨勢圖點位排除/恢復前端仍可 build，並檢查新增清單入口與恢復呼叫存在；本階段只驗證不改程式。

## [2026-10-06] - SPC-POINT-FILTER-TASK-012 後端回歸測試
- 目的：確認單點排除後端口徑未被後續前端小工作影響，涵蓋單點排除計算、趨勢圖 normality 與 Xbar 代表案例；本階段只驗證不改程式。

## [2026-10-06] - SPC-POINT-FILTER-TASK-011 趨勢圖已排除點恢復清單
- 目的：補上趨勢圖已排除點清單，讓 `ExcludedHidden` 點即使圖上不可見也可恢復；本階段限定趨勢圖頁，不改後端 API 與資料表。

## [2026-10-06] - SPC-POINT-FILTER-TASK-010 趨勢圖右鍵單點排除
- 目的：在趨勢圖點位新增右鍵選單，呼叫單點排除 API 設定顯示排除、隱藏排除或恢復；本階段先不做趨勢圖已排除點清單。

## [2026-10-06] - SPC-POINT-FILTER-TASK-009 管制圖已排除點恢復清單
- 目的：補上管制圖已排除點清單，讓 `ExcludedHidden` 點即使圖上不可見也可恢復；本階段限定管制圖頁，不處理趨勢圖清單。

## [2026-10-06] - SPC-POINT-FILTER-TASK-008 管制圖右鍵單點排除
- 目的：在管制圖點位新增右鍵選單，呼叫單點排除 API 設定顯示排除、隱藏排除或恢復；本階段先不做已排除點清單，避免隱藏點缺恢復入口前擴張過多。

## [2026-10-06] - SPC-POINT-FILTER-TASK-007 趨勢圖與常態檢定套用單點排除
- 目的：補齊趨勢圖/直方圖共用的 normality 資料口徑，以及 Attribute chart 單點排除；本階段仍不處理前端右鍵選單與隱藏點渲染。

## [2026-10-06] - SPC-POINT-FILTER-TASK-006 管制圖計算套用單點排除
- 目的：讓管制圖後端計算讀取 active `SpcPointExclusions`，將單一量測點標記為不列入計算；本階段先限管制圖變量資料與子組，不處理前端右鍵選單、隱藏渲染、趨勢圖/直方圖。

## [2026-10-06] - SPC-POINT-FILTER-TASK-002 單一圖點排除資料模型與 API
- 目的：建立管制圖/趨勢圖單一量測點排除狀態的後端基礎，提供查詢已排除點、設定顯示/隱藏排除與恢復 API；本階段先不接入圖表計算與前端右鍵選單，避免一次改動過大。

## [2026-10-05] - SPC 修正-1002 Excel 解析與小工作排序
- 目的：依使用者提供的 `docs/SPC 修正-1002.xlsx` 解析 SPC 待辦，將常態檢定、界線預設、Ca 顯示、班別合併、raw data 下載與趨勢圖分布功能排入小工作；本階段只規劃，不修改功能程式、不建置、不發布。

## [2026-10-05] - SPC 測試問題：總覽下拉與資料數口徑修正
- 目的：依 `docs/SPC測試問題_20261005.xlsx` 修正製程總覽線別下拉混入非線別項目，以及 Xbar 類項目總表資料數與管制圖管制點數口徑不一致。
- 補充：`TotalCount` 口徑修正只套用製程 `PROC`，藥液 `CHEM` 維持 raw data 筆數，避免影響藥液功能與資料計算。
- 再修正：製程標準代碼為 `PROCESS`，已改用標準化 control scope 判斷，避免總覽資料數仍回退 raw data 筆數；已發布 SPC 測試站 backend，備份 `backend.backup-process-total-count-scope-170030`。
- OOS 再確認：製程總覽 `OosCount/OocCount` 同樣改用製圖 `chartData.points` 口徑；藥液 `CHEM` 維持 raw data 口徑。已發布 SPC 測試站 backend，備份 `backend.backup-process-oos-scope-193639`。

## [2026-10-05] - SPC-1002-TASK-006 趨勢圖直方圖
- 目的：依 `docs/SPC 修正-1002.xlsx` 第 4 項，讓趨勢圖頁也能查看 raw data 分布、常態曲線與 P-value，但不顯示管制界線、不套用規則管理。
- 發布：前端 testhost build 通過，已發布 SPC 測試站 frontend，備份 `frontend.backup-trend-histogram-203651`。

## [2026-10-05] - SPC-1002-TASK-001 常態檢定 P-value 修正
- 目的：修正小樣本 raw data 常態性檢定 P-value 偏高的問題；以 adjusted Jarque-Bera 統計量取代一般漸近 Jarque-Bera 統計量，讓類似系統顯示約 0.10 但實際應判定 `<0.05` 的資料可正確標示偏離常態。

## [2026-10-05] - SPC-1002-TASK-003 Ca 顯示正負號驗證
- 目的：依 `docs/SPC 修正-1002.xlsx` 第 3 項確認 Ca 顯示已移除絕對值；本次僅驗證後端計算、前端顯示與既有回歸測試，無功能程式異動。

## [2026-10-05] - SPC-1002-TASK-004 班別管制圖合併呈現
- 目的：讓 Xbar-R/Xbar-S 管制圖在同圖合併不同班別資料時，點位 metadata、Tooltip 與詳細卡可顯示班別/取樣階段；不改資料庫、歷史資料、排序規則或統計公式。

## [2026-10-05] - SPC-1002-TASK-005 raw data 下載
- 目的：新增管制圖 raw data CSV 下載，沿用既有查詢條件與權限，讓使用者可用 Excel 對帳製程/藥液原始量測資料與常態檢定結果。

## [2026-10-05] - 小工作排序與 Portal/SPC 新需求規劃
- 目的：依使用者要求，將未來發想先進工作池、依輕重緩急排序及避免衝突的規則寫入入口文件；同時把 Portal 公告/福利文案與 SPC 藥液公式需求排入小工作清單。本階段只規劃，不修改功能程式、不建置、不發布。

## [2026-10-05] - SPC 單一 IIS Site 部署規格
- 目的：依使用者確認，建立 SPC Web/API 改為單一 IIS Site 的最小修改規格與 rollback 計畫；本階段只做文件，不修改產品程式、不發布 IIS、不改資料庫或 AD/SSO 邏輯。

## [2026-10-05] - 校正列表隱藏保管人欄位測試站發布
- 目的：收尾 20260921 小型前端變更，將已建置通過但待發布的校正列表欄位調整發布至 SPC 測試站；不改後端、資料庫、Portal 或正式站。

## [2026-10-01] - Particle Monitoring 前端工作台
- 目的：提供 Particle 原始資料、趨勢、R1-R9 位置比較及 C/U 管制圖的單一操作畫面，並清楚顯示抽樣基準與 U-chart 資料限制。

## [2026-10-01] - Particle Monitoring TransFiles Long Format 串接
- 目的：讓落塵來源改用 Particle 專屬 preview 契約並保留逐筆追溯；避免舊 Attribute `UnitCount=1` 被誤當成 U-chart 真實抽樣體積。

## [2026-09-30] - 管制圖規格線與管制線顯示穩定化
- 目的：修正分段規格線未納入 y 軸範圍、動態管制線資料不足時無 fallback，造成勾選規格/管制界限後偶發看不到線。

## [2026-09-30] - SPC SSO 重導迴圈保護
- 目的：避免 API 401、SPC `/login`、Portal `/Spc/Launch`、`/portal-sso` 之間短時間反覆導頁，造成畫面閃爍與網址一直重讀；僅調整前端登入導向防抖，不改後端與資料。

## [2026-09-30] - 落塵監控 DUST_C/DUST_U 圖型計算
- 目的：讓 SPC 落塵監控主檔 `DUST_C`、`DUST_U` 能銜接既有 Attribute C-chart/U-chart 計算與查詢畫圖路徑，避免畫面查詢時被判定為不支援圖型。

## [2026-09-29] - 落塵監控查詢維度支援
- 目的：讓 SPC 管制圖頁與 summary API 正式辨識 DUST 落塵監控維度，避免 DUST scope 在未掛圖型時被誤歸類為製程；補前端 DUST 配色與 scope 測試。

## [2026-09-29] - 落塵監控管制圖主檔種子
- 目的：新增 SPC 落塵監控的最小後端主檔支援，確保測試/新環境具備 DUST 群組與 C-chart、U-chart 可選圖型；不建立落塵量測資料、不處理 TransFiles 匯入。

## [2026-09-29] - CA 顯示保留正負號
- 目的：依 SPC 藥液修改需求，CA 不再取絕對值並可顯示正負號；Cpk/Ppk 維持既有 mean、規格與 sigma 公式結果不受 CA 顯示符號影響。

## [2026-09-29] - PPC 重複鍵儲存防護
- 目的：修正編輯 SPC 管制項目時，同鍵其他列造成 SQL Server 唯一索引錯誤直接回到前端；改為後端先偵測並回傳清楚 409 訊息。

## [2026-09-22] - 藥液 GENERAL 班別視同早班
- 目的：管制圖與畫面不再把 SamplingPhase=GENERAL 當成「一般」丟掉；GENERAL 視同早班。開／收線仍只限 N1／N2。不改資料列。

## [2026-09-22] - 正式 N1／N2 舊 CLOSE 轉收線
- 目的：使用者授權後，將正式庫 198 筆 CHEM N1/N2 舊 `SamplingPhase=CLOSE`＋`GENERAL` 改為早班代碼 `OPEN`＋收線 `CLOSE`；只改兩欄，先備份再交易更新。

## [2026-09-22] - N1／N2 9 月前僅開收線
- 目的：記錄 9 月前只有開線／收線、9 月後才分早中班，並禁止調整既有資料；取消前案正式 198 筆轉換。

## [2026-09-22] - N1／N2 舊晚班匯入資料轉收線盤點
- 目的：唯讀核對兩庫 schema、PortalDaily 舊 CLOSE 候選與轉換後衝突；正式候選 198 筆，未執行資料修正。

## [2026-09-22] - 咬蝕正式匯入規格
- 目的：確認 `PMR_PORTAL_UAT`／`PMR_SPC_2026` 正式目標，建立固定 48 份 manifest 的正式匯入、備份、防誤連、對帳與回復規格；本次不執行正式寫入。
- 實作：匯入工具新增 production dry-run、雙重 confirm 與連線守衛，產出正式主檔變更計畫；未執行 apply。
- 備份：兩個正式庫 COPY_ONLY／CHECKSUM／VERIFYONLY 通過，新增 24 小時備份證據守衛；仍未執行 apply。
- 正式執行：使用者另行授權後匯入 48 份；Portal 3,650 點、SPC 3,746 筆，逐點、16 圖與防重跑通過；無網站發布。

## [2026-09-22] - 咬蝕正式庫唯讀盤點
- 目的：比對測試 9 月 48 份與正式目標現況；只讀不寫入，供後續正式匯入決策。

## [2026-09-17] SSO 工號／AD 單一使用者
- 目的：完整簽章配對既有工號與 AD，保留 ID/權限，阻止 legacy 失敗重試另建帳號；核心先建測試，僅 SPC 測試發布。

## [2026-09-17] SPC 直接導向 Portal 登入
- 目的：移除 AD 登入過渡卡片，保留 Portal SSO 及原頁返回；僅測試前端發布。

## [2026-09-17] Chameleon 來源表缺漏
- 目的：補上既有服務所需來源表的專屬 EF migration，修復設備即時狀態 SQL 無效物件錯誤；測試 API 發布，不改設備或正式庫。

## [2026-09-17] 藥液班別與取樣階段分離
- 目的：保留所有線別早/中班，僅 N1/N2 增加開/收線獨立維度，避免互覆；核心先建測試，EF migration，僅測試發布。

## [2026-09-16] 一句話需求預設流程
- 目的：將使用者確認的 SDD 精簡模板持久化為 SPC 專案指示，僅適用開發請求。

## [2026-09-16] 儀器列表捲動改善
- 目的：限制表格高度、固定表頭並提供左右捲動按鈕，避免大量資料需拉至頁尾。僅前端呈現。

## [2026-09-16] 儀器規格欄位
- 目的：补齊量測規格、精度、備註、校驗規範、允收標準，主檔/匯入一致，僅補測試庫空白，保留日期及通知。核心先建測試。

## [2026-09-15] - 校正通知選用 Synology Chat
- 修改目的：新增 Email/Chat 管道選擇、加密 webhook、手動測試及持久化排程分流；先建核心測試，僅發布 SPC 測試站。

## [2026-09-15] - 校正提醒天數簡化設定
- 修改目的：逗號輸入改為常用天數勾選與自訂新增，保留既有值與通知規則。

# 10 Change Log

## [2026-09-14] - 儀器主檔直接修改校正日期目的
- 使用者明確要求主檔儲存時可直接修改校正日期；取代禁止修改上次日期的舊程式限制，不限首次補填或無歷史資料。
- 先建立日期編輯、歷史/稽核、日期驗證、通知與版本回歸測試，再移除主檔上次日期禁止變更判斷；保留原到期日改期理由與通知規則。
- 依 `specs/20260914-calibration-date-edit/` 完整 SDD 實作、驗證，通過後直接發布 SPC 測試站；不改正式資料、不寄真信、不變更 Portal。

## [2026-09-12] - 儀器校正到期管理亮色配色
- 依使用者「顏色太暗、太重」，校正管理頁改為亮底青／天空／琥珀配色，不改欄位、匯入或通知規則。

## [2026-09-12] - 未校／免校列先建主檔、日期後補
- 依使用者「剩下的 12 筆先匯入，讓我自己建立日期」：未校、預計、-、-- 不轉成日期；免校／-- 週期先以 12 月建檔並警示。
- 下次到期日改可空；無到期日不列入摘要、不寄提醒。使用者之後在主檔自行填日期。

## [2026-09-12] - 校驗方式公式改讀儲存值
- 正式 Excel 的 J 欄是共用公式（依年校／送校／廠校得出外校／內校／免校）。改為與日期相同：有儲存值就匯入並警示，沒有儲存值才拒絕。
- 補上實際範本 47 筆可匯入、12 筆因未校／預計／免校不合格的測試，避免再把整表擋下。

## [2026-09-12] - 儀器列表改依編號排序
- 依使用者「以儀器設備編號為排序」，儀器校正管理列表改依編號（Code）排序，不再先依下次校正日。
- 到期摘要與測試寄信仍依到期日選樣，不改提醒規則。

## [2026-09-12] - 儀器主檔顯示校驗方式
- 依使用者「秀出校驗方式」，新增可選主檔欄位，匯入讀 Excel J 欄，列表／表單／預覽顯示。
- 只當文字保存，不改週期或免校判定；先補解析與寫入測試再改主檔與畫面。

## [2026-09-12] - 儀器主檔顯示放置地點
- 依使用者「秀出放置地點」，新增可選主檔欄位，匯入讀 Excel H 欄，列表／表單／預覽顯示。
- 不拿放置地點推定部門；先補解析與寫入測試再改主檔與畫面。

## [2026-09-12] - 儀器校正測試通知按鍵
- 依使用者要求在儀器校正管理新增手動測試寄信，沿用 SMTP 測試路徑與【測試】主旨，不寫入到期通知紀錄，不開啟 `Calibration:DeliveryEnabled`。
- 先新增權限與防重寄回歸測試，再實作 API 與畫面按鍵。

## [2026-09-12] - 儀器 Excel 匯入測試站發布目的
- 依使用者後續「發布」授權，延續前案測試站範圍，只部署 SpcApi/SpcWeb 至 D:/SPC/release/test/backend、frontend。
- 已核對 IIS 實際目錄/8081、8083 綁定與 PMR_SPC_TEST，保留 appsettings/web.config、附件與既有設定；備份後部署並核對 hash/HTTP。
- 不發布正式站、不修改 Portal、不匯入真實儀器資料、不啟用寄信。發布證據納入原匯入規格。

## [2026-09-12] - 儀器 Excel 批次匯入本機驗證完成
- 完成解析/預覽/勾選/交易匯入、權限重驗及稽核；共用既有主檔驗證，不修改量測或通知邏輯。
- 72 後端與 3 隔離 UI 測試通過；API/Web 建置成功。測試主機改用與產品相同的 .NET 10 測試套件及記憶體金鑰，未使用產品啟動/通知服務。
- 介面已檢查桌面/手機；測試站登入與 SQL Server 端到端待驗證；未發布、不寫正式庫、不寄真信。
- 規格與證據：`specs/20260911-instrument-calibration-import/`；技術契約：`ai_docs/import-formats/instrument-calibration.md`。

## [2026-09-11] - 儀器 Excel 批次匯入開發目的
- 依本次使用者授權建立 `specs/20260911-instrument-calibration-import/` 完整 SDD；在既有校正模組新增第一工作表解析、預覽勾選、僅新增交易匯入及稽核。
- 先建立解析、日期、重複、交易回復及權限測試，再實作核心；保留來源公式快取日期，不臆造合格紀錄、部門或保管人。
- 使用隔離 SQLite 及本機驗證，不發布 IIS、不寫正式資料庫、不寄信；不修改來源 Excel。

## [2026-09-11] - 儀器校正測試站發布
- 補齊 AddCalibrationModule 的 EF Designer，使測試庫 `PMR_SPC_TEST` 能套用校正資料表。
- 只發布 `release/test` 四站；保留測試 appsettings／web.config；`Calibration:DeliveryEnabled` 未開啟。
- 正式庫、正式 IIS、`D:\Sites\PmrPortal` 不發布。

## [2026-09-11] - 儀器校正通知完成本機實作與驗證
- 依規格 v4 決議（R-001～R-016）補齊排程掃描、摘要計數、前端台北日與月週期預覽，並以假郵件／隔離資料庫做本機驗證。
- 核心修改前先補單元測試：08:00 掃描、送校中摘要、改期必填原因、保管人可取消、第 4 次不重試。
- 不套用正式 DB、不寄真信、不發布；`Calibration:DeliveryEnabled` 預設關閉，背景仍掃描但不連 SMTP。

## [2026-09-11] - 儀器校正 Q-01～Q-06 定案對齊
- 使用者回覆「同意」，六項建議方案正式定案；規格升為 v4 可實作。
- 對齊摘要即將到期窗口（跟隨最大提醒天數）、送校中計入摘要、Editor 預設頁面權限含校正管理。
- 僅本機測試與文件；不套用正式 DB、不寄真信、不發布。

## [2026-09-11] - 儀器校正 v2 實作目的
- 使用者同意實作及本機驗證；新增校正主檔、歷史附件、授權、持久化通知、SPC UI 與 Portal 摘要。
- 核心修改前建立測試；migration 僅產生檔案，測試用隔離資料與假郵件，不發布、不寄真信。

## [2026-09-11] - 儀器校正到期通知 SDD 文件目的
- 依使用者授權盤點 SPC／Portal 主檔、權限、SMTP、排程及附件能力，建立 `20260911-instrument-calibration` 跨專案規格、計畫、任務與驗證方式。
- 本次僅文件；未決規則保留草稿，不修改程式、資料庫、不寄信、不發布。

## [2026-09-10] - SPC 藥液匯入資料移轉正式庫目的
- 依使用者「同意，並移轉」，先盤點測試／正式差異、驗證備份，再僅新增可追溯的 CHEM 藥液匯入資料。
- 保留正式既有資料；排除手動量測、其他管制資料、人員、主檔與設定，不發布應用程式。
- 規格：`specs/20260910-chemical-production-transfer/spec.md`；執行結果以驗證紀錄為準。

## [2026-09-10] - 藥液管制圖早中晚班辨識
- 將藥液管制圖由早班/晚班雙序列調整為早班、中班、晚班獨立序列，避免中班資料在配對模式遺漏。
- 僅修改 SPC Web 顯示與 Tooltip，不改變量測、規格或 SPC 運算；只發布測試環境。

## [2026-09-09] - 週月報表排行、Cpk 公式與 AD/工號登入相容
- 週/月報表新增 OOS/Cpk 前5與後3排行及 Cpk/Ppk 公式說明。
- Portal 工號格式正規化，維持 AD 帳號優先與多重對應拒絕。
- 只驗證並發布測試環境。

## [2026-09-09] - 藥液量測品保權限與異動稽核
- Portal SPC 代理端點維持品保角色授權。
- SPC 藥液日報新增、修改、刪除均寫入 `UploadDetails` 稽核明細，刪除先記錄原值再移除量測、計算及警示。
- 只發布測試環境，正式環境不發布。

## [2026-09-09] - 規格線、早中班與當日藥液刪除開發目的
- 依 `specs/20260909-chemical-entry-and-chart/spec.md` 修正圖表刻度、加入獨立班別與當日單筆刪除。
- 核心邏輯先補測試；只驗證並發布測試站，正式環境不發布。

## [2026-09-09] - 其餘七專案 SDD 文件導入目的
- 依使用者同意，將既有共用流程延伸至其餘七個工作區；新增文件入口與來源索引，更新導入狀態。
- 開工規格：`specs/20260909-sdd-rollout/spec.md`。本次不涉及產品、資料或部署修改。

## [2026-07-08] - SPC 製圖後圖表名稱顯示規則
- **SPC 管制項目總覽製圖**:
  - 從總覽列點擊「製圖」後，管制圖頁上方新增「圖表名稱」區塊。
  - 圖表名稱依序使用 `線別 / 製程線別`、`槽位`、`管制圖名稱` 組合，格式為 `線別 - 槽位 - 管制圖名稱`。
  - 若不是從總覽列進入，前端會以 `PartProcessCharacteristic` 主檔資料 fallback 組合製程/機台、槽位、品質特性名稱。

## [2026-07-08] - 登入角色選單與路由權限收斂
- **前端權限顯示規則**:
  - `Viewer` 登入後左側選單僅顯示「儀表板」、「SPC 管制圖」、「量測值趨勢圖」。
  - `Editor` 登入後顯示全部功能選單。
- **路由保護同步**:
  - 將異常管理、追溯查詢、系統操作手冊等非三個檢視頁同步標記為 `editorOnly`，避免 Viewer 直接輸入 URL 進入非授權頁面。

## [2026-07-08] - SPC 總覽槽位欄位與週/月報寄送設定
- **SPC 管制項目總覽 (Summary)**:
  - `/api/v1/spc/summary` 回傳新增 `SlotName`，前端 `SpcSummaryTable` 於「製程線別」後顯示「槽位」欄位。
  - 總覽表水平捲動寬度同步調整，避免新增欄位後遮擋後方 Ppk、OOS、製圖等資料。
- **SPC 週報 / 月報寄送設定**:
  - 新增前端頁面 `/settings/spc-reports`，提供部門篩選、使用者收件人勾選、週報/月報啟用、寄送時間設定。
  - 預設寄送時間為 `08:00`；週報預設星期一，月報預設每月 1 日。
  - 側邊欄「系統管理與通報設定」新增「SPC 週報月報設定」入口。
  - 新增 `POST /api/v1/spc-report-settings/send-now` 立即寄送端點，前端提供「立即寄送週報」與「立即寄送月報」按鈕，可直接測試 SMTP、收件人與 Excel 附件。
  - 立即寄送回傳每位收件人的成功/失敗結果，且不更新正式排程的上次寄送時間，避免測試寄送影響排程判定。
- **Excel 匯出與排程期間**:
  - `SpcOverviewReportService` 改為沿用 SPC 總覽統計結果產生 Excel，欄位包含槽位、本期/上月 Ppk、本期/上月 OOS、%OOS、OOC、管制界線計算方式等。
  - `SpcReportSchedulerService` 週報改取上一週完整期間，月報改取上一個完整月份，並透過既有 SMTP 寄送服務附加 Excel。
- **測試資料清除限制**:
  - 本次清除測試資料僅允許使用 `/api/testdata/clear-tagged` 的安全標記清除流程，保留維護主檔；若正式 SQL Server 無法連線，不改用直接刪表或手動刪除方式。

## [2026-07-08] - SPC 總覽比較欄位與管制圖/趨勢圖分流修正
- **SPC 管制項目總覽 (Summary)**:
  - `/api/v1/spc/summary` 回傳新增 `GroupType`、`ChartKind`，前端總覽表可直接顯示「管制圖 / 趨勢圖」。
  - 總覽表新增上一個完整月份比較欄位：上一月 `Ppk`、上一月 `OOS 件數`、上一月 `%OOS`，方便與目前查詢區間的本期數據對照。
- **圖表分流與頁面責任拆分**:
  - `SpcSummaryTable` 製圖按鈕依列資料的 `GroupType` 顯示管制圖或趨勢圖圖示。
  - `SpcChartView` 若收到趨勢圖列資料會導向 `TrendChartView`；`TrendChartView` 若收到管制圖列資料會導向 `SpcChartView`。
  - `SpcChartView` 移除內嵌的「量測點位趨勢圖」區塊，管制圖頁只呈現 SPC 管制圖、異常點清單與量測值分布直方圖；原始量測時序趨勢集中於趨勢圖頁。
- **公式顯示一致性修正**:
  - 管制圖頁公式標籤改讀實際 primary control limit 的 `calculationMethod`，支援 `iControlLimitsStat`、`xbarControl` 與計數型控制界限來源。
  - I-MR / 單值移動全距圖統一顯示「移動全距法」，避免總覽顯示移動全距法但圖內誤顯示標準全距法。
- **管制圖與趨勢圖互斥設定**:
  - `PartProcessCharacteristic.DisplayMode` 改為只能選擇 `CONTROL_CHART` 或 `TREND_CHART`，不再允許 `BOTH`。
  - SPC 管制項目設定頁改為「管制圖 / 趨勢圖」二選一，圖表種類下拉只顯示所選圖種的類型。
  - 後端儲存時驗證 chart type 所屬 `ControlChartGroup.GroupType` 必須與 `DisplayMode` 一致，避免 API 直接送出管制圖與趨勢圖混用設定。

## [2026-06-25] - 作業人員角色權限與匯入量測者帳號整合
- **修改目的**:
  - 將既有作業人員主檔擴充為可登入的系統使用者，提供 `Viewer`（僅檢視與查詢）及 `Editor`（完整操作）兩種角色。
  - 計量型與計數型資料確認匯入時，將檔案內量測者自動加入系統使用者管理並預設為 `Editor`。
  - 權限限制同時落實於後端 API 與前端操作介面，避免只隱藏按鈕卻仍可直接呼叫寫入 API。
  - 清理資料時僅移除具有測試標記的資料，保留既有維護主檔與正式使用者。
- **相容性原則**:
  - 既有作業人員資料升級後預設保留 `Editor` 權限。
  - 保留既有 demo 管理帳號作為過渡登入方式。
  - 所有資料庫結構變更均透過 EF Core Migration 執行。

## [2026-06-24] - 新增管制界線試算與分段管控 (Trial Calculate & Segmented Control Limits)
- **後端分段界線模型與資料庫升級 (AppDbContext & Program.cs)**:
  - 新增 `ControlLimitSegment` 實體與 `ControlLimitSegments` 資料表，用於儲存不同時間區間的自訂統計管制界線。
  - 在 `Program.cs` 啟動階段加入 SQL 自動建表邏輯，避免直接執行 Migration 時遭遇 Windows 沙箱權限阻擋。
- **點級動態界線異常判定與圖表運算 (SpcEngine & Rules)**:
  - 升級 `WesternElectricRulesValidator` 與 `NelsonRulesValidator` 的異常檢定引擎，支援「點級動態界線解析」。各點所處時間對應的分段管制界線（`UCL`/`CL`/`LCL`）會作為檢定基準，若無分段則回退至全局設定。
  - 升級 `AttributeChartCalculator`、`ImrChartCalculator`、`XbarRChartCalculator`、`XbarSChartCalculator`，在輸出數據點時將 active UCL/CL/LCL 動態對應至各個量測點，並輸出 `uclStat`/`clStat`/`lclStat` 供前端繪製。
- **後端試算與分段 CRUD API (Controllers & Services)**:
  - 新增 `ControlLimitSegmentsController`，提供分段管制界線的 CRUD 端點，並在建立或更新時實作時間區間重疊的防呆校驗。
  - 在 `SpcController` 新增 `trial-calculate` 試算端點，允許輸入日期區間，拉取該區間內的量測數據並呼叫 SPC 引擎計算出統計界界線值。
- **安全清理測試資料 (DatabaseSeeder.cs)**:
  - 實作 `ClearTransactionalDataAsync()` 方法，只清除量測值、批次、警報等交易性測試數據，安全保留料號、工站、檢驗特性等主配置維護檔 (維護主檔)。
  - Expose `/api/testdata/clear-transactions` 路由至 `TestDataController`。
- **前端介面分段管理與 stepped 曲線渲染 (frontend)**:
  - `PartProcessCharacteristicsView.vue`: 新增「分段管制線與界線試算」功能按鈕。點擊後開啟 Modal，左側可進行歷史數據試算，一鍵帶入右側；右側支援分段上限、中心線、下限、有效日期區間及備註的 CRUD 管理，並整合後端區間重疊錯誤回報。
  - `SpcChartView.vue`: 配合動態界線，將 ECharts 中心線 (CL) 改為如同 UCL/LCL 的 stepped line (階梯折線) 渲染，與點級分段相符。

## [2026-06-24] - 查詢時間範圍上限與預設值限制 (日期卡關)
- **後端安全防護與效能優化 (SpcController.cs)**:
  - 限制 `/chart` 與 `/summary` 統計查詢端點的時間間隔最高不可超過 93 天（約 3 個月），防範大量量測數據的掃描。
  - 當查詢起迄日為空時，預設自動查詢最近 3 個月的數據，防止全表無日期過濾的重度資料庫查詢。
- **前端日期選取與阻擋 (SpcChartView.vue)**:
  - 預設載入「量測起日」為 3 個月前，「量測迄日」為今日。
  - 當使用者挑選日期區間大於 3 個月（93 天）時，由前端主動進行阻擋並回報「查詢時間範圍最多不可超過 3 個月」警告訊息，不向後端發送無效請求。

## [2026-06-23] - 新增直方圖常態分佈曲線疊加與常態性檢定
- **後端常態性檢定與曲線生成 (SpcEngine & Services)**:
  - 實作 Jarque-Bera 常態性檢定，包括偏態 (Skewness)、峰態 (Kurtosis) 及卡方生存函數計算 p-value。
  - 實作與前端組寬對齊的平滑常態 PDF 曲線產生器，點數 100 點，並透過 $ScaledPdf = PDF \times n \times binWidth$ 高度轉換重合公式，確保其能與計數直方圖無縫貼合。
- **前端直方圖與常態曲線疊加 (SpcChartView.vue)**:
  - 採用 ECharts 雙 X 軸方案，解決 category 軸柱狀圖與 value 軸曲線無法依數值精準對齊的問題。
  - 新增直方圖頂部常態性檢定專屬指標卡，展示偏態、峰態、p-value 與常態判定（顯著水準 $\alpha = 0.05$）。
- **測試防護網**:
  - 新增 `NormalityTest.cs`，包含正常數據、極端偏離常態數據與零變異防呆邊界情況驗證。

## [2026-06-23] - 製程工站槽位與 SPC 管制項目同步問題修復
- **槽位自動刪除與同步機制**:
  - 在 `MasterDataV2Controller.cs` 的 `SyncMachineTanksAsync` 中，實作了槽位（Tanks）與前端 UI 設定的雙向同步。前端移除槽位時，資料庫中對應的槽位記錄一併刪除，並具備外鍵約束保護。
  - 機台建立時，強制呼叫 `SyncMachineTanksAsync` 建立對應的 `ProductionLine`，防止產生無產線對應的孤立機台。
  - 機台代碼更新時，主動同步變更既有的 `ProductionLine.LineCode`，確保關聯的既有槽位對照不因機台改名而遺失。
- **匯入關聯補齊**:
  - 修正 `UploadService.cs`，在 Excel/CSV 自動匯入建立機台時，同步補建 `ProductionLines` 關聯資料，確保大小寫不一致或新機台能正常加載槽位。
- **前端載入時序與防呆優化**:
  - 於 `PartProcessCharacteristicsView.vue` 將槽位過濾改為以 API 動態向後端請求 `/api/machines/{machineId}/tanks`，根治前後端代碼大小寫或空格不一致引起的配對失敗。
  - 重構 `openEditModal` 編輯載入時序，改為先非同步載入槽位清單後再回填表單欄位，徹底防止競爭條件（Race Condition）導致編輯時已選取槽位被重置為空。

## [2026-05-18] - SPC 企業級品質管理系統全面升級 (Phase 1 ~ Phase 4 Complete)
- **資料庫與主檔架構重構 (Phase 1)**: 擴充 EF Core 模型，加入企業層級架構 (`Plant`, `Factory`, `Process`, `Machine`, `PartProcessCharacteristic`) 與中介暫存表，並透過 `SeedData.cs` 自動寫入標準量測項目與西方電氣規則。
- **SPC 運算引擎與西方電氣規則擴充 (Phase 2)**: 實作完整的 Western Electric Rules (Rule 1~4) 自動檢驗引擎，並擴展統計常數表 ($n=2\sim 25$) 與製程能力指數 ($C_p, C_{pk}, P_p, P_{pk}, \hat{\sigma}_{within}, \sigma_{overall}$) 即時運算。
- **兩階段資料匯入引擎優化 (Phase 3)**: 重構 CSV 與 Excel 上傳解析器，完整兼容中英文表頭對應（如 `料號` / `PartNo`，`測量值` / `MeasuredValue` 等），並建立暫存校驗與正式轉入工作流。
- **前端高階戰情室與互動管制圖重構 (Phase 4)**:
  - 升級 `App.vue` 企業級側邊欄與全域黑暗模式切換。
  - 重構 `DashboardView.vue` 戰情中心，加入即時警報走勢圖與 Cpk 後段班排行榜。
  - 重構 `SpcChartView.vue` 為頂級即時互動式 SPC 管制圖戰情室，支援六大管制圖、規格線、統計界限與紅點異常標記。
  - 修復 Vue 3 非同步 DOM 掛載與 ECharts 畫布初始化時序問題 (`nextTick`)，確保動態切換檢驗基準時無縫重繪管制圖。
  - 於 `SeedData.cs` 寫入真實抽樣檢驗歷史數據（25 筆計量型與 15 筆計數型），並完善 `UploadBatch` 關聯，實現開箱即用的完整展示。
  - 升級 `VariableUploadView.vue`, `AttributeUploadView.vue`, `UploadPreviewView.vue` 支援拖曳上傳與 Stage 2 檢核確認。

## [2026-05-15] - Formula Engine & Attribute Charts (P2)
- **Formula Engine 重構**: 將 `FormulaEngineService` 替換為 `NCalcSync` 函式庫，全面支援動態字串表達式解析。
- **內建變數綁定**: 實作動態綁定 `AVG`, `STDEV`, `MAX`, `MIN`, `RANGE`, `SUM`, `COUNT`, `USL`, `LSL` 等保留字，供表達式直接取用。
- **Attribute 管制圖**: 新增 `AttributeChartCalculator` 模組，完整支援計數型 P, NP, C, U 管制圖的界限計算。
- **單元測試**: 為 `FormulaEngineService` 與 `AttributeChartCalculator` 加入 xUnit 測試並全數通過。

## [2026-05-15] - SPC Unit Testing (P4)
- **測試防護網**: 建立 `MesSpc.Api.Tests` xUnit 測試專案。
- **單元測試**: 為 `ImrChartCalculator`, `XbarRChartCalculator` 與 `NelsonRulesValidator` 撰寫完整的覆蓋測試，涵蓋極端案例與常規計算。
- **配置優化**: 透過 `<InternalsVisibleTo>` 暴露內部模型供動態斷言，確保 API 設計乾淨。

## [2026-05-15] - SPC Engine Refactoring (P2)
- **架構解耦**: 將 SPC 核心運算從 `SpcService` 抽離至獨立的 `SpcEngine` 模組，解除 Entity Framework 相依性。
- **領域模型**: 建立 `SpcDataPoint` 與 `Subgroup` 模型，作為運算引擎的標準傳遞物件。
- **計算器模組**: 實作 `ImrChartCalculator` 與 `XbarRChartCalculator`。
- **規則引擎**: 實作 `NelsonRulesValidator`，支援 Nelson Rule 1 至 Rule 6 的自動判定，並將結果附加至回傳資料點。

## [2026-05-15] - Core Architecture Refactoring
- **核心架構重構**: 引入 `BaseEntity` 抽象類別，統一主鍵名為 `Id`。
- **自動稽核系統**: 實作 `AppDbContext` 自動填充 `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`。
- **資料安全與稽核**: 實作軟刪除 (`IsDeleted`) 與樂觀併發控制 (`RowVersion`)。
- **組織架構擴充**: 新增 `Plant`, `Factory`, `ProductionLine`, `Unit`, `Shift`, `Operator` 等主檔。
- **企業化資料庫**: 完成 SQL Server 遷移腳本 `V4_EnterpriseMasterDataAndAudit` 並成功應用。
- **文件化**: 更新 `ai_docs` 描述最新架構。

## [2026-05-14]
- 執行第一階段：專案盤點與現況分析。
- 執行第二階段：建立 `ai_docs` 基礎文件庫。
- 產出改造優先順序報告。

## 2026-09-17 正式資料同步測試庫（準備）
目的：依使用者同意保留校正資料的方式刷新測試業務資料。規格：specs/20260917-production-data-refresh/spec.md；人員 ID 衝突待確認處理邊界，尚未寫入資料庫。

資料同步已完成：41 表同步／18 表保留，全部一致性及保留檢查通過；細節見 specs/20260917-production-data-refresh/verification.md。

## 2026-09-17 藥液歷史查詢完整性
修正Portal每製程200筆截斷；先建測試，再新增SPC分頁／日期條件與Portal完整載入。規格：specs/20260917-chemical-history-complete/spec.md。

## 2026-09-17 藥液日期狀態
先建立測試後新增按月唯讀狀態及Portal自訂月曆；日期選取保留輸入且不自動載入。規格：specs/20260917-chemical-date-status/spec.md。

## 2026-09-17 舊班別早班對應
依使用者確認將GENERAL／空白舊班別視為早班，統一日曆／讀取／寫回防重複；先測試再修正，無批次改資料。specs/20260917-legacy-morning-shift/spec.md。

## 2026-09-17 舊量測日期載入（目的）
補上缺少日報日期時使用量測日期的讀取與更新比對，避免歷史資料不可載入及儲存重複；N1/N2舊CLOSE依最新指示暫緩、不分類。規格：specs/20260917-legacy-measurement-date/spec.md。

## 2026-09-17 咬蝕量第二批目的
保留25/50原始點並補50點X̄-S、線速、Portal完整同步與日期載入，採专用交易upsert避免藥液逐點覆寫。僅試匯0901四線，負值隔離，不匯整月。先建測試再改核心。

## 2026-09-18 咬蝕載入修復目的
針對9/1有資料後API載入失敗，先建MVC序列化回歸，排除父子物件循環，不改量測資料或權限。規格specs/20260918-etch-report-load/spec.md。

## 2026-09-18 月份補匯目的
依使用者要求補入其他9月有效日報以顯示管制圖；擴充試匯工具日期範圍與複合鍵，負值隔離、既有衝突拒絕及測試庫保護不變。先建解析測試。

## 2026-09-18 TransFiles嚴格匯入目的
新增測試限定預覽與嚴格匯入，現有手動修改日報維持不變；來源差異不覆寫，跨站部分失敗可重試。先建立核心測試。

## 2026-09-18 藥液舊開收線資料修正
依使用者更正OPEN/CLOSE階段含義與兩庫資料授權，測試庫先備份修正460筆；正式缺SamplingStage欄位，暫不改以避免舊程式合併資料，須限定相容性升級計畫。

## 2026-09-18 N2中班收線修正目的
依使用者確認將測試庫3日30筆N2中班GENERAL歸CLOSE，保留中班，備份後交易驗證；無程式變更。

## 2026-09-18 N1/N2單線圖目的
依確認將開收線連成同一序列，避免同日班別Map折疊取樣點；API保留階段並依同順序算MR，先測試後實作。
## [2026-09-18] 子組大小輸入支援50
- 目的：將編輯管制項目的前端max從25改50，對齊已支援的X̄-S 50點；不變更核心計算，僅發布測試前端。

## [2026-09-18] 正式完整升級相容性
- 目的：在隔離環境補足Portal/SPC升級契約與migration驗證，準備可核准候選，不發布正式、不更動正式DB。

## [2026-09-18] 已核准完整schema與N1中班收線
- 目的：本機隔離SQL Server套用8項遷移並驗證470筆舊開收線、N1/N2中班收線；正式維持唯讀。

## [2026-09-30] 藥液 F 表資料模型目的
- 目的：為讓 SPC 保存共用 F 表版本、儲存格值與公式引用索引，新增獨立資料表；先只建立承載結構，不覆寫既有 PPC 藥液公式。

## [2026-10-01] Particle Monitoring Long Format 實作目的
- 目的：依 `specs/20260930-particle-monitoring/` 已確認設計，新增 Particle 專用 Long Format 資料模型與 EF migration；先以模型測試保護欄位型別、查詢索引、來源座標冪等鍵及 UploadBatch Restrict 外鍵，不改寫既有 DUST Attribute 歷史資料。
- T-010 目的：新增專用 Particle preview／confirm 服務與 API，沿用既有 staging；先以測試保護欄位錯誤、來源座標重複、跨批疑似重複、reject／skip 與 confirm 冪等，不改既有 Variable／Attribute 匯入行為。
- T-011 目的：新增 Particle 原始量測、單序列趨勢與同事件位置比較 API；先以測試保護 Location／ParticleSize 隔離、穩定排序、缺測 null 與重測歧義，不改既有 SPC 圖表查詢。
- T-012 目的：新增 Particle 專用 C-chart adapter 與 SPC API；先以測試保護 20 點門檻、LCL 不小於 0、同時間重測、bigint 精度與 sequence 隔離，規格線與統計管制線分開回傳。
- T-012A 目的：依使用者確認，讓 Particle SPC 可選 C/U；先測試 U-chart decimal SamplingVolume、動態界線、缺分母拒絕與同時間重測，不以 UnitCount=1 假裝正規化。
# 2026-10-02 TASK-001 設定與密鑰安全

- 目的：避免 SMTP 密碼由 API 回傳，並避免留白更新清空既有密碼；同步清理追蹤中的範例敏感值。
- 範圍：`SettingsController`、SMTP 設定頁、範例設定檔與相關測試。
- 限制：不自動輪替現有測試站或正式環境密鑰，改由部署環境注入並另行執行輪替。

# 2026-10-13 SPC/Portal/KM Web API 單一網址設定

- 目的：依使用者要求，將 SPC、入口網站與 KM 的 Web/API 對外網址統一為站台根網址加 `/api`，避免 Web/API 分成不同外部 URL 或設定誤填 `/api` 造成 `/api/api`。
- 範圍：跨系統設定基準、Portal/KM 相關設定與 BaseUrl 防呆；不操作正式站、不修改資料庫。

## [2026-10-15] - SPC-ETCH-PREVIEW-500-SERVER-DIAGNOSTIC 咬蝕預覽伺服器錯誤可追蹤化
- 目的：處理測試站咬蝕量預覽 123 筆 HTTP 500，提供不含 SQL 或連線機敏內容的錯誤代碼與 Trace ID，以便定位實際資料庫交易例外。
- 範圍：SPC `EtchTestImportController` 安全錯誤回應與結構化紀錄、對應測試；不改 Excel、主檔、計算、schema、Portal、TransFiles 或資料匯入。
- 驗證：先補錯誤回應與既有預覽不寫入測試，再執行後端 build；通過後發布 SPC 測試站 backend，僅以一筆預覽驗證。

## [2026-10-15] - SPC-ETCH-PREVIEW-500-SERVER-DIAGNOSTIC 預覽交易鎖定修正
- 目的：咬蝕量預覽不寫入資料，卻與確認匯入共用 SQL `sp_getapplock`；將該鎖保留於確認寫入，避免測試站 124 份唯讀預覽全部在鎖定路徑失敗。
- 範圍：`EtchReportSyncService` 的預覽交易判斷與測試；不改 Excel、主檔、計算、schema、Portal、TransFiles 或確認匯入鎖定。
- 驗證：先補預覽不得要求寫入交易的單元測試，再執行咬蝕測試、後端 build 與測試站單筆預覽；通過後發布 SPC 測試 backend。

## [2026-10-15] - SPC-ETCH-PREVIEW-AUTH-SCHEME 修正
- 目的：咬蝕量預覽控制器具有 `[Authorize]`，但測試站 `Auth:Enabled=false` 時程式未註冊 JWT Authentication Scheme，授權挑戰因此拋出 500。
- 範圍：依 JWT 設定存在與否註冊 Authentication/Authorization；保留 `Auth:Enabled` 作為全站 controller 預設授權開關，不調整 JWT 金鑰、角色、資料庫、Excel 或匯入內容。
- 驗證：先補啟動決策單元測試，再執行相關測試與 backend build；通過後發布 SPC 測試 backend，僅做未授權預覽回 401 的 smoke test。


## 2026-10-19：咬蝕預覽人工驗證接續
- 目的：依使用者「確認，請開始」接續既有 SPC-ETCH-PREVIEW-500-SERVER-DIAGNOSTIC-20261015 T-004；先驗證測試站登入與既有資料製圖，保留預覽不寫入的既定範圍。
- 範圍：本次先補人工驗證紀錄，不修改程式、設定或資料；TransFiles 25/50 點預覽待實際操作結果，不先標示完成。

## 2026-10-19：咬蝕預覽驗證收尾目的
- 依使用者提供的預覽及確認匯入完成畫面，唯讀核對本機結果檔與測試站新增日期製圖，更新 T-004、需求、TODO 與變更紀錄；不修改程式、不再寫入資料、不重新發布。

## 2026-10-19：Portal 咬蝕預覽診斷轉送驗證目的
- 為既有 SPC-ETCH-PREVIEW-500-PORTAL-FORWARDING-20261015 T-003 新增本機失敗路徑測試，驗證 TransFiles 真實 client/GUI 處理函式的提示與 JSON LOG 可見錯誤代碼及 Trace ID；不修改應用程式、不連線或寫入測試/正式資料庫、不打包或發布。

## 2026-10-19 SPC＋Portal 正式發布目的
- 使用者已明確選擇正式站；先核對遠端 IIS、來源、版本與 schema 相容性，準備隔離套件及備份/rollback。不解除原型工具 Production 防護，不操作未評估的正式 migration，不更新 KM。

## 2026-10-19 正式版更新工具目的
依使用者要求實作 SPC／Portal 正式發布工具：目標白名單、manifest/hash、SQL/網站/IIS 備份、明確授權與失敗回復。先測試再實作；不操作正式站或 DB。

## 2026-10-19 遠端環境切換目的
依使用者確認，增加Test／Production固定目標、遠端入口與單站SPC測試相容；先測試再改核心，保留舊正式與原型防護。本次不Apply／Rollback。

## 2026-10-19 更新程式EXE目的
依使用者要求新增繁體中文Windows執行檔，包裝既有遠端引擎；先建測試再實作授權／參數核心。不變更站台／DB，不執行實際更新。

## 2026-10-19 EXE製作交付包目的
補上使用者缺少的新增交付包入口，靜態讀取API metadata並沿用產包器，不要求手寫JSON／migration清單；先建測試，不執行站台更新。
## [2026-10-19] - SPC-CHEM-REFERENCE-HISTORY-20261019 實作
- 目的：補設定頁參照文件備註及總覽共用版本歷史入口，沿用同PPC與Reason500字；超長拒絕必須在公式寫入前完成。
- 範圍：兩頁、歷史顯示元件、備註驗證與相關測試；無schema／公式運算變更，測試通過後僅發布SPC測試站。
## [2026-10-20] - SPC-VERSION-HISTORY-DATE-FILTER-20261019 實作
- 使用者同意規格版本表與測試庫migration。新增不可變規格快照，統一EF寫入紀錄；三類歷史依台北修改日期先篩後分頁。正式與KM不動。
- 先建立日期邊界／版本／交易測試，隔離庫演練Up／Down後備份驗證測試庫，再發布SpcSingleTest；無舊歷史虛構回填。
## [2026-10-20] - SPC-TEST-WEB-API-SAME-ORIGIN-20261020
- 修正testhost舊VITE_API_BASE污染產物，API client固定同站/api；先補瀏覽器回歸，實際bundle驗證後僅發布測試前端，DB／後端不動。
## [2026-10-20] - SPC-CHART-SEGMENT-VERSION 需求登錄
- 目的：將點擊管制圖日期分段查看版本與分析公式加入工作池，保留歷史證據不足及相依功能限制。
- 範圍：僅 spec、TODO、需求索引與變更紀錄；無程式、資料庫或發布操作。

## [2026-10-20] - SPC-CHART-SEGMENT-VERSION 明細實作目的
- 使用者確認先完成分段明細查閱；新增唯讀API及管制圖入口，列出目前設定及既有歷史，缺版本證據明示未知。
- 先建立隔離／分頁／無寫入測試；不改計算、不新增有效期、不改DB或Portal／TransFiles；驗證後僅發布SPC測試站。

## [2026-10-20] - SPC-FORMULA-EFFECTIVE-TRACE 規劃目的
- 使用者指定只規劃生效日期、自動選版與留存；沿用既有公式版本，提出量測日期解析、有效期間關聯、F快照、追溯與跨入口契約。
- 僅spec／plan／tasks／verification及索引工作池同步；不改程式、DB、不執行migration或發布。

## [2026-10-20] - SPC-FORMULA-TRACE-TASK-001 基準實作目的
- 建立實際計算器／轉檔來源的無副作用相容性基準；使用者明確要求TransFiles仍需直接寫DB，現有GUI API路徑僅為現況，不作目標替代。
- 只改SPC測試／runner／SDD契約，不改應用程式、Portal／TransFiles來源、DB、權限、migration或發布。

## [2026-10-20] - SPC-FORMULA-TRACE-TASK-002 schema目的
- 先建關聯／不可變／回復測試，再建立公式有效期、receipt／trace及統計工作最小模型與migration；只在固定前綴隔離庫演練。
- 未授權共用測試／正式DB升級；不發布會在啟動自動Migrate的新DLL、不啟用模式、不修改其他系統。

## [2026-10-20] SPC-FORMULA-TRACE-TASK-003
- 目的：沿用公式歷史，建立台北日期排程、共用計算及過期預覽防護；核心先測試，不提早啟用寫入閘門。
- 範圍：SPC服務／API／UI及測試，測試站需備份驗證後發布；Portal／TransFiles整合留下一件，正式不操作。

## [2026-10-20] SPC-FORMULA-TRACE-TASK-004
- 目的：Portal沿用SPC日期計算，原子保存初驗／複驗receipt、量測及trace；核心先測試，既有匯入未啟用policy不受影響。
- 範圍：SPC交易／UploadService hook與Portal代理／表單；無新schema，測試站依順序發布，正式／TransFiles不操作。

## 2026-10-20 TASK-005目的
沿用既有公式憑證新增受控SQL直寫及統計consumer，讓TransFiles真正落資料庫；核心先建測試、隔離驗證後只發布測試，不操作KM或正式站。

## 2026-10-20 TASK-006目的
以已保存receipt／trace查閱實際版本，整合日期分段及逐點畫面；舊資料與子組缺證據明示未知。先建核心測試，唯讀查詢、不改計算或資料庫。
# 2026-10-21 SPC日期公式/管制圖示範資料
- 依使用者授權建立獨立測試資料；僅PMR_SPC_TEST，沿用引擎選版/計算/保存與圖表，不改核心程式、schema或正式環境。規格：specs/20261021-formula-chart-demo/spec.md。

## 2026-10-21 SEC-FOLLOWUP-006正式前端API拓撲相容
目的：正式SpcWeb無/api代理，需集中client明確支援既有正式API白名單；Test同源及舊環境URL防護保留。不安裝Runtime/IIS模組、不改權限或SSO key。先建測試，再做最小helper與client接線，依規範測試/測試站/正式驗證。

## 2026-10-22 PARTICLE-EMPTY-TASK-001
- 目的：測試Particle表／批次0，TransFiles落塵工作執行緒只匯出Excel，漏接既有預覽上傳。先補回歸再修最小分支，保留使用者確認，不改統計或資料庫。
## [2026-10-22] SPC-PARTICLE-EMPTY-TASK-002 UTC回傳修正目的
- 實站驗收發現SQL datetime2未帶Kind，Particle時間回傳缺Z，造成最新事件位置比較偏移。只在Particle輸出指定既存UTC語意，保留ticks、原始資料及統計公式；先建立SQL型態回歸測試，再發布測試API。

## 2026-10-22 SPC正式連線與API調整規劃
- 目的：唯讀核對正式連線/API版本並提出格式建議與小工作排序；不修改程式、DB、IIS或發布。

## 2026-10-22 PPC編輯保留選填線別
- 目的：修正非必填MachineId遭清空造成2620與2610錯誤衝突；先建回歸測試再修正，僅測試backend發布，不改正式資料/索引。

## 2026-10-22 常態分析固定AD規劃
- 目的：依使用者要求僅規劃移除偏態/峰態與固定Anderson-Darling，加入P2小工作；不修改程式/計算/DB或發布。

## 2026-10-23 常態分析固定AD實作
- 目的：依測試站發布授權，先建立預設AD/明確AJB及邊界回歸，再調整兩頁固定AD、移除偏態/峰態與方法下拉，保留API相容與原始數值。

## 2026-10-24 I18N-TASK-02
- 目的：先定義三站共用JSON/key/placeholder契約、詞彙初稿與缺漏檢查設計；只文件/示例，不改產品或DB、不發布、不接續下一Task。

## 2026-10-24 I18N-TASK-03
- 目的：僅規劃Portal本人語言偏好/API、跨站身分信任邊界與schema提案，不執行migration/端點/角色/SSO/發布，不接續下一工作。

## 2026-10-24 I18N-TASK-04 修改目的（實作前）
導入 SPC 共用介面三語試點與隔離本機偏好；先建核心測試，再修改 bootstrap/App。保留路由與業務狀態，不改 DB/API/權限或公式；驗證後只發布 SPC 測試前端。

## 2026-10-24 I18N-TASK-04 完成
共用Vue I18n三語試點與隔離本機偏好完成，26單元及已發布17+8瀏覽器驗收通過；測試Web0.1.72/API沿用0.1.72，無DB或正式更新。詳specs/20261024-i18n-task04/verification.md，STOP於TASK-04。

## 2026-10-24 I18N-TASK-05A 修改目的（實作前）
TASK-05依12頁/7919行盤點拆5批，本次只做產品料號/品質特性三語顯示。先建操作與DTO回歸，再改顯示及抽屜共用語言選單；不改模型/值/公式/API/DB/權限。通過後只發布SPC測試前端。

## 2026-10-24 I18N-TASK-05A 完成
產品料號/品質特性三語與抽屜復用LocalePicker完成，原模型/DTO/權限不變；30單元、本機39/發布39+17瀏覽器與build/同源/hash/health通過。測試Web0.1.73，API沿用0.1.72，無DB/正式更新；其餘05批次及TASK06～21共20小工作留待辦。

## 2026-10-24 I18N-TASK-05B 修改目的（實作前）
翻譯製程/機台/槽體、線別槽體、大類別公開分支、WE八規則四頁固定顯示。先建顯示helper與操作測試；保留API模型/代碼/JSON/規則/權限。types隱藏分支留05C。通過後只發布SPC測試前端。

05B範圍補充（修改前）：手機操作測試失敗，僅四頁套用既有校驗頁導覽上下堆疊與main min-width，解決操作區過窄；其他頁不改。

## 2026-10-24 I18N-TASK-05B 完成
四頁公開介面三語、UI訊息按顯示時解析與限定路徑手機導覽完成；37單元、本機79及已發布135瀏覽器驗證/build/同源/hash/health通過。測試Web0.1.74、API沿用0.1.72，無schema/引擎/正式變更。小分類及公式留05C，剩19小工作，STOP。

## 2026-10-24 I18N-TASK-05C1 目的（實作前）
將圖表小分類/公式設定固定介面補三語與模態內切換；保留公式原文、DTO、搜尋、固定WE規則及權限。05C拆C1/C2/C3，本次僅C1，測試前端發布，無DB/API/正式變更。

### 2026-10-24 I18N-TASK-05C1 完成
保留types分支補45鍵三語，公開入口仍隱藏；40單元、23隔離元件、本機98/發布98回歸與build通過。SpcSingleTest Web0.1.75/API0.1.72；無DB/API/正式變更；真人審稿留TASK20，下一項C2未開始。

## 2026-10-24 I18N-TASK-05C2 目的（實作前）
線別分析總覽/F表/公式歷程與共用版本日期元件固定介面三語；保留公式/JSON、匯出、query/DTO/日期與權限，無核心運算/後端/DB變更。必要時僅兩頁沿用既有手機堆疊導覽，完成後只發布SPC測試前端。

### 2026-10-24 I18N-TASK-05C2 完成
線別分析/F表/公式歷程及共用日期固定UI三語105鍵，原計算/匯出/query/DTO不變；兩手機路徑堆疊與F表容器橫捲。46單元/本機47＋17/舊歷史9/發布47＋17/build與實際hash/health通過；SpcSingleTest Web0.1.76/API0.1.72，無DB/正式，剩19件STOP於C2。

## 2026-10-24 I18N-TASK-05C3 目的（實作前）
管制項目主表單/分段歷史試算/規格歷史/公式排程固定介面三語；原識別、規格計算、JSON/DTO/日期/理由/清除防呆與權限保持。舊常態方法切換說明依固定AD基準校正；模態語言控制及必要手機排版，僅SPC測試前端發布。

### 2026-10-24 I18N-TASK-05C3 完成
管制項目/分段規格歷史試算/公式排程三語300鍵，原識別/JSON/DTO/日期/理由/清除防呆與權限維持；抽屜語言控制與本路徑手機堆疊。51單元、本機40＋17/舊歷史9/排程7/發布40＋17/build/hash/health通過。測試Web0.1.77/API0.1.72，無DB/正式，剩18件STOP於C3。

## 2026-10-24 I18N-TASK-05D 目的（實作前）
SPC既有量測補登/修改頁及工站綁定/Soft Hold固定UI三語，保留數值/ISO日期/DTO/Enter順序/綁定與卡控流程/權限；只必要手機排版與模態語言控制，不改Portal/DB/API，只發布SPC測試前端。

### 2026-10-24 I18N-TASK-05D 完成
SPC既有量測管理補登/修改頁與綁定/卡控固定UI三語80鍵，原數值/日期/DTO/Enter/卡控/權限保持；本路徑手機堆疊與圖示點擊修正。54單元/本機38＋17/發布38＋17/build/hash/health通過。測試Web0.1.78/API0.1.72，Portal/DB/正式未改；剩17件STOP於05D。
# 2026-10-25 I18N-TASK-05E 變更目的
- 依使用者「開始下一項」只處理匯入批次與預覽固定 UI 三語化；先記錄 specs/20261025-i18n-task05e 規格與 BDD。
- 保留 TransFiles 匯入分工、原始來源／錯誤／建議、API／主檔補建／權限／數值日期；不恢復網頁匯入，不改 DB/API/正式站。

### 2026-10-25 I18N-TASK-05E 完成
批次清單／預覽固定UI與既有主檔補建提示三語89鍵，原數值／日期／payload／query／API／權限不變；僅兩路徑沿用手機堆疊及按鈕換行。58單元、本機35＋17／發布35＋17／build／同源／hash／health通過。測試Web0.1.79/API0.1.72，無DB／後端／正式更新；draft真人審稿留TASK20，剩16件STOP於05E。
# 2026-10-26 I18N-TASK-06A 目的（實作前）
- 共用分段版本／已保存計算證據的固定UI三語化；不改API/query/分頁/過期回應保護/數值/公式/日期/計算/寫入/權限。
- 手機明細74px溢出紅燈：實作前記錄僅spc/trend-chart沿用既有stackedMobileNav，不修改圖表計算或資料。
- 保留原始JSON與證據、不推定舊版本；先記規格，僅發布SPC測試前端，06其餘三批留待辦。

### 2026-10-26 I18N-TASK-06A 完成
分段版本與已保存計算證據104鍵三語，原公式／數值／JSON／日期／GET分頁／過期回應保護與警語保持。手機兩圖表入口堆疊，62單元／本機29＋舊17＋shell17／發布29＋17及build/hash/同源/health通過；測試Web0.1.80/API0.1.72，正式未改。06拆四批，剩18件，STOP於06A。
# 2026-10-26 I18N-TASK-06B1 目的（實作前）
- 只翻譯管制圖頁首／查詢／週月報控制／監控摘要／下載與排除清單固定UI及相關讀取錯誤；不改query/數值/CSV/計算/日期限制/群組與主檔值/寫入/權限。
- 手機週月報日期44px紅燈，修改前記錄僅同元件週月報／能力／異常分析三入口沿用既有stackedMobileNav。
- 手機截圖英文週期選項截字，改查詢控制全寬／按鈕另列，保留sm以上原排列；不改查詢參數。
- B2圖內及總覽表、B3點位寫入表單另列，不標全頁完成；先規格、必要驗證後只發布測試前端。

### 2026-10-26 I18N-TASK-06B1 完成
查詢／週月報／監控摘要與下載排除清單70鍵三語；原query／數值／日期／CSV／canvas／恢復DELETE及權限保持。手機三別名堆疊、欄位全寬。65單元／本機38＋分段17＋shell17／發布38＋17及build/hash/同源/health通過；測試Web0.1.81/API0.1.72，正式未改。B拆三批後剩19件，STOP於B1。
# 2026-10-26 I18N-TASK-06B2 實作前目的
- 依連續授權翻譯圖表presentation／直方圖固定UI與總覽表；只patch文字，不重建、不改series/dataZoom/排序值/數值/公式/日期/API。
- tooltip保留原參數純文字編碼，真人審核不能由AI代替，後續依工作池逐件接續。

### 2026-10-26 I18N-TASK-06B2 完成
圖表／直方圖與總覽99鍵三語，保留series／zoom／排序及數值；67 i18n＋4 presentation單元與必要瀏覽器／build／測試發布驗證通過；測試Web0.1.82/API0.1.72，正式未改，剩18件，直接接續B3。
# 2026-10-26 I18N-TASK-06B3 實作前目的
- 三語化點位右鍵／備註視窗／異常列表與OCAP固定UI及alert；只改選項顯示，原中文enum value、狀態、DTO／端點／reason／數值日期與草稿不變。
- 備註視窗沿用既有LocalePicker；必要驗證後只發布測試前端，再依授權接續06C。

### 2026-10-26 I18N-TASK-06B3 完成
點位備註／排除與OCAP98鍵三語，原中文value／DTO／reason／0／日期／草稿保持；70單元與必要瀏覽器／build／測試發布驗證通過，Web0.1.83/API0.1.72，正式未改。剩17件，接續06C。
# 2026-10-26 I18N-TASK-06C 實作前目的
- 趨勢圖完整固定UI／查詢／抽屜／備註及圖內presentation三語；共用既有chart字典factory，只patch文字，原統計／數值／series／zoom／日期／query／寫入DTO／reason及草稿不變。
- 依連續授權必要驗證後發布測試前端並接續06D，無正式／DB／其他系統變更。

### 2026-10-26 I18N-TASK-06C 完成
趨勢圖54鍵三語、共享chart factory與統計卡穩定key，原數列／query／日期／固定AD保持。必要驗證與測試Web0.1.84發布完成，API0.1.72不變，剩16件接續06D。

### 2026-10-26 I18N-TASK-06D 目的
警示總覽與處置固定顯示三語化，保持原權限、狀態、DTO、模擬預填值與送信行為；只測試前端，先規格後實作。

### 2026-10-26 I18N-TASK-06D 完成
警示總覽／處置117鍵三語，原狀態／DTO／Closed後ACK、郵件參數與草稿保持；75單元、本機48／發布48及build/hash/health/同源通過。只測試Web0.1.85，剩15件接續07。

### 2026-10-26 I18N-TASK-07A 目的
設備狀態／點位／監控固定顯示三語，保留timer／query／DTO／enum與raw資料；依模組拆分07，每批驗證發布提交後續行。

### 2026-10-26 I18N-TASK-07A 完成
設備狀態／點位／監控73鍵三語，原timer／query／DTO／enum／raw保持；78單元、本機41／發布41、build/hash/health/同源通過，測試Web0.1.86/API0.1.72。剩18件接續07B。

### 2026-10-26 I18N-TASK-07B 目的
粒子監控固定UI與圖表標籤三語，保留C/U、數列、queryRange、原始資料及統計判定，僅測試前端。

### 2026-10-26 I18N-TASK-07B 完成
粒子監控28鍵三語，原C/U／數列／queryRange／資料保持；81單元、本機25／發布25及build/hash/health/同源通過，只測試Web0.1.87/API0.1.72，剩17件接續07C。

### 2026-10-26 I18N-TASK-07C 目的
儀器校驗與Excel匯入固定UI三語，保留日期判定／版本／DTO／FormData／狀態value／權限與原資料；僅測試前端，業務API全攔截驗證。

### 2026-10-26 I18N-TASK-07C 完成
校驗／匯入199鍵三語，日期／版本／DTO／FormData與原資料保持；86單元、本機45＋原回歸14／發布45及build/hash/health/同源通過，只測試Web0.1.88/API0.1.72。剩16件接續07D。

### 2026-10-26 I18N-TASK-07D 目的
SMTP／報表設定／使用者固定顯示三語，保留原角色權限、DTO、password處理、收件ID與排程；只測試前端，不真寄信或寫入。

### 2026-10-26 I18N-TASK-07D 完成
SMTP／報表／使用者173鍵三語，原角色權限／DTO／收件ID／排程與raw保持；91單元、本機51／發布51及build/hash/health/同源通過，只測試Web0.1.89/API0.1.72。剩15件接續07E。

### 2026-10-26 I18N-TASK-07E1 目的
公開查詢／追溯／咬蝕入口固定顯示三語，保留端點／query／code／raw資料與樹狀圖。07E拆公開業務頁與手冊/SSO兩批，未接路由舊檔不新增入口。

### 2026-10-26 I18N-TASK-07E1 完成
公開查詢／系譜／咬蝕入口94鍵三語，原query／code／series／raw與連結保持；95單元、本機40／發布40及build/hash/health/同源通過，只測試Web0.1.90/API0.1.72。剩15件接續E2。

### 2026-10-26 I18N-TASK-07E2 目的
操作手冊與SSO固定顯示三語；保留token／claims／hash／redirect／登入協定原碼，僅固定訊息與原handler語言選單，無Portal／AD操作。

### 2026-10-26 I18N-TASK-07E2 完成
手冊／SSO固定顯示127鍵三語，原token／claims／hash／redirect保持；97單元、本機41／最終發布41及build/hash/health/同源通過，只測試Web0.1.91/API0.1.72。剩14件接續08。

### 2026-10-26 I18N-TASK-08A 目的
先建核心測試，再新增API共用三語資源與相容messageKey/args、標準model validation附加契約與前端介接；保留HTTP／舊message／數字日期文化／授權與DB，僅測試前後端發布。

### 2026-10-26 I18N-TASK-08A 完成
12鍵單一來源API三語模組、相容metadata與顯示adapter；101前端／25後端、本機12／shell17／發布12與build/hash/health/同源通過。只測試Web0.1.92/API0.1.73，無schema修改，剩17件接續08B。

### 2026-10-26 I18N-TASK-08B 目的（實作前）
僅Auth失敗與Viewer guard安全提示附加三語descriptor，先測既有拒絕／成功與資料結果；簽章／nonce／claims／權限分支與HTTP不改，無schema。

### 2026-10-26 I18N-TASK-08B 完成
身分與Viewer安全提示13鍵三語；102前端、31 API／12隔離身分、本機15／發布15與build/hash/health/同源通過。只測試Web0.1.93/API0.1.74，剩16件接續08C。

### 2026-10-26 I18N-TASK-08C1 目的（實作前）
管制項目驗證改typed descriptor，原分支／繁中訊息與HTTP不變；先測LSL0.2編輯排除自己與真正重複、群組名稱純文字；不改schema／公式／歷史。

### 2026-10-26 I18N-TASK-08C1 完成
26鍵三語typed驗證、103前端／26後端、本機18／發布18與build/hash/health/同源通過；LSL0.2排除自己與真正重複拒絕資料結果回歸通過。只測試Web0.1.94/API0.1.75，剩19件接續C2。

### 2026-10-26 I18N-TASK-08C2 目的（實作前）
其餘主檔錯誤三語；物件增量descriptor、原字串body保持並用明示白名單header提供顯示key，不改業務分支／HTTP／usage／原資料；先測兼容與安全fallback。

### 2026-10-26 I18N-TASK-08C2 完成
2026-10-26 08C2完成：24鍵／105前端、30後端、本機24／發布24及build/hash/health/同源通過，原string body與text/plain相容、舊關聯清單保持；只測試Web0.1.95/API0.1.76，剩18件，接續08C3 公式／圖表與查詢。

### 2026-10-26 I18N-TASK-08C3A 目的（實作前）
公式／圖表／查詢控制器固定回應附加metadata，原body/status/data與計算／寫入／日期驗證保持；服務例外另C3B，核心先測。

### 2026-10-26 I18N-TASK-08C3A 完成
2026-10-26 08C3A完成：60鍵／106前端、34後端、本機27／發布27及build/hash/health/同源通過，11控制器除顯示metadata外與原文完全相同；只測試Web0.1.96/API0.1.77，剩18件，接續08C3B 服務例外與動態驗證。

### 2026-10-26 I18N-TASK-08C3B 目的（實作前）
既有服務例外附加明示descriptor，保留原型別／Message／Inner與分支；controller沿原catch回應，未知fallback與global安全策略不變；動態舊string header受限UTF8 args，先測再改。

### 2026-10-26 I18N-TASK-08C3B 完成
2026-10-26 08C3B完成：79鍵／108前端、64必要後端、公式基準15與TransFiles來源特徵10、本機33／發布33及build/hash/health/同源通過，9服務除例外metadata外全文不變；只測試Web0.1.97/API0.1.78，剩17件，接續08C4 匯入／設備／校驗／設定驗證。

### 2026-10-26 I18N-TASK-08C4A 目的（實作前）
剩餘控制器固定提示三語，原code/message/error/body及HTTP不變，機敏內容不放args；僅隔離驗證，不呼叫真郵件／Webhook／目錄或清除資料。

### 2026-10-26 I18N-TASK-08C4A 完成
2026-10-26 08C4A完成：86鍵／109前端、14本批API測試、本機36／發布36與build/hash/health/同源通過；校驗完整124項為122通過、2項本批前基準同樣失敗，未宣稱全綠；只測試Web0.1.98/API0.1.79，剩18件，接續08C4B 服務與欄位驗證。

### 2026-10-26 I18N-TASK-08C4B 目的（實作前）
校驗/匯入公開驗證例外明示metadata，原Status/Code/Message與分支不變；設備不可用訊息record增量key，不改cache/HTTP/權限。逐列錯誤另C4C，既有兩項校驗差異保留。

### 2026-10-26 I18N-TASK-08C4B 完成
2026-10-26 08C4B完成：55鍵／110前端、21必要API與設備來源3、本機39／發布39與build/hash/health/同源通過；校驗124為122通過、同2項既有差異保留；只測試Web0.1.99/API0.1.80，剩17件，接續08C4C 匯入逐列錯誤與警示。

### 2026-10-26 I18N-TASK-08C4C 目的（實作前）
匯入逐列errors/warnings保留原陣列與DB code/message，只在read DTO／明示建立點補索引、三語摘要／建議與安全父子Details；先測原資料／索引／限制，無schema或payload／計算變更。

### 2026-10-26 I18N-TASK-08C4C 完成
2026-10-26 08C4C完成：67鍵／112前端、24核心與6隔離HTTP、本機45／發布45與build/hash/health/同源通過；DB錯誤／payload與稽核欄位保持，校驗125為123通過／同2既有差異；只測試Web0.1.100/API0.1.81，剩16件，接續08C5 逐頁即時錯誤descriptor接線。

### 2026-10-26 I18N-TASK-08C5A 目的
已知API錯誤改存受驗證描述，切語言即時重繪；四頁先接線，保留原字串介面／原文fallback，不改業務與授權。核心先測，只測試發布。

### 2026-10-26 I18N-TASK-08C5A 完成
2026-10-26 08C5A完成：115前端、49錯誤切換／17共用／39主檔／56製程群組回歸、發布49與build/hash/health/同源通過；已存錯誤無新請求即時切語言；只測試Web0.1.101/API0.1.81，剩16件，接續08C5B 其餘頁面與巢狀DTO接線。

### 2026-10-26 I18N-TASK-08C5B 目的
其餘公開頁面的已存API錯誤及設備／匯入明細接三語描述，只讀metadata，保留原文與資料／DTO／原生alert；先測索引與fallback再實作，僅測試發布。

### 2026-10-26 I18N-TASK-08C5B 完成
2026-10-26 08C5B完成：117前端、31即時錯誤／DTO、49相容API與17共用、193操作回歸通過；發布31與build/hash/health/同源通過；只測試Web0.1.102/API0.1.81，剩15件，接續08D 郵件與背景通知。

### 2026-10-26 I18N-TASK-08DA 目的
郵件內容以明示request語言呈現，背景預設繁中；先建純builder與fake投遞測試，不改SMTP／收件人／outbox檔名與告警資料。只測試發布。

### 2026-10-26 I18N-TASK-08DA 完成
2026-10-26 08DA完成：27鍵／118前端、31必要API加nullable郵件補驗10、藥液59及校驗郵件5、12郵件版面、SMTP3／發布32與build/hash/health/同源通過；只測試Web0.1.103/API0.1.82，剩15件，接續08DB 校驗Email／Chat與背景通知。

### 2026-10-26 I18N-TASK-08DB 目的
校驗手動通知內容以request語言呈現，背景固定繁中；純builder先測，只替換顯示內容，不更動資格／租約／冪等／retry／Unknown及通知紀錄。無真寄送，僅測試發布。

### 2026-10-26 I18N-TASK-08DB 完成
2026-10-26 08DB完成：12鍵／119前端、51校驗通知與16共用API、12郵件版面、發布32與build/hash/health/同源通過；背景狀態／重試／原DTO保持；只測試Web0.1.104/API0.1.83，剩14件，接續08E 人讀報表與匯出。

### 2026-10-26 I18N-TASK-08C6 目的
補試算／排程／常態與子組固定說明的明示三語metadata；核心先測，保留原文／算法／數列及既有AD需求，不依譯文判斷業務。只測試發布。

- 08C6驗收補充：手機英文直方圖tooltip越界，限制常態說明區寬260px並換行；僅顯示樣式，API84不變，前端106補發。

### 2026-10-26 I18N-TASK-08C6 完成
2026-10-26 08C6完成：10鍵／120前端、47不同必要API分批、藥液59與跨專案公式25、本機8圖表／42PPC／6邊界及發布8圖表／2PPC通過；5檔原公式SQL逐字保持；只測試Web0.1.106/API0.1.84，剩14件，接續08E 人讀報表與匯出。
