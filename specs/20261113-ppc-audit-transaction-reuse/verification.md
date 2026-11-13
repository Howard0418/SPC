# 驗證紀錄

## 根因與實作

- 測試站 Windows Application log 於 2026-11-13 14:17～14:20 記錄 `/api/part-process-characteristics/2513` 失敗：`The connection is already in a transaction and cannot participate in another transaction.`
- `BusinessDataAuditFilter` 已開啟 Serializable 交易，PPC Update 又呼叫 `BeginTransactionAsync`。修正為已有 `CurrentTransaction` 時沿用外層交易，只有自行建立者才自行提交。
- 沒有 schema migration；公式內容、權限、Portal 與正式資料均未改動。

## 自動化與建置

- 新增 `PpcFormulaUpdateReusesBusinessAuditTransaction`：SQLite 關聯式測試在稽核交易內更新 PH 公式，PPC、公式版本與稽核事件同時寫入，1/1 通過。
- 相關回歸 `BusinessDataAuditFilterTests|PartProcessCharacteristicMaintenanceTests|ChemicalAnalysisFormulaVersionServiceTests`：19/19 通過。
- `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore`：成功，0 warning、0 error。
- 完整 API 測試：552 passed、10 failed、11 skipped；10項為既有藥液歷史判定／修復測試，隔離重跑同樣失敗，與本次交易修改無關，仍列回歸風險，不納入本件修正。

## 測試站發布

- Release ID：`spc-ppc-audit-tx-20261113`；來源 commit `667e730934469e99dfb3bfc9b9826f3ea8652cc2`。
- SPC 測試 API：`0.1.128`；Web 保持 `0.1.129`；migration 0。
- `PMR_SPC_TEST` 備份 `PMR_SPC_TEST-spc-ppc-audit-tx-20261113-cecc8e9441d248f8a7435c8b7fd260bd.bak` 已 VERIFY 通過；發布暫時權限已撤銷，dbcreator=0、sysadmin=0。
- 首頁 200/text-html；目前 JS 200/application-javascript；CSS 200/text-css。
- 同源（瀏覽器實際 API 基底）`/api/health` 回 `ok / 0.1.128 / test`，`/api/version` 回 `0.1.128 / test`。
- 實際部署 DLL ProductVersion 為 `0.1.128+667e730934469e99dfb3bfc9b9826f3ea8652cc2`。

## 待驗收與發布界線

- 先前錯誤請求已 rollback，PH 的 `U11` 改為 `USL` 必須由使用者重新按儲存；登入態實際 PUT 與畫面重載仍待使用者確認。
- 正式站未授權、未發布。

## 進度表與回復

- 中央原檔已備份為 `進度表-PPC公式儲存交易修正前-20261113-143214.xlsx`，再以候選檔取代；輸出 SHA-256 為 `BABED01E155F6A157167ADB5464190B06EAC49F72D0155770838B353A34CE69E`。
- 「小工作總表」新增本件待人工驗收列；「專案進度表」新增 2026-11-13 原生單日里程碑。原生 drawing anchors 105→105，工作表2、table1及套件13 parts保持。
- Artifact Tool 重讀、變更區渲染、ZIP重讀及 OpenXML SDK 驗證通過（0錯誤）；Microsoft Excel 人工開啟與LibreOffice列印未執行。
- 目前核心未完成41件，其中SPC相關18件；人工驗收與正式授權仍不視為完成。
- 回復方式：以本次更新前備份覆蓋中央原檔；程式則回復本次控制器及API版本提交後重新發布測試站。
