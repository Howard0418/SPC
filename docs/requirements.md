# SPC 現行需求索引

- 後續安全/正式更新：[主規格](../specs/20261020-security-production-followups/spec.md)；001唯讀盤點及[維護決策](../specs/20261020-security-production-followups/verification.md)完成，剩002～006共5件。KM主機有WithSecure服務運行、Defender停止；未啟用防毒/安裝Runtime或發布站台。


- 日期公式示範資料：[PPC2636操作/驗證](../specs/20261021-formula-chart-demo/verification.md)。2026-10-21按使用者授權建立測試專用12日/兩公式/兩分段/一異常，原始值與初複驗追溯由現有服務保存，不修改業務PPC或正式環境。驗收後由使用者依專用ID範圍處理，永久清除追溯需受控方案。

- 藥液公式日期選版與追溯：[主規格v6](../specs/20261020-formula-effective-trace-plan/spec.md)；TASK-001～006完成。[Portal日期預覽及原子留存](../specs/20261020-formula-trace-task004/spec.md)支援初複驗、日期失效、冪等／全批交易與raw規格判定；[驗證](../specs/20261020-formula-trace-task004/verification.md)及[Antigravity案例](../specs/20261020-formula-trace-task004/antigravity-tests.md)。測試SPC API0.1.67／Web0.1.69、Portal API1.0.183／Web1.0.226，既有設定及資料不變，policy未啟用。[TASK-005真正DB直寫／consumer](../specs/20261020-formula-trace-task005/spec.md)及本人低權限連線已完成；[TASK-006分段／逐點實際版本](../specs/20261020-formula-trace-task006/spec.md)已完成，提供[Antigravity案例](../specs/20261020-formula-trace-task006/antigravity-tests.md)；真人案例待驗，正式未更新。

- 管制圖日期分段版本明細：[SPC-CHART-SEGMENT-VERSION-20261020](../specs/20261020-chart-segment-version-detail/spec.md)；2026-10-20依使用者選擇完成v2唯讀查閱，管制圖／藥液趨勢圖可查看目前分段與公式修改歷史，無證據明示未知。測試API0.1.63／Web0.1.67已發布，正式未更新；[Antigravity案例](../specs/20261020-chart-segment-version-detail/antigravity-tests.md)待真人操作。公式排程及自動選版契約已由TASK-003完成；Portal留存已由TASK-004整合；TransFiles與實際版本圖表已由TASK-005／006整合；舊量測仍明示未知，獨立規格／管制界限版本不在本批範圍。

- Portal 咬蝕預覽安全診斷轉送：[SPC-ETCH-PREVIEW-500-PORTAL-FORWARDING-20261015](../specs/20261015-etch-preview-500-portal-forwarding/spec.md)；保留 SPC HTTP status、錯誤代碼與 Trace ID，TransFiles 提示與日誌可顯示安全診斷。2026-10-19 TransFiles 離線錯誤路徑測試 4 passed、Portal 回歸 5 passed；測試 API 沿用 2026-10-15 發布，未重新發布或操作正式站。未執行打包 EXE 人工故障驗收，詳見驗證紀錄。

- SPC 咬蝕量預覽 HTTP 500 根因診斷：[SPC-ETCH-PREVIEW-500-SERVER-DIAGNOSTIC-20261015](../specs/20261015-etch-preview-500-server-diagnostic/spec.md)；咬蝕量預覽不使用確認匯入專用 SQL 交易鎖，且在 `Auth:Enabled=false` 但 JWT 金鑰存在時仍須註冊 JWT 驗證，避免 `[Authorize]` 端點回 500。測試站 backend 已發布，未授權 preview 已驗證回 401；2026-10-19 已完成兩站預覽 130/130、測試庫匯入 130 SUCCESS（125 unchanged、5 新增/補同步）及新增日期 PT1/QE2 Xbar-S 製圖驗證；正式站未發布。

- SPC 咬蝕量預覽 500 診斷修正：[SPC-ETCH-PREVIEW-500-DIAGNOSTICS-20261014](../specs/20261014-etch-preview-500-diagnostics/spec.md)；TransFiles 透過 Portal 呼叫 SPC 咬蝕量預覽時，未知/停用線別與既有資料缺批次等可預期問題需回可讀 409，不得只顯示 HTTP 500。已發布 SPC 測試站 backend，正式站未發布。
- SPC N1/N2 藥液開收線顯示修正：[SPC-CHEM-N1N2-CLOSE-LABEL-20261014](../specs/20261014-chemical-n1n2-close-label/spec.md)；N1/N2 藥液管制圖 tooltip、X 軸與點位明細需以短標籤顯示：早班開線=`早(開)`、早班收線=`早(收)`、中班開線=`中(開)`、中班收線=`中(收)`，不得將 CLOSE 歸成「晚班」；一般線別舊班別相容仍保留 `CLOSE=晚班`。已發布 SPC 測試站 frontend，正式站未發布。
- SPC 匯入入口調整為 TransFiles 直接匯入：[SPC-IMPORT-VIA-TRANSFILES-ONLY-20261014](../specs/20261014-spc-import-via-transfiles-only-plan/spec.md)；SPC 網站不再作為 Excel 人工匯入入口，匯入改由 TransFiles 轉檔工具直接寫入資料庫；SPC Web 保留匯入批次查詢、錯誤追蹤、新資料提示、主檔維護與製圖確認。已完成契約文件、`GET /api/uploads`、`GET /api/uploads/recent-imports`、批次查詢頁、首頁新資料提示，並將批次預覽頁收斂為唯讀追蹤，不再提供網頁確認匯入、放棄暫存或重複資料覆蓋選項；已發布 SPC 測試站 backend/frontend，正式站未發布。
- SPC 10 月藥液匯入失敗分析與預檢報告：[SPC-CHEM-OCTOBER-IMPORT-FAILURE-20261014](../specs/20261014-chemical-october-import-failure-plan/spec.md)；指定檔案 `docs/SPC文件/批次轉換結果_藥液匯入檔_2026-10.xlsx` 本體欄位完整且 676 筆必填欄位無空白，初步判斷較可能為主檔對應、單位不一致、槽位名稱、重複上傳或日期時間解析口徑造成。`SPC-CHEM-OCT-IMPORT-TASK-001` 已完成，匯入預覽會顯示來源檔、工作表、Excel 列號、錯誤欄位、錯誤代碼與修正建議；已發布 SPC 測試站 backend/frontend，正式站未發布。
- SPC 管制線計算公式文案釐清：[SPC-CONTROL-LINE-FORMULA-LABEL-20261014](../specs/20261014-control-line-formula-label/spec.md)；SPC 管制項目編輯頁進階設定公式區須顯示為「管制線計算公式」，說明其影響 UCL／CL／LCL，並明確不同於常態檢定方法。已發布 SPC 測試站 frontend，正式站未發布。
- SPC 管制項目編輯畫面直覺化：[SPC-PPC-EDIT-UI-SIMPLIFICATION-20261014](../specs/20261014-ppc-edit-ui-simplification/spec.md)；新增／編輯抽屜改為固定分頁，基本資料、規格與抽樣、圖表設定、藥液公式、進階設定分區操作；顯示方式與管制圖類型各只保留一處，畫面文字統一使用「槽位」。已發布 SPC 測試站 frontend，正式站未發布。
- SPC 常態檢定方法切換：[SPC-NORMALITY-METHOD-SWITCH-20261014](../specs/20261014-normality-method-switch/spec.md)；SPC 管制圖與量測趨勢圖可選 Adjusted Jarque-Bera 或 Anderson-Darling 常態檢定，後端 `/api/v1/spc/chart` 以 `normalityMethod` 參數切換，未指定時維持 Adjusted Jarque-Bera。正式站未發布。
- SPC 槽位顯示一致：[SPC-TANK-SLOT-DISPLAY-20261014](../specs/20261014-spc-tank-slot-display/spec.md)；SPC 管制項目總覽與管制圖監控明細都顯示「槽位」，且需保留完整槽位名稱，例如 `蝕刻(鎳)`，不得切成 `蝕刻`。正式站未發布。
- SPC/Portal/KM Web API 單一網址設定：[UNIFIED-WEB-API-ENDPOINTS-20261013](../specs/20261013-unified-web-api-endpoints/spec.md)；對使用者、瀏覽器與跨系統 SSO 設定一律使用站台根網址，API 掛同站 `/api/...`。SPC 測試站為 `https://SPCTEST.pmr.com.tw/` 與 `/api/...`；Portal 測試站為 `https://portaltest.pmr.com.tw/` 與 `/api/...`，內部 `PmrPortalApi` 僅作 Web proxy upstream；KM 測試站為 `https://kmtest.pmr.com.tw:8086/`，若未來新增 API 亦掛同站 `/api/...`。正式站未發布。
- SPC 直方圖 scale/bin 改善：[SPC-RAWDATA-NORMALITY-ISSUES-20261012](../specs/20261012-spc-rawdata-normality-issues-plan/spec.md)；`SPC-HISTOGRAM-1012-TASK-001` 已完成，管制圖與趨勢圖直方圖 bins 改以未排除量測值範圍建立，遠端規格/管制線不再壓縮主要分布；常態分布曲線縮放同步改用相同 bin width。已發布 SPC 測試站 backend/frontend，正式站未發布。
- SPC Raw Data 規格資訊補欄：[SPC-RAWDATA-NORMALITY-ISSUES-20261012](../specs/20261012-spc-rawdata-normality-issues-plan/spec.md)；`SPC-RAWDATA-1012-TASK-003` 已完成，raw data CSV 於 `Value` 後補上 `SpecLSL`、`SpecTarget`、`SpecUSL`、`Unit`，讓下載內容可直接對照量測值與規格資訊。已發布 SPC 測試站 backend，正式站未發布。
- SPC Raw Data `Value` 日期誤判與項目資料正確性修正：[SPC-RAWDATA-NORMALITY-ISSUES-20261012](../specs/20261012-spc-rawdata-normality-issues-plan/spec.md)；`SPC-RAWDATA-1012-TASK-002` 已完成，後端 raw data CSV 組裝抽出 `RawDataCsvExporter`，日期欄位才套用日期格式化，`Value` 欄保留原始 invariant 字串，並過濾其他 `PartProcessCharacteristicId` 的列。已發布 SPC 測試站 backend，正式站未發布。
- SPC Raw Data 下載上下文修正：[SPC-RAWDATA-NORMALITY-ISSUES-20261012](../specs/20261012-spc-rawdata-normality-issues-plan/spec.md)；`SPC-RAWDATA-1012-TASK-001` 已完成，前端下載 raw data 時會依序解析 `ppcId`、總表列 `partProcessCharacteristicId`、舊欄位 `ppcId` 與 selected mapping，修正 All 總表 drilldown 後無法取得單一管制項目 ID 的問題。已發布 SPC 測試站 frontend，正式站未發布。
- SPC 常態檢定方法差異揭露：[SPC-RAWDATA-NORMALITY-ISSUES-20261012](../specs/20261012-spc-rawdata-normality-issues-plan/spec.md)；`SPC-NORMALITY-1012-TASK-001` 已完成，管制圖與趨勢圖會揭露 SPC 採 Adjusted Jarque-Bera 偏態/峰態檢定，Minitab 常用 Anderson-Darling，兩者 p-value 與判定不可直接等同。已發布 SPC 測試站 backend/frontend，正式站未發布。
- 咬蝕量 X- 線別不匯入 SPC 已完成：[SPC-ETCH-X-LINE-PREFIX-20261012](../specs/20261012-etch-x-line-prefix-requirement/spec.md)；TransFiles 解析咬蝕日報時讀取各區塊第 2 列原始線別標題，`X-` 前綴線別加入 `excluded` 與 Excel「排除清單」，不產生 SPC `reports`；未帶 `X-` 線別維持既有匯入流程。已完成測試與真實來源唯讀抽取驗證；未打包 EXE、未連線 Portal/SPC、未寫入資料，SPC 測試站發布不適用，正式站未發布。
- 小工作池已新增：[SPC Raw Data 與常態檢定異常規劃](../specs/20261012-spc-rawdata-normality-issues-plan/spec.md)；依 `docs/SPC_RawData 異常說明-1012.xlsx` 分析，待處理問題包含 All 總表後 raw data 無法下載、RawData `Value` 日期誤判、下載內容缺規格資訊、常態檢定與 Minitab 差異，以及小變異資料直方圖 scale/bin 不易判讀。本次只規劃並加入小工作，未修改程式、未建置、未發布。
- 大型模組拆分與架構治理：[ARCH-TASK-010-LARGE-MODULE-GOVERNANCE-20261012](../specs/20261012-architecture-task010-large-module-governance/spec.md)；SPC 新增 `tools/architecture/Get-LargeModules.ps1` 與 `docs/architecture-governance.md`，固定大型模組盤點門檻、拆分候選、責任邊界、拆分順序與最低驗證要求。門檻 700 可抓出既有超標模組並回傳非 0；門檻 10000 回傳 0。本次只建立治理基線，不修改應用程式、不發布。
- 測試與可重建基線：[ARCH-TASK-009-REPRODUCIBLE-TEST-BASELINE-20261012](../specs/20261012-architecture-task009-reproducible-test-baseline/spec.md)；SPC 新增 `tools/testing/Invoke-SpcBaseline.ps1`，依序執行後端測試、後端 build、前端 build 與可選 smoke URL，支援 `-TestFilter`、`-SkipTests`、`-SkipFrontend` 與 `-SmokeUrl`，避免小工作驗證時平行 build/test 搶 DLL 鎖並統一驗證入口。本次不修改應用程式、不發布、不清資料。
- 稽核與操作者追溯：[ARCH-TASK-008-AUDIT-OPERATOR-TRACEABILITY-20261012](../specs/20261012-architecture-task008-audit-operator-traceability/spec.md)；SPC 後端新增共用 `AuditActorResolver`，統一依 `Identity.Name`、`ClaimTypes.Name`、`name`、`preferred_username`、`username`、`NameIdentifier`、`sub` 解析操作者。SPC 點位備註/排除寫入 `UpdatedBy` 時改用共用解析規則；本次不新增資料表、不修改 JWT 發行或登入流程。
- SMTP、檔案與背景排程可靠性：[ARCH-TASK-007-SMTP-FILE-SCHEDULER-RELIABILITY-20261012](../specs/20261012-architecture-task007-smtp-file-scheduler-reliability/spec.md)；SPC 項目管制總覽週報/月報背景排程已抽出 `SpcReportScheduleProcessor`，寄送報表時需至少一位收件者成功才更新 LastSent；若全部 SMTP 寄送失敗，LastSent 保持不變以便後續排程重試。不新增資料表、不變更 SMTP 設定 UI。
- 發布流程與環境隔離：[ARCH-TASK-006-RELEASE-ENVIRONMENT-ISOLATION-20261012](../specs/20261012-architecture-task006-release-environment-isolation/spec.md)；SPC release 工具已將正式環境名稱與 `D:\Sites\PmrPortal` 路徑阻擋抽成共用 `ReleaseTargetGuard.psm1`，`ReleaseApplyPackage.ps1` 與 `ReleaseBackupRestore.ps1` 共用同一份安全邊界，並新增 PowerShell 自動化測試。本次未操作正式站、未發布、未刪除資料。
- 資料庫 migration 與啟動流程：[ARCH-TASK-005-DATABASE-STARTUP-FLOW-20261011](../specs/20261011-architecture-task005-database-startup-flow/spec.md)；SPC API 已將資料庫 provider 正規化與啟動動作決策抽出為 `DatabaseStartupPlanner`，明確區分 SQLite `EnsureCreated` 與 SQL Server baseline + EF migration，降低日後整理相容性 SQL 與發布時誤判 provider 的風險。本次不新增 migration、不改相容性 SQL、不操作正式資料庫。已發布 SPC 測試站 backend，正式站未發布。
- 後端授權模型收斂：[ARCH-TASK-004-BACKEND-AUTHORIZATION-CONSOLIDATION-20261011](../specs/20261011-architecture-task004-backend-authorization-consolidation/spec.md)；SPC backend 已新增共用授權存取服務，校正管理與儀器匯入改由同一服務解析登入者角色、頁面權限與操作者狀態，降低 Viewer/Editor 與 `calibration.manage` 判斷不一致風險。不改登入流程、JWT 格式或資料庫 schema。已發布 SPC 測試站 backend，正式站未發布。
- 小工作完成後剩餘工作提醒：[COMPLETION-REMAINING-WORK-REMINDER-20261011](../specs/20261011-completion-remaining-work-reminder/spec.md)；每執行完一個小工作後，最終回覆須依 `TODO.md` 提醒目前仍有多少未完成小工作；暫緩或外部阻擋項目可分開簡述。本次僅文件規則變更，未修改程式、未發布。
- MES Sync 資料可靠性：[ARCH-TASK-002-DIAGNOSTICS-MES-SYNC-RELIABILITY-20261010](../specs/20261010-architecture-task002-mes-sync-reliability/spec.md)；SPC API 背景服務不再將無效 JSON 或未支援 MessageType 的 MES Sync 訊息假標 `Processed`，改標 `Failed` 並留下錯誤，批次處理邏輯已抽出可單元測試。已發布 SPC 測試站 backend，正式站未發布。
- 診斷端點與錯誤資訊隔離：[ARCH-TASK-003-DIAGNOSTICS-ERROR-ISOLATION-20261010](../specs/20261010-architecture-task003-diagnostics-error-isolation/spec.md)；SPC API 非 Development 未處理例外不再回傳 exception message/detail，Development 保留診斷資訊，新增匿名 `/api/health` 最小健康檢查。已發布 SPC 測試站 backend，正式站未發布。
- SPC 單一 IIS Site：[20261005-single-iis-site](../specs/20261005-single-iis-site/spec.md)；T-001～T-008 已完成，`SpcSingleTest` 提供 `http://172.16.110.27:8084` 與 `https://172.16.110.27:8901`。2026-10-12 已重建並刷新測試前端，Portal 測試站 SSO 回跳改至 `https://172.16.110.27:8901/portal-sso`；HTTP/HTTPS 首頁、子路由、API version、mixed content、登入、Portal SSO、JWT、未登入 401 與 Viewer 403 smoke 通過。正式站未發布。
- 工作池已完成項目整理：[TODO-COMPLETED-ITEMS-CLEANUP-20261010](../specs/20261010-todo-completed-items-cleanup/spec.md)；`TODO.md` 未完成排序區已移除 SPC 管制圖點位備註、Portal 生日通知三個已完成 Task 與團保取消項目，完成狀態改由需求索引、CHANGELOG 與對應 specs 追溯。本次只整理文件，未修改功能、未發布。
- 正式機更新工具使用手冊與授權檢核：[RELEASE-TOOL-TASK-005](../specs/20261010-release-tool-task005/spec.md)；已建立正式機更新手冊 `docs/release-tools/production-release-runbook.md`，涵蓋授權清單、參數範本、前置檢查、操作順序、rollback、evidence 格式與停止條件。本次僅文件變更，正式站操作仍需另行授權，未連線正式機、未發布。
- Portal 測試生日祝賀按鍵：[PORTAL-BIRTHDAY-PREVIEW-BUTTON-20261012](../specs/20261012-portal-birthday-preview-button-plan/spec.md)；`PORTAL-BIRTHDAY-REPLAN-TASK-004` 已於 PmrPortal 完成。人事生日通知設定頁新增測試按鍵，點擊後直接顯示購物網站廣告式生日賀卡彈窗並可關閉；目前賀卡背景透明、無外框/圖片陰影、右上角浮動關閉按鍵、圖片置中放大。預覽不寫入已讀狀態、不修改生日資料或正式生日觸發規則。圖片上傳已支援常見 Content-Type 變體，預覽圖片會使用 Portal API 完整圖片網址；240 字祝詞可正常儲存；可選「只顯示圖片」或「顯示賀詞 + 圖片」。已發布 Portal 測試站 API/Web，正式站未發布。
- SPC 線別分析項目總覽公式版本記錄補強：[SPC-CHEM-OVERVIEW-FORMULA-VERSION-20261007](../specs/20261007-chemical-overview-formula-versioning/spec.md)；線別分析項目總覽修改藥液分析公式時，必須與 SPC 管制項目設定頁共用同一個 `ChemicalAnalysisFormulaVersion` 版本記錄與回復功能，並保存每筆版本參照的文件/修改依據備註。已實作、發布 SPC 測試站並於 2026-10-10 補驗 HTTP smoke 通過；正式站未發布。
- SPC 管制圖量測點備註：[SPC-CHART-POINT-REMARKS-20261007](../specs/20261007-spc-chart-point-remarks/spec.md)；SPC 管制圖與趨勢圖每個可定位量測點可由右鍵新增/編輯/清空備註，備註以獨立 `SpcPointRemarks` 軟停用資料表保存並可重新查詢顯示，不影響管制界線、OOC/OOS、Cpk 或既有點位排除/隱藏狀態。已發布 SPC 測試站，正式站未發布。
- SPC F 表版本記錄與管制界線重算：[SPC-FTABLE-CL-REPLAN-20261007](../specs/20261007-spc-ftable-control-limit-replan/spec.md)；標準差方法切換後重算管制界線已完成後端防護。F 表版本記錄/回復已新增歷程資料表、查詢/回復 API 與維護頁版本記錄。
- Portal 生日通知調整與團保取消規劃：[PORTAL-BIRTHDAY-REPLAN-20261007](../specs/20261007-portal-birthday-replan/spec.md)；依使用者新指示，團保專區取消，生日資料維護不再獨立成區。`PORTAL-BIRTHDAY-REPLAN-TASK-001` 已於 Portal 權限管理補回出生年月日欄位並發布測試站；`PORTAL-BIRTHDAY-REPLAN-TASK-002` 已新增人事專區生日祝詞與祝賀圖片設定頁並發布測試站；`PORTAL-BIRTHDAY-REPLAN-TASK-003` 已完成登入生日通知套用新版設定並發布測試站。
- 工作池狀態同步：[PORTAL-TASK-003 公告主旨自動帶組織單位](../specs/20261007-portal-task003-sync/spec.md)；核對 PmrPortal 已有完成規格與測試站 API 發布紀錄後，已自 `TODO.md` 未完成工作池移除。本次只整理文件，未修改功能，未發布。
- 工作池結案確認：[確認設定與密鑰安全小工作結案](../specs/20261007-confirm-secret-security-task/spec.md)；依使用者確認，`TASK-001：設定與密鑰安全` 已自 `TODO.md` 未完成工作池移除。本次只整理文件，未修改設定、密鑰或程式，未發布。
- 工作池顯示規則已調整：[TODO 只顯示未完成小工作](../specs/20261007-todo-open-items-only/spec.md)；`TODO.md` 後續只列未完成、待執行或待確認項目，已完成小工作改由 `CHANGELOG_CUSTOM.md` 與 specs 追溯。本次只整理文件，未修改功能，未發布。
- 小工作池已新增：[正式機更新工具、全專案 AI+BDD 與 KM 教育訓練系統規劃](../specs/20261007-release-ai-bdd-km-training-plan/spec.md)；正式機更新工具需先備份、更新 IIS 網站資料夾、smoke test 與 rollback 演練，正式操作仍需另行授權；全專案 AI+BDD 分批導入；KM 教育訓練系統先規劃課程、教材、指派、簽到、測驗、完成紀錄與報表。本次只排程與設計，未實作，未發布。
- 全專案 AI+BDD 盤點與共用範本：[AI-BDD-ALL-TASK-001](../specs/20261009-ai-bdd-all-task-001/spec.md)；已建立十專案導入盤點、共用段落、STOP RULE 與 BDD feature 範本。本次為文件型小工作，未修改應用程式、未建置、未發布。
- AI+BDD 第一批高頻專案導入：[AI-BDD-ALL-TASK-002](../specs/20261009-ai-bdd-all-task-002/spec.md)；PmrPortal、TransFiles、KM 與 SPC 已對齊 AI+BDD 入口、features 目錄與導入紀錄。本次為文件型小工作，未修改應用程式、未發布。
- AI+BDD 第二批支援/工具專案導入：[AI-BDD-ALL-TASK-003](../specs/20261009-ai-bdd-all-task-003/spec.md)；Chameleon、DH_Temperature、PMR_ERP撈取工單、DS2000、Voice、python-pypxlib 已完成輕量 AI+BDD 入口、features 目錄與導入紀錄。本次為文件型小工作，未修改應用程式、未發布。
- 小工作池已新增：[Portal 生日/團保與 SPC 咬蝕 X- 小工作規劃](../specs/20261007-new-work-items/spec.md)；Portal 待規格包含生日資料管理與登入生日快樂通知、團保專區瀏覽與人事管理，SPC 待規格包含咬蝕 X- / 不生產資料保留匯入但不列入 SPC 統計與圖表。本次只排程與發想，未修改功能，未發布。
- SPC 開發流程已擴充：[SPC AI + BDD 開發模式導入](../specs/20261007-spc-ai-bdd-development/spec.md)；後續 SPC 開發採 SDD + BDD + AI Coding，重要功能需以 Given / When / Then 定義驗收行為，AI Coding 只處理目前 Task 必要範圍。本次只建立文件與 `features/` 入口，未修改業務功能，未發布。
- KM 開發流程已初始化：[KM TASK-000 SDD + BDD 開發框架初始化](../KM/specs/tasks/TASK-000-sdd-bdd-initialization.md)；KM 後續工作採一次一個 Task、Spec 先行、BDD 驗收、測試驗證與 `KM/docs/progress.md` 追蹤。本次只建立文件與目錄骨架，未修改業務功能，未發布。
- 正式站已發布：[SPC 正式機發布](../specs/20261007-spc-production-publish/spec.md)；2026-10-07 已重建 production backend/frontend 交付包並更新正式 IIS `SpcApi`、`SpcWeb` 指向 `D:\SPC\release\production\...`。正式 API `/api/version` 回 `environment=production`，正式 Web 首頁與新版 JS/CSS MIME smoke test 通過；未發布 Portal，未寫入 `D:\Sites\PmrPortal`，未手動修改正式資料。
- 測試站已完成：[藥液分析公式版本記錄與回復](../specs/20261006-chemical-formula-versioning/spec.md)；藥液公式目前存在 `ChemicalAnalysisConfigJson` 並由單筆主檔編輯覆蓋，已完成規格、後端版本資料模型、migration、版本服務、既有 PUT 整合、查詢/回復 API、後端測試、前端版本紀錄入口、回復操作、前端 build/UI 靜態檢查與 SPC 測試站 smoke test。正式站未發布。
- 測試站已完成：[藥液公式總覽與批次儲存頁](../specs/20261006-chemical-formula-overview-batch/spec.md)；既有藥液總覽頁已完成公式 draft 編輯、異動標示、批次儲存確認與成功/失敗結果欄；第一版沿用既有單筆 PUT 與公式版本紀錄，不新增後端批次 API。SPC 測試站 frontend 已發布；backend 未變更、未發布；正式站未發布。
- 測試站已完成：[藥液公式總覽可讀性改善](../specs/20261006-chemical-formula-overview-readability/spec.md)；公式欄位預設 5 行、等寬字體、較高最小高度並保留可手動拉高，不改 API、儲存或計算。SPC 測試站 frontend 已發布；backend 未變更、正式站未發布。
- 測試站已完成：[藥液公式總覽完整預覽](../specs/20261006-chemical-formula-overview-full-preview/spec.md)；公式欄位預設完整換行顯示，按「編輯」才切換為 textarea，保留既有批次儲存，不改 API、儲存或計算。SPC 測試站 frontend 已發布；backend 未變更、正式站未發布。
- 測試站已完成：[藥液公式總覽分頁](../specs/20261006-chemical-formula-overview-pagination/spec.md)；總覽表格每頁 5/10/20 筆，篩選回第 1 頁，批次儲存仍涵蓋全部 dirty rows，不改 API、儲存或計算。SPC 測試站 frontend 已發布；backend 未變更、正式站未發布。
- 測試站已完成：[SPC 管制圖/趨勢圖點位排除與隱藏恢復](../specs/20261006-chart-point-exclusion/spec.md)；右鍵設定「顯示但不列入計算」與「隱藏且不列入計算」，隱藏點可由已排除點清單恢復。管制圖右鍵選單、管制圖已排除點恢復清單、趨勢圖右鍵選單與趨勢圖已排除點恢復清單已完成；後端回歸、前端 build/靜態 UI 檢查與 SPC 測試站 smoke test 已通過。正式站未發布。
- 小型修正（測試 API 已發布）：[PPC 重複鍵儲存防護](../specs/20260929-ppc-duplicate-key-guard/spec.md)；SPC 管制項目新增/編輯若同鍵其他列已存在，後端先回 409 可讀訊息，不再外洩 SQL 唯一索引錯誤。
- 測試與正式 SPC 前端已發布：[藥液 GENERAL 視同早班](../specs/20260922-chemical-general-as-open/verification.md)；班別 GENERAL／空白當早班，圖上不丟歷史點；開／收線仍只限 N1／N2。資料未改；正式 Portal 未發布。
- 正式已轉換：[N1／N2 舊 CLOSE 轉收線](../specs/20260922-chemical-close-stage-apply/verification.md)；`PMR_SPC_2026` 198 筆 CLOSE＋GENERAL → OPEN＋CLOSE，剩餘 0。9 月前僅開收線、9 月後才分早中班；`OPEN`＋`GENERAL` 開線列與 MIDDLE 未改。[盤點](../specs/20260922-chemical-close-stage-inventory/verification.md)
- 正式匯入完成：[2026 年 9 月咬蝕日報正式匯入](../specs/20260922-etch-production-import/verification.md)；Portal `PMR_PORTAL_UAT` 48 份／3,650 點，SPC `PMR_SPC_2026` 3,746 筆／16 mappings；逐點、16 圖、隔離與 48 份防重跑均通過。
- 盤點（未寫入）：[咬蝕正式庫唯讀盤點](../specs/20260922-etch-production-inventory/spec.md)；測試 48 份 vs 正式 0；Portal Production 連線 `PMR_PORTAL_UAT` 已由使用者確認為正式。
- SSO 防重複（測試 API 已發布、歷史資料關係待確認）：[AD／工號單一操作者](../specs/20260917-sso-single-operator/spec.md)；可信工號與 AD 沿用既有 ID/權限，legacy 不再首次另建帳號；衝突不猜測合併。

- 登入小型變更（測試站已發布）：[直接導向 Portal 登入](../specs/20260917-direct-portal-login/spec.md)；不顯示 AD 按鈕/過渡卡片，保留既有 SSO 與原頁返回。

- 設備狀態修復（測試站已發布）：[Chameleon 來源表 migration](../specs/20260917-chameleon-source-schema/spec.md)；補齊既有模型需要的表與唯一索引，空表沿用設定檔來源，設備狀態 API 恢復 200。

- 跨專案（測試站已發布）：[藥液班別與 N1/N2 取樣階段](../specs/20260917-chemical-shift-stage/spec.md)；早/中班保留，N1/N2 開/收線独立儲存；12 核心及 3 UI 案通過，真人登入驗收待執行。

- 開發流程：[一句話需求預設流程](../specs/20260916-short-request-defaults/spec.md)；簡短開發需求自動依 AGENTS.md 執行 SDD 與必要驗證。

- 小型變更（測試站已發布）：[儀器列表捲動改善](../specs/20260916-calibration-table-scroll/spec.md)；固定表頭、60dvh 表格內捲動、上方左右按鈕；手機導覽與內容上下排列。
- 小型變更（測試站已發布）：[校正管理列表隱藏保管人欄位](../specs/20260921-calibration-hide-custodian-column/spec.md)；列表不顯示保管人，主檔編輯、保管人收件通知與後端契約不變。

- 儀器欄位（測試站已發布）：[量測規格、精度、備註、校驗規範、允收標準](../specs/20260916-calibration-details/spec.md)；主檔/表單/匯入一致，原 Excel 補回測試庫 59 筆空白欄，保留日期與既有值。

- 新功能（測試站已發布，真實 NAS 待驗證）：[校正通知 Email／Synology Chat](../specs/20260915-calibration-chat/spec.md)；全域二選一、加密 Webhook、手動測試與群組防重送。

- 小型變更（測試站已發布）：[校正提醒天數簡化設定](../specs/20260915-calibration-reminder-picker/spec.md)；常用天數勾選、自訂 0～365 天與恢復預設，保留既有設定及通知規則。
- 小型變更（測試站已發布）：[儀器校正到期管理亮色配色](../specs/20260912-calibration-ui-colors/spec.md)；亮底青／琥珀卡片，不改業務規則。備份 `20260912-215320`。
- 新增功能（測試站已發布，登入端到端待驗證）：[儀器 Excel 批次匯入 v1.1](../specs/20260911-instrument-calibration-import/spec.md)；第一工作表預覽勾選、僅新增、批次補齊主檔、交易與稽核。72 後端及 3 介面測試通過；SPC 測試站備份 `20260912-145908`；實際登入/SQL Server 端到端待驗證。正式庫與真信未執行。
- 小型變更（測試站已發布）：[未校列先建檔後補日期](../specs/20260912-calibration-import-pending-dates/spec.md)；12 筆可匯入，日期留空由使用者後補。備份 `20260912-170311`。
- 小型變更（測試站已發布）：[校驗方式公式讀儲存值](../specs/20260912-calibration-method-formula/spec.md)；正式 Excel J 欄公式可匯入。備份 `20260912-164138`。
- 小型變更（測試站已發布）：[儀器列表依編號排序](../specs/20260912-calibration-list-sort/spec.md)；校正管理列表依儀器編號，不改到期摘要。備份 `20260912-162830`。
- 小型變更（測試站已發布）：[儀器主檔校驗方式](../specs/20260912-calibration-method/spec.md)；列表／表單／匯入預覽顯示 Excel J 欄，不改週期判定。備份 `20260912-161409`。
- 小型變更（測試站已發布）：[儀器主檔放置地點](../specs/20260912-calibration-location/spec.md)；列表／表單／匯入預覽顯示 Excel H 欄，不推定部門。備份 `20260912-160201`。
- 小型變更（測試站已發布）：[儀器校正測試通知按鍵](../specs/20260912-calibration-test-email/spec.md)；提醒設定可寄【測試】信，不寫入每日防重寄、不開啟自動寄信。備份 `20260912-154446`。
- 規格定案（測試站已發布，待端到端／正式上線）：[儀器校正到期通知 v4.1](../specs/20260911-instrument-calibration/spec.md)；R-001～R-016 已確認。基準 3.6 已摘要。正式庫、真信與正式發布未執行。
- 業務主基準：[SPC 現行需求基準](SPC_REQUIREMENTS_BASELINE_2026-08-07.md)。
- 現況整理：[2026-08-31 程式與需求](CURRENT_CODE_AND_REQUIREMENTS_2026-08-31.md)。
- 後續已完成變更：[CHANGELOG_CUSTOM.md](../CHANGELOG_CUSTOM.md)。
- 開發流程：[SDD v1.1](sdd-workflow.md)；[十專案導入登錄](sdd-projects.md)。
- 流程導入：[20260909-sdd-adoption](../specs/20260909-sdd-adoption/spec.md)。

基準日期早於部分變更紀錄；下一次修改相關功能前，須先對照已確認的新需求並同步該範圍。
本次未全面驗證或重寫業務基準；2026-08-31 現況整理也不代表最新程式驗收結果。

- 資料同步（已完成；41 張業務表同步，18 張校正／人員／設定表保留）：[正式業務資料同步測試庫](../specs/20260917-production-data-refresh/spec.md)。


- 歷史查詢（測試站已發布，真人登入畫面驗收通過）：[藥液歷史完整查詢](../specs/20260917-chemical-history-complete/spec.md)；按日期先篩選、分批載入全部結果，解除每製程200筆截斷。


- 日期狀態（測試站已發布，真實畫面驗收通過）：[藥液日期日曆](../specs/20260917-chemical-date-status/spec.md)；依線別／班別／階段顯示日報、歷史及無資料，選日期保留草稿、不自動載入。


- 舊班別對應（測試API已發布，實際載入驗收通過）：[未標班別視為早班](../specs/20260917-legacy-morning-shift/spec.md)；GENERAL／空白日報納入早班，修改沿用ID，階段不推定。

- 舊量測載入（測試API已發布，真人頁面載入通過）：[量測日期回退](../specs/20260917-legacy-measurement-date/spec.md)；缺日報日期改用量測日期、沿用ID更新，N1/N2舊CLOSE暫緩處理。

- 第一批盤點完成、待第二批核准：[9月咬蝕量匯入與X̄-S](../specs/20260917-etch-september-import/spec.md)；只完成Excel/既有程式/測試庫唯讀分析，未開發、匯入或發布。

- 咬蝕第二批（測試站已發布、0901四線試匯完成，人工畫面待驗收）：[規格](../specs/20260917-etch-september-import/spec.md)；原始25/50點X̄-S、整體ER與實際線速；負值隔離，全月未匯入。

- 咬蝕載入修復：[規格與驗證](../specs/20260918-etch-report-load/spec.md)；排除點位返回父日報的JSON循環，完整點位契約保留。

- 9月咬蝕補匯：[規格](../specs/20260918-etch-month-import/spec.md)；工具及預覽完成，等待第3批明確核准，尚未寫入。

- 9月咬蝕第3批已核准並完成：新增44份、共48份/3650原始點；4份負值與9/18四份未完整仍隔離。[驗證](../specs/20260918-etch-month-import/verification.md)。

- [TransFiles日報預覽確認匯入](../specs/20260918-transfiles-etch-upload/spec.md)：僅兩測試庫、完整子組、相同略過/衝突拒絕、Pending重試；API與工具已更新，登入後人工驗收待完成。

- [藥液舊開收線修正](../specs/20260918-chemical-stage-repair/spec.md)：測試460筆已分階段（開線262/收線198），其他線GENERAL與已知中班保留；正式缺欄位，待相容性範圍確認。

- [N2中班歸收線](../specs/20260918-n2-middle-close/spec.md)：2026/9/11、9/14、9/16測試庫各10筆已歸MIDDLE+CLOSE，API載入通過。

- [N1/N2藥液開收線單線圖](../specs/20260918-chemical-single-line/spec.md)：同管制項目保留全部點位、日期/階段排序、班別提示及MR對齊；API0.1.59/前端0.1.58測試站已發布。

- 子組大小編輯上限由25修正為50：[規格](../specs/20260918-subgroup-input-50/spec.md)。既有X̄-S 50點支援不變，前端0.1.59已發布測試站。

- [正式完整升級相容性](../specs/20260918-production-compatibility/spec.md)：9項隔離契約及5項發布檢核通過；額外校正/設備schema範圍待確認，完整遷移演練未完成，不可發布。
# Particle Monitoring
- [落塵／Particle 粒子監控 Long Format](../specs/20260930-particle-monitoring/spec.md)：R1-R9、0.5/1/5/10 µm Long Format，支援 preview/confirm、查詢、位置比較及 C/U-chart；測試站已發布，正式站未發布。


## 2026-10-09 KM 教育訓練需求盤點同步

- 對應 SPC 同步規格：`specs/20261009-km-training-task001-sync/spec.md`
- 對應 KM 規格：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\KM\specs\tasks\TASK-012-training-requirements-inventory.md`
- 結論：`KM-TRAINING-TASK-001` 已完成角色、資料物件、人員資料來源原則與 BDD 驗收盤點；本次不涉及程式、資料庫、正式資料或發布。

## 2026-10-09 KM 課程與教材管理同步

- 對應 SPC 同步規格：`specs/20261009-km-training-task002-sync/spec.md`
- 對應 KM 規格：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\KM\specs\tasks\TASK-013-training-course-material-management.md`
- 結論：`KM-TRAINING-TASK-002` 已完成課程主檔、分類、講師、時數、教材附件、啟停用與版本紀錄規格；本次不涉及程式、資料庫、正式資料或發布。

## 2026-10-09 KM 訓練指派、報名與簽到同步

- 對應 SPC 同步規格：`specs/20261009-km-training-task003-sync/spec.md`
- 對應 KM 規格：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\KM\specs\tasks\TASK-014-training-assignment-registration-attendance.md`
- 結論：`KM-TRAINING-TASK-003` 已完成訓練指派、梯次報名、名額限制、簽到、補簽與補課規格；本次不涉及程式、資料庫、正式資料或發布。

## 2026-10-09 KM 測驗、完成認定與個人進度同步

- 對應 SPC 同步規格：`specs/20261009-km-training-task004-sync/spec.md`
- 對應 KM 規格：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\KM\specs\tasks\TASK-015-training-exam-completion-progress.md`
- 結論：`KM-TRAINING-TASK-004` 已完成題庫、測驗、及格分數、完成證明、個人待辦與完成歷程規格；本次不涉及程式、資料庫、正式資料或發布。

## 2026-10-09 KM 管理報表與匯出同步

- 對應 SPC 同步規格：`specs/20261009-km-training-task005-sync/spec.md`
- 對應 KM 規格：`C:\Users\ihao_ting.PMR.000\Desktop\SPC開發\KM\specs\tasks\TASK-016-training-report-export.md`
- 結論：`KM-TRAINING-TASK-005` 已完成完成率、逾期清單、課程歷程、部門統計與 Excel 匯出規格；本次不涉及程式、資料庫、正式資料或發布。

## 2026-10-09 正式機更新工具規格

- 對應規格：`specs/20261009-release-tool-task001/spec.md`
- 對應驗證：`specs/20261009-release-tool-task001/verification.md`
- 結論：`RELEASE-TOOL-TASK-001` 已完成正式機更新工具的環境盤點欄位、release manifest、hash、版本、目標環境、來源包與 smoke test URL 規格；本次僅文件變更，未連線正式機、未讀取機敏設定、未刪除資料、未發布。

## 2026-10-09 正式機更新工具備份/回復原型

- 對應規格：`specs/20261009-release-tool-task002/spec.md`
- 對應工具：`tools/release/ReleaseBackupRestore.ps1`
- 對應驗證：`specs/20261009-release-tool-task002/verification.md`
- 結論：`RELEASE-TOOL-TASK-002` 已建立只允許非正式目標演練的備份/回復 PowerShell 原型，支援網站資料夾備份、IIS 設定匯出、hash 紀錄與回復；已驗證語法、Production 阻擋與 `D:\Sites\PmrPortal` 阻擋。本次未連線正式機、未讀取機敏設定、未刪除資料、未發布。

## 2026-10-09 正式機更新工具網站資料夾更新原型

- 對應規格：`specs/20261009-release-tool-task003/spec.md`
- 對應工具：`tools/release/ReleaseApplyPackage.ps1`
- 對應驗證：`specs/20261009-release-tool-task003/verification.md`
- 結論：`RELEASE-TOOL-TASK-003` 已建立只允許非正式目標演練的網站資料夾更新 PowerShell 原型，支援 app_offline 建立、app pool 停啟、複製交付包、保留環境設定與 smoke test evidence；`app_offline.htm` 移除列入待刪除清單，不自動刪除。本次未連線正式機、未讀取機敏設定、未刪除資料、未發布。

## 2026-10-09 正式機更新工具沙盒演練

- 對應規格：`specs/20261009-release-tool-task004/spec.md`
- 對應驗證：`specs/20261009-release-tool-task004/verification.md`
- 結論：`RELEASE-TOOL-TASK-004` 已使用非正式 sandbox 完成備份、套用與 rollback 驗證；首次演練抓到 wildcard copy 問題並修正兩支工具，重跑後備份、套用、回復均通過。Sandbox 證據輸出未提交，`app_offline.htm` 與 sandbox 目錄未刪除並列入待刪除清單。本次未連線正式機、未讀取機敏設定、未刪除資料、未發布。

- 2026-10-19 SPC／Portal 正式更新已完成：使用者授權三筆 DB 相容 migration，備份驗證、版本/健康與匿名授權檢查通過；登入後人工驗收待執行。追溯 D:/SPC/specs/20261019-spc-portal-production-release/verification.md。

- 正式版更新工具：[RELEASE-PRODUCTION-TOOL-20261019](../specs/20261019-production-release-tool/spec.md)。SPC／Portal 獨立正式入口，支援預檢／備份更新／回復、固定主機目標、manifest SHA256、實際 EF pending 比對、SQL 備份驗證及失敗復原。舊非正式原型仍阻擋 Production；本次完成隔離驗證，未在正式機執行新版工具。

- 發布環境切換與遠端入口：[RELEASE-ENVIRONMENT-SWITCH-20261019](../specs/20261019-release-environment-switch/spec.md)。固定Test／Production與SPC／Portal四組目標，預設Test／Plan；實際四組Plan通過，不混用主機、IIS、DB；SPC前端須分環境build。本次未Apply／Rollback。

- Windows更新執行檔：[RELEASE-UPDATER-EXE-20261019](../specs/20261019-release-updater-exe/spec.md)。單EXE內嵌遠端引擎，提供環境／系統／交付包選擇、預檢綁定、兩項授權、確認、更新／回復及背景紀錄；C#20＋PowerShell新增4／既有35通過，僅Plan與負向驗證完成，未實際更新。

- EXE 製作交付包：[DESKTOP-BUNDLE-CREATION-20261019](../specs/20261019-desktop-bundle-creation/spec.md)。1.1.0 新增製作視窗，自動產生 manifest／ZIP，靜態讀取 API metadata、只讀目標版本，驗證後返回主畫面；不自動更新。

- Portal會議室正式更新（2026-10-19）：[Portal發布驗證](D:/PmrPortal/specs/20261019-meeting-production-release/verification.md)，API1.0.182／Web1.0.225，migration0，備份／健康／版本檢查通過。

- SPC正式最新版核對（2026-10-19）：[核對紀錄](../specs/20261019-spc-production-latest-check/spec.md)。正式API0.1.60／Web0.1.63已為已驗證最新版，本次無需重新部署；測試版本較舊，不作正式更新來源。

- 測試站已完成：[SPC-CHEM-REFERENCE-HISTORY-20261019](../specs/20261019-chemical-formula-reference-history/spec.md)。補設定頁版本依據欄位、總覽共用歷史入口及500字錯誤提示；沿用既有版本服務與Reason，保留20261007前案，後端17／前端6測試通過，測試站API0.1.61／Web0.1.64；正式未發布。

- 測試站已完成：[SPC-VERSION-HISTORY-DATE-FILTER-20261019](../specs/20261019-version-history-date-filter/spec.md)。使用者確認規格界線／藥液公式／F表版本歷史按修改日期查詢，非設定生效期；含台北日期日界、單邊區間、格式錯誤與先篩後分頁。2026-10-20經同意新增規格版本表並補齊測試庫既有F歷史migration，無虛構回填；37相關／9瀏覽器通過，測試站API0.1.62／Web0.1.65，正式未更新。

- SPC測試前端同源API修復：[SPC-TEST-WEB-API-SAME-ORIGIN-20261020](../specs/20261020-test-web-api-same-origin/spec.md)。API client固定/api，舊VITE_API_BASE不可覆蓋；Web0.1.66測試站發布、線上bundle14請求同源及12真實主檔API200驗證通過，API0.1.62／DB／正式不變。
