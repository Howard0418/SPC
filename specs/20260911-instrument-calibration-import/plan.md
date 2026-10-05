# 技術計畫
- 功能 ID：20260911-instrument-calibration-import
- 規格版本：1.1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
SPC 新增 CalibrationImportParser、CalibrationImportService、InstrumentImportController；在儀器頁掛載獨立 InstrumentImportPanel。既有服務抽出共用主檔欄位驗證（先測試）。Program 僅註冊服務。

## 實作方式與需求對應
R-001～004：以 .NET ZIP/XML 讀 OOXML 儲存值（保留公式快取，不觸發計算）。R-005～007：預覽/提交同一檔案 SHA256；伺服器只接受列號及補齊欄位，不接受任意客戶端儀器資料。唯一鍵與單一 transaction 保護重複及部分寫入。
R-008：獨立授權控制器，依登入角色、SPC 啟用人員及 CalibrationRules.CanManage 檢查。R-009：沿用 CalibrationAuditLogs，成功與略過批次寫同交易；回復後記失敗結果及 ILogger，不直接呼叫 SMTP。

## API、檔案格式及資料模型
- GET /api/v1/instruments/import/access：管理權限，回傳 canManage。
- POST /api/v1/instruments/import/preview：multipart file，回傳 hash、sheetName、rows（原值/解析值/errors/warnings/status）。
- POST /api/v1/instruments/import/commit：multipart file + options JSON（hash、selectedRows、department、custodianOperatorId、usageStatus、includeCustodian），回傳新增/略過/失敗與逐列結果。
- 統一 success/data/message。錯誤檔案 400、未登入 401、禁止 403、交易競爭 409。無 migration、無預覽持久化資料表。

## 相容性與風險
只新增，不覆寫；相同編號不同名稱也略過。資料不含歷史校正。現有日期規則共用，不改舊 API 行為。公式快取可能過期，預覽提示由使用者確認／重新儲存。

## 驗證安排
先新增 tests/MesSpc.Calibration.Tests 匯入測試，涵蓋 AC-001～009；用隔離 SQLite 做交易/權限 HTTP 整合，不連正式庫、不啟動真通知。dotnet test 校正測試；dotnet build SPC API；npm run build:test SPC Web。前端流程本機檢查；AC-010 文件內容及相對連結檢查。

## 發布、備份及回復
本次不發布 IIS、不連正式庫。原始 Excel 不修改。無 schema 變更；如日後部署，由獨立授權核對環境與備份。程式回復只撤回本功能檔案/掛載，保留既有工作區修改。

### 2026-09-12 後續測試站發布授權
- 前段為原實作階段範圍；本次使用者追加「發布」。實際 IIS：SpcApi → D:/SPC/release/test/backend，http://172.16.110.27:8081；SpcWeb → D:/SPC/release/test/frontend，http://172.16.110.27:8083。
- AppEnvironment=test、Auth=true、資料庫 PMR_SPC_TEST；Calibration:DeliveryEnabled 未啟用。已唯讀核對校正表與 migration；部署前比對所有程式 migration 均已套用。
- Release publish 至獨立 staging，前端 testhost build；不使用同時處理 production 的 publish-environments.ps1。
- 備份原 API/Web 程式（排除運行中 logs）；使用 app_offline.htm 短暫停止 API，僅覆蓋 staging 程式。保留全部 appsettings*.json、web.config、uploads/logs 等运行資料，不鏡像刪除旧檔。
- 檢查 payload hash、設定 hash、API version/test、Web/新資產 200、匯入 access 未登入 401；preview/commit 無 multipart 時允許 415。失敗則將覆蓋檔由備份回復、刪除本次新增的程式檔並移除 app_offline。
- 2026-09-12 已完成測試站部署，備份 `20260912-145908`。不進行真實儀器提交/SQL 寫入測試；實際使用者登入匯入驗收仍列待驗證。
