# Portal／SPC／KM 國際化可行性報告

2026-10-22｜I18N-TASK-01｜來源分析完成，功能未實作。

## 1. 結論與可重用架構

可分階段導入。**SPC 採 Vue I18n；Portal／KM 保留 Razor，採共用 JSON 契約的 .NET 與輕量前端介接。**不為國際化改寫三套前端，不建置第二套登入。完整偏好同步及無刷新切換尚非既有能力。

| 系統 | 已核對架構／版本 | 可重用位置 |
|---|---|---|
| SPC | Vue 3.5.34、Router 4.6.4、ECharts 5.6.0、Vite 6.4.2、Tailwind 宣告 ^3.4.17；API net10.0；Web 0.1.70／API 0.1.68 | `frontend/mes-spc-web/package-lock.json`（實際鎖定版本）、`package.json`、`src/main.js`、`src/App.vue`、`src/router/index.js`；後端 `backend/MesSpc.Api/MesSpc.Api.csproj` |
| Portal | ASP.NET Core net8.0 Razor Pages；Bootstrap 5.1.0、jQuery 3.6.0；錯誤記錄頁另內嵌 Vue 2.7.16／BootstrapVue 2.23.1；Web 1.0.234／API 1.0.191 | `D:/PmrPortal/src/PmrPortal.Web/PmrPortal.Web.csproj`、`Pages/Shared/_Layout.cshtml`、`wwwroot/lib/bootstrap/dist/css/bootstrap.css`、`wwwroot/lib/jquery/dist/jquery.js`；API csproj |
| KM | ASP.NET Core MVC net10.0-windows；自訂 CSS／JS；1.6.7 | `C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/KM/src/PMR.KnowledgeHub/PMR.KnowledgeHub.csproj`、`Views/Shared/_Layout.cshtml`、`wwwroot/css/`、`wwwroot/js/site.js` |

以下 SPC 路徑相對 D:/SPC（簡寫 src/ 的 Vue 路徑相對 frontend/mes-spc-web）；Portal 路徑相對 D:/PmrPortal；KM 路徑相對上述 KM 根目錄。KM 雖附 Bootstrap 5.3.3 靜態檔，共用 Layout 未載入它，不能據此當作現行 UI 元件庫。

Vue I18n 官方支援 Vue 3，可評估 v11 的 Composition API `legacy:false`；實作時鎖定並驗證具體版本，本次未安裝。參考：[安裝及相容性](https://vue-i18n.intlify.dev/guide/installation)、[Composition API](https://vue-i18n.intlify.dev/guide/essentials/started)。

.NET 可沿用 IStringLocalizer／View／驗證的介接點，但官方慣用資源為 resx；**JSON localizer 及前端即時切換需補介接，不是現成內建能力**。參考：[ASP.NET Core localization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)。不因本案升級 Runtime。

## 2. 現況與尚未支援清單

盤點方法：列舉全部指定 UI 目錄，搜尋語系註冊／偏好／硬編碼／格式／權限，再閱讀關鍵入口與代表性業務。完整路徑見 [來源清單](inventory.md)。尚未逐行審核所有文字或進行真人頁面操作。

| 功能 | 現況及依據 | 後續處理 |
|---|---|---|
| 三語字典、穩定 key、fallback、缺漏檢查 | 未見完整實作；SPC `src/main.js` 僅註冊 Router，package 未引用 vue-i18n；Portal／KM Program 未註冊 localization | 建字典／介接／key 與參數檢查 |
| 右上語言選單、不重登入、不丟表單 | 未見語言選單；三套共享 Layout／App 為入口 | SPC 保留 reactive state；Razor 需同時處理伺服器及動態 DOM 文字，不能靠整頁重載達成 |
| 導覽、登入、表單、表格、Dialog、提示 | SPC 35 View＋9 元件；Portal Pages 72＋Areas/Mobile 9 cshtml；KM 44 cshtml，含 partial／基礎範本／可能未路由檔案 | 每批核對路由可達性、角色、文字 key；數量不等於實際頁面或翻譯條目 |
| 後端／API／驗證訊息 | Portal `Controllers/GroupInsuranceController.cs:53` 等硬編碼 message；`Models/PortalLinks/PortalLinkUpsertRequest.cs:7` Required；KM Repository／Controller 中文例外；SPC Controller／Service 訊息 | 增量錯誤代碼／messageKey／args，保留原 message、HTTP 狀態與既有呼叫契約；含 model validation、前端 validation |
| 現有 .resx | Portal `Properties/Resources.resx` 有框架描述，但 `/root/data` 實際 0 筆 | 不是可直接重用的三語字典；Designer.Culture 不代表完成語系功能 |
| 已儲存使用者偏好 | Portal `Entities/User.cs:3`／`Data/AppDbContext.cs:8` 的 Users，SPC `Domain/Entities/Models.cs:71` Operator，KM Database／Models 未見語言欄位；實庫未查 | 不覆寫生日、帳號、角色欄位；另提偏好 schema／授權 |
| 圖表及公式文字 | SPC `SpcChartView.vue:1644–1647,1827,2018,2552` 硬編碼軸名、圖例、zh-TW 日期；9 元件含公式／歷史／分段明細 | 重建顯示標籤，保留資料、選段、縮放、量測日期、公式原文與版本證據 |
| 匯出與排程 | SPC `Services/SpcOverviewReportService.cs:50–64`、`Controllers/ReportsController.cs:33`；Portal `Pages/Stationery/Summary.cshtml.cs:47` 等 ClosedXML；KM `Controllers/TrainingController.cs:84–92` 中文 CSV 欄頭 | 報表標籤／欄寬／工作表名／列印驗證；機器匯入格式另保留，不逕改欄頭 |
| PDF | 已見 Portal PDF 上傳／列印及 KM PDF 預覽／憑證頁；本次未確認通用 PDF 產生器 | 使用者原 PDF 不翻譯；後續區分系統列印 PDF、上傳附件與產生器，測試字型／換頁，不假設已有 PDF 匯出 |
| 郵件、背景通知、持久化訊息 | Portal `MeetingRoom/MeetingRoomInvitationMailService.cs:228` 固定 zh-TW；KM `DocumentRepository.cs:231,233` 存中文通知／狀態 | 新通知可存 code＋args 或格式化時選語系；既有紀錄原樣保留。寄件語言／多收件者與排程無瀏覽器語系須另定政策 |

### 登入與偏好同步

- Portal：`Api/Program.cs:68,105–106` LDAP AD 與 cookie；Users 有 `AdAccount`／`AdObjectGuid`。`Services/SpcSsoTokenService.cs`、`KmSsoTicketService.cs`、`Middleware/SpcSsoTokenMiddleware.cs` 已有跨系統入口。
- SPC：`backend/MesSpc.Api/Program.cs:29` JWT Bearer；`src/router/index.js` 以 token、role、permission 判斷。切換語言不得重建角色或以翻譯字串比較權限。
- KM：`src/PMR.KnowledgeHub/Program.cs` 的 KmAuth／KmSession／Portal handler、`KmSsoTicketValidator`／SQL nonce；`Services/KmAuthorizationService.cs:18` 控制文件可見性。語系不能改票證、nonce、AD 或 ACL。
- 既有正式拓撲為 Portal 網域、SPC／KM 不同 IP／port，測試亦不同 origin；見前次 [正式核對](../20261021-spc-portal-production-preflight/verification.md)。本次未重新連線驗證部署設定。LocalStorage 不可跨 origin；父網域 cookie 也不能解決正式 IP 入口。
- 建議 Portal 作偏好權威來源，後續提供「目前本人」讀／寫設定契約，嚴格 allowlist 三種 locale。先確認 AD GUID／帳號及各系統使用者對應；SPC JWT 不能假定可直接當 Portal cookie 使用。沿用既有驗證完成本人辨識，不新增共用高權限服務帳號。
- 優先序：使用者本次明確選擇 → 已登入者伺服器偏好 → 合法本機快取 → zh-TW。明確選擇成功後更新伺服器；同步失敗保留本頁資料並顯示待同步，不默默登出或回復舊選擇。新登入依伺服器偏好，跨系統同步時點至少入口／登入／主動切換；跨已開分頁即時推送另評估，不假裝現有能力。
- 未登入可用 localStorage；單純 localStorage 只能作先期快取，不算完成帳號持久化要求。預設維持 zh-TW，不由瀏覽器英文自動覆蓋使用者預設。
- Portal Web `Program.cs:226` 的 proxy 會複製 request headers；PageModel 的個別 HttpClient 呼叫仍需逐一確認文化傳遞。兩套 Razor 的 PortalApi handler `UseCookies=false` 要保留，語系只能放每個 request，避免共享 HttpClient／全域 culture 導致跨使用者污染。

## 3. 建議檔案結構與相容規範（尚未建立）

```text
localization/                         # 規範與發布快照，非線上共用服務
  contract.md / dictionary.schema.json / glossary.csv / review-ledger.csv
  shared/{zh-TW,zh-CN,en-US}.json
各專案/localization/{zh-TW,zh-CN,en-US}/
  common.json                        # 同版本共用快照
  <system>.<module>.json              # 系統專屬字典
SPC frontend/mes-spc-web/src/i18n/     # Vue I18n 介接、載入及格式化
Portal Web／Api Localization/         # 共用 JSON localizer 與錯誤契約
Portal Web wwwroot/js/i18n.js         # Razor 頁面保留表單的即時文字切換
KM src/PMR.KnowledgeHub/Localization/  # 相同契約，獨立建置
tools/i18n/                           # 缺漏、重複、placeholder、來源 hash 檢查
```

- 共用契約主文件建議存 SPC，字典以版本化快照同步到獨立 repo；不強制合併、不在正式切換時依賴外部翻譯或另一站可用性。打包檢查版本／hash 防止字典漂移。
- key 如 `common.actions.save`、`portal.meetings.created`、`spc.chart.controlLimits`、`km.documents.accessDenied`；不使用中文原文當 key。模組粒度載入並快取；JSON 缺項逐 key 明確 fallback zh-TW，不只 fallback 整個檔案。繁中也缺項須記錄診斷，CI 阻擋。
- 共同 placeholder 採 `{name}` 等具名參數，.NET 與 Vue 的複數／格式語法不同，需介接測試，不能直接把 Vue 特殊語法當 .NET String.Format。三語 placeholder 名稱及數量一致。
- Portal `Pages/Admin/BugErrorLogs.cshtml:179–185` 為 Vue 2.7.16／BootstrapVue 2.23.1 島狀頁面，根 package.json 也有 Vue 2 依賴；不能裝入 SPC 的 Vue I18n v11。建議沿用共用 JSON lookup，透過 Vue 2 的 reactive locale／method 介接，不用外部 DOM 文字改寫對抗 Vue 重繪，也不為此新增另一套 Vue。`Areas/Mobile/Pages/Shared/_LayoutMobile.cshtml:8` 及 9 個 Mobile 範本需獨立選單／驗收。`MeetingRoom/Board.cshtml:1` 明確路由至 /MeetingRoom/Reservation，不能只按檔名尋找頁面。
- 字典只存顯示文字，Razor 編碼／DOM textContent／Vue 插值，不用任意 HTML 注入。不得翻譯 DB 代碼、欄位名、URL、檔案 key、Excel 公式或授權字串。
- Razor 伺服器輸出第一屏，瀏覽器以 data-i18n 等標記更新文字、placeholder、aria、title、option 顯示；不重建輸入節點、不改 value 或送出模型，不以整頁重載冒稱無狀態遺失。動態新增 Modal／驗證／AJAX 也走同一契約。
- API 建議增量 `{code, messageKey, args, message}`，既有消費者仍可讀 message。包含 400 validation field errors、401／403、業務錯誤及安全的未知錯誤；不公開內部 exception。不以中文訊息判斷分支。
- 顯示語言與數字解析／商業時區分離。保持 ISO、InvariantCulture、Asia/Taipei 業務日與現有精度；不要全域修改 CultureInfo 連帶改 JSON、小數點、匯入或公式日期。

## 4. 英文 UI 風險

以下是來源風險，**不是已實測破版或已修復**。

| 區域 | 實際來源 | 風險及建議 |
|---|---|---|
| SPC 左導覽 | `src/App.vue:229,244` w-64／truncate | 英文較長會截斷；改可換行／合理寬度與高度，必要 tooltip，不能靠全面縮字或隱藏 |
| SPC 篩選／圖表 | `SpcChartView.vue:2118,2320,2591` 密集 grid；`:1644` 軸名 | Label、8 欄摘要及軸名空間不足；自適應欄數，圖表 grid／legend 實測；不能更動序列計算 |
| Portal 頂列 | `Pages/Shared/_Layout.cshtml` flex-nowrap／overflow-hidden、公司名／日期／登出 | 加入選單後寬度緊；依 breakpoint 換行，檢查焦點與日期長度 |
| Portal 表格／月曆／表單 | `Pages/Admin/Permissions.cshtml`、`GroupInsurance/Index.cshtml`、會議相關頁及 shared sidebar | 英文欄名、按鈕、日期、月週導覽；保留表格局部水平捲動，避免整頁溢出 |
| KM 搜尋／Sidebar | `wwwroot/css/secure-search.css:1` minmax(260px,1fr)＋150／150／130px；`sidebar-layout.css:2` profile ellipsis | 既有 1000／650px 搜尋 breakpoint、760px 導覽 breakpoint 可重用，但英文長字及右上選單仍需實測 |
| 三站 Dialog／通知／Footer | 共用 Layout、Vue 元件、動態 JS | 長警告按鈕、工具提示、鍵盤操作；允許換行與自適應高度 |
| 匯出／列印 | 前述 ClosedXML、CSV、KM ApprovalCertificate | 工作表名 31 字元限制、欄寬、頁邊、PDF 字型嵌入及換頁；文件原文不改 |

## 5. SPC／KM 特殊處理

### SPC

- Xbar-R／Xbar-S／I-MR、P／NP／C／U、CL／UCL／LCL、Cp／Cpk 保留慣用符號與後端圖表代碼；翻譯說明與軸名，不改算法或小數精度。
- 術語草案：規格界限＝Specification Limits；管制界限＝Control Limits；製程能力＝Process Capability；失控＝Out-of-Control；常態性檢定＝Normality Test；Anderson-Darling Test 保留專名。簡體使用「规格界限／控制限／过程能力」等候選詞，須業務審核，不能視為已批准 Glossary。
- `SpcEngine/Calculators/XbarRChartCalculator.cs:192`、XbarS／Imr／Attribute／Particle 計算器呼叫 WesternElectricRulesValidator；另有 `SpcEngine/Rules/NelsonRulesValidator.cs` 類別。**Nelson 類別存在不等同所有圖表啟用 Nelson**；語系需求不得順便替換規則引擎，也不可把 WE 規則誤標 Nelson。
- 重用既有統計與公式測試固定資料，三語比較點值、CL/UCL/LCL、Cp/Cpk、常態檢定結果、規則代碼、分段日期與實際公式版本完全一致；只允許顯示文字差異。
- `Services/VersionHistoryDateRange.cs:11`、`RawDataCsvExporter.cs:85,90`、`ParticleUploadService.cs:222` 的 Invariant／日期協定要保留；圖表同批跨日選版不得由 locale 影響。公式內容與參照文件為業務資料，不自動改寫。
- ECharts 切語言更新 options 文字，保留 dataZoom、選段及選點；Tooltip／匯出圖片亦更新。純文字之外的固定精度與運算輸出不可交給 locale 再解析。
- 排程報表無使用者當前 UI；需決定報表語言欄位或固定政策。機器匯入模板／原始 CSV 欄頭不可隨 UI 默默變更；人讀報表可另選語系。

### KM

- `Database/001-create-km.sql:3–12` 的 dbo.documents 儲存 title、summary、category、status、version 等；`Services/DocumentRepository.cs:35,166` 原樣寫入。UI 翻譯不得覆寫這些使用者內容、文件／附件、版號或摘要。
- 特別注意：既有 status／confidentiality／AI usage／審核狀態部分直接儲存中文字串，SQL 如 `DocumentRepository.cs:19,231` 也用中文字串判斷。必須以原值映射翻譯顯示，不改 option.value、POST 值或 SQL 條件，不能把 English 的 Approved 寫回原欄位。
- `Services/SearchIndexRepository.cs:14` SecureFullTextSearchAsync 實際為 dbo.document_search_indexes.content 與 documents 的 LIKE 搜尋；不是已證明可跨語意翻譯的搜尋引擎。`Controllers/DocumentsController.cs:49` 逐筆 CanViewAsync 後產生 snippet，需保留 ACL 與原查詢，搜尋摘要原文不自動翻譯。
- 切換簡體／英文不等同搜尋字詞自動繁簡／英中轉換；分類名稱若使用者建立，保留原值。跨語言搜尋擴充、文件標題／摘要多語欄位與內文 AI 翻譯各列獨立可選後案，不納入預設 UI 翻譯。
- 簽核、閱讀、下載、AI 禁止、版本、稽核與文件權限不變。新系統通知可按 locale 顯示，既有持久化通知／稽核內容不批次改寫。

## 6. 工作拆分與建議順序

詳 [tasks.md](tasks.md)：TASK-01 完成；TASK-02～21 為 **20 個待確認工作包**，不得自動開始。先契約／安全與資料方案，再以 SPC 試點，分別處理 Portal、KM，最後整合與人工／UI 驗收；不一次修改三套。

偏好持久化設計先行，但 schema 實作另授權；每個工作包開始前細化單批必要檔案與 BDD。大模組可再拆小，不以本表估算取代實際工作量。正式發布不是本次授權範圍。

## 7. 預估受影響檔案與風險

| 系統 | 未來累計粗估（非本次修改） | 主要風險 |
|---|---|---|
| SPC | 約 60～90 個既有檔＋字典／測試；至少 35 View、9 元件、App／入口、API 錯誤／報表 | 高：數值／日期／規則語意；中：圖表布局及長文字 |
| Portal | 約 130～195 個既有檔＋字典／測試；81 範本（Pages 72／Mobile 9），另 PageModel／JS／API／驗證／報表 | 高：跨使用者 culture、個人偏好、AD／SSO、持久化 schema；中：不刷新切換與錯誤契約 |
| KM | 約 60～90 個既有檔＋字典／測試；44 範本及 Controller／Repository／CSS／JS | 高：中文業務值誤寫、ACL／簽核與文件原文；中：搜尋與排版 |

這是來源範圍估計，不是所有檔案必改或工時承諾；未做字串 AST 抽取前，不給翻譯條目數或正確率。共用字典、測試、後端錯誤、郵件／匯出需額外新增必要檔案。既有 Portal 兩個使用者修改附件不屬本案。

各包 rollback：應用／字典以同一版本成對回復並保留繁中；不能回復字典卻留下新 key。新偏好可空值相容，失效 fallback zh-TW；schema 回復／備份需於具體方案驗證，不能假設刪欄安全。正式更新逐站核對與授權，不改 IIS binding、不升級 Runtime。

## 8. 翻譯品質及 UI 驗收

1. 建立 glossary 與 ledger：key、zh-TW 原文／hash、三語譯文、模組／畫面、參數、術語版本、AI 不確定標記、審核者／日期／結果／重大程度。先 AI 譯與語意／術語／情境／反向檢查，再交熟悉業務的審核者；變更原文即使 key 不變也須重新審核。
2. 各語言分別統計「合格已審核條目／已審核條目」≥97%，另報「已審核／全部應審核」覆蓋率、樣本範圍及未審清單，避免用少數抽樣冒稱全系統合格。SPC 公式／統計名詞、安全警告、權限及重要操作須全部人工審核，重大誤譯 0；AI 自評不算驗收。審核人尚未指定。
3. CI：三語 key 集合、重複 key、缺檔、placeholder、格式、非法 locale、fallback、原文 hash、疑似硬編碼均檢查；key 完整不等於語意合格。偽長文字測試僅輔助，不能取代真人英文。
4. 重用 SPC Playwright 1.60.0／node tests、API／Chemical xUnit，Portal 現有 Api/Web/Compatibility 等測試、MyProject.UiTests 的 NUnit／Selenium 4.33.0（ChromeDriver 137 與現用瀏覽器相容性需核對），根 package.json 亦已宣告 Playwright ^1.60.0，KM Workspace/Sso/Regression 測試。Portal／KM 瀏覽器 E2E 先評估重用現有執行環境；需要新增套件才另列理由，不先安裝 Selenium／Playwright。
5. 至少 3 系統×3 語言×3 桌面尺寸＝27 組基礎配置；每組涵蓋各角色可達頁、選單、表單、表格、Modal、錯誤及匯出。再加既有行動版：先以 390×844 及原 breakpoint 附近（KM 650／760／1000）驗證，其他原有支援尺寸在各包補核對，不能只測桌面。
6. 檢查 1920×1080／1366×768／1280×720 的溢出、遮擋、文字截斷、換行、Tooltip、鍵盤焦點、Header／Footer、圖表軸／圖例；表格允許容器內水平捲動，不能用 overflow hidden 或縮小全部字體過關。產出報告及失敗截圖（測試產物不提交 repo）。
7. Happy Path：三語導覽及即時切換；Boundary：最長譯文、閏日、跨日公式、長欄頭、表單尚未送出；Invalid：未知 locale、缺 key／參數、同步或 API 失敗；Regression：JWT／AD／SSO／權限不變、兩使用者語系隔離、重登入保留偏好、原始資料與統計完全一致。
8. BDD 範例：Given 已填未送出表單並套用日期查詢，When 切換 English，Then 同一路徑、參數、輸入與焦點保留且文字更新；Given 無權限文件，When 任一語言搜尋或直接下載，Then 仍不可見／拒絕；Given 同一 SPC fixture，When 三語分析及匯出，Then 數值／版本／規則代碼相同。

本階段僅完成來源與文件驗證：沒有三語畫面、測試報告截圖或 97% 成果；不將後續驗收規劃寫成已通過。
