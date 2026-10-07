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

