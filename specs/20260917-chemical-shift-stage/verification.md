# 驗證與發布（2026-09-17）

## 必要驗證
- 核心測試先建；初次因新政策/欄位/參數尚未實作而失敗，實作後 SQLite 核心 11 案通過；migration 範圍與 JSON 錯誤回應補驗 2 案通過（其中 1 案重驗，共 12 個獨立案例）。
- [核心測試](../../tests/MesSpc.Chemical.Tests/ChemicalStageTests.cs)：`dotnet test tests/MesSpc.Chemical.Tests --no-restore`；最後補驗 filter `FullyQualifiedName~OtherLine|FullyQualifiedName~Migration_Only`。
- Portal `tests/chemical-stage-ui.cjs`：Node/Playwright 2 案通過，另只執行新增 late daily 案例通過，共 3 案。讀取實際 Razor 頁面腳本，主檔/日報 API 使用測試回應；不是已登入 IIS 端到端。初期夾具欄位 ID/新增表單顯示與 UTF-8 已修正。
- SPC API、Portal API、Portal Web 均 `dotnet publish -c Release --no-restore` 成功，暫存產物雜湊與部署主 DLL 一致。SPC 出現 NU1900（NuGet 弱點來源無法連線），無编譯錯誤；Portal 兩專案此次建置无輸出警告。EF CLI 10.0.5 較 runtime 10.0.9 舊，但 migration 產生成功。

|驗收|證據與結果|
|---|---|
|AC-001|UI 保留早班/中班兩選項，標籤為班別；各線別切換後仍可選，通過|
|AC-002|N1/N2 精確代碼開/收線；ST1、N10 停用並送 GENERAL；空白/大小寫正規化與非法階段拒絕，通過|
|AC-003|N1、N2 各四組實際 SQLite 預覽、確認、載入互不覆寫；同組再次儲存更新；關聯唯一索引拒絕真重複；其他線兩班獨立。UI payload/query 四組一致，切換清除草稿，遲到回應不覆蓋新階段，通過|
|AC-004|歷史 GENERAL 不被四組新資料覆寫，舊參數查詢可取回；Portal 保留原授權，發布後代理未登入 401、頁面 302 /Login。既有測試庫資料檢核一致，通過必要範圍；真人品保登入操作尚未執行|

## Migration 與相容性
- `20260917002203_AddChemicalSamplingStage`：僅新增 SamplingStage（非空，GENERAL 預設）及替換日報唯一鍵，共 3 個 Up operations。
- 產生 migration 時發現工作區既有 Chameleon table/校正 Id 註解的 model 差異，已從本次 migration 與新增 snapshot 變動排除，未改該等既有業務程式。
- 測試庫發布前只有本次 migration 未套用；發布後確認紀錄存在。
- 發布前後 VariableMeasurements 均 1,838 筆；`CHECKSUM_AGG(BINARY_CHECKSUM(Id,MeasuredValue,SamplingPhase,PortalDailyDate))` 均 450790204，新增階段均 GENERAL。未把舊資料猜測為開線/收線。
- 自動測試使用自動釋放的 SQLite 記憶體資料庫，未建立真實量測或寄信。

## 發布與備份
- 備份識別：`chemical-shift-stage-20260917-082711`。
- 應用備份為各目標目錄的 `.backup-chemical-shift-stage-20260917-082711` 同層目錄；測試庫 COPY_ONLY/CHECKSUM 備份且 RESTORE VERIFYONLY 成功：SQL Server 備份目錄內 `PMR_SPC_TEST_chemical-shift-stage-20260917-082711.bak`。
- SPC：`D:/SPC/release/test/backend`（IIS SpcApi），資料庫 PMR_SPC_TEST；`8081/api/version` 200、environment=test。
- Portal：`D:/PmrPortal/release/test/portal-api`（PmrPortalApi）及 `portal-web`（PmrPortalWeb）；Target=Test，代理 SPC=8081，Web API=8091。
- 保留既有 appsettings*.json/web.config，驗證檔案 hash 未變；先啟動 SPC 確認 schema，再啟動 Portal。全部本次 app_offline 已移除。
- Smoke：Portal 藥液頁 302 /Login，代理 401；SPC Web 8083 首頁 200（本次不需變更 SPC 前端）。正式站與 D:/Sites/PmrPortal 未發布。
- 回復使用一致的測試庫備份及三應用備份；已有分階段新資料時不可直接執行縮回舊唯一鍵的 Down。

## 文件
本次規格、雙專案索引/變更及 API 契約同步；本次 Markdown 本機連結檢查通過。保留原工作區其他修改，未提交 Git。
