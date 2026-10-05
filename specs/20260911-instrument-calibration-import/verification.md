# 驗證紀錄
- 功能 ID：20260911-instrument-calibration-import
- 規格版本：1.1
- 日期／環境：2026-09-11～12／本機，隔離 SQLite、JWT HTTP 測試主機、127.0.0.1:5187 瀏覽器測試
- 實作狀態：本機實作完成
- 驗證狀態：本機必要檢查通過；測試站實際登入/SQL Server 端到端待驗證
- 發布狀態：已發布 SPC 測試站（`release/test`，備份 `20260912-145908`）；正式庫／真信未執行

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | ReadsFirstByRelationshipAndPreservesDatesAndSkipsFooter、RejectsInvalidArchiveAndTemplate、ActualTemplateIsReadOnlyAndHas59RowsWithExplicitExceptions | 第一張表、正確表頭/空白/頁尾/列號 | 實際範本 59 筆，其他表故意放無效 XML 亦未讀取 | 通過 |
| AC-002 | ParsesExplicitCycles、ReadsFirstByRelationshipAndPreservesDatesAndSkipsFooter | 年/月週期與編號正規化 | 12/24/60/6 月正確，A/B/I/Z/AA 欄位正確 | 通過 |
| AC-003 | SupportsExcelDateSystems、ReadsCachedFormulaWithoutRecalculationAndRejectsMissingCache、InvalidRowsHaveActionableErrors | 日期系統/文字/公式儲存值/模糊資料 | 1900/1904、完整西元日期、保留 2027-03-16；無快取及未校/預計拒絕 | 通過 |
| AC-004 | InvalidRowsHaveActionableErrors、MissingNamesCodesAndDuplicateCodesCannotBeSelected | 必填/日期順序/未來日期/未知週期 | 逐列錯誤；無效選列阻止整批寫入 | 通過 |
| AC-005 | SelectedOnlyAndReplaySkipsWithoutHistoryOrNotifications；Playwright desktop/mobile | 只匯入所選有效列，要求補齊主檔 | HTTP/服務真實 SQLite；介面勾選/全選/取消、必要欄位、結果，3 項 UI 測試通過 | 通過（隔離環境） |
| AC-006 | MissingNamesCodesAndDuplicateCodesCannotBeSelected、SelectedOnlyAndReplaySkipsWithoutHistoryOrNotifications、RealMultipartPreviewCommitReplayAndRevokedPermission | 同檔重複標錯、既有及重送略過 | 重送新增 0、略過 1，只有 1 筆主檔 | 通過 |
| AC-007 | CommitRevalidatesHashSelectionAndInactiveCustodian、FailureOnSecondInsertRollsBackFirstAndWritesFailureAudit、InvalidSelectedRowPreventsEntireWriteButCanBeExcluded | 重驗 hash/選列/保管人，整批回復 | SQLite trigger 第二筆失敗後主檔 0 筆、2 筆失敗且有批次稽核；排除錯誤列可新增 | 通過 |
| AC-008 | BothWriteEndpointsRejectUnauthorizedUsers、RealMultipartPreviewCommitReplayAndRevokedPermission；UI revoked permission | 401/403、權限撤回後提交拒絕、隱藏入口 | 真 JWT/MVC 對匿名、Viewer、無頁面權限、停用及未知帳號拒絕；權限改為 [] 後拒絕 | 通過（隔離環境） |
| AC-009 | SelectedOnlyAndReplaySkipsWithoutHistoryOrNotifications、FailureOnSecondInsertRollsBackFirstAndWritesFailureAudit | 稽核/結果、沒有校正歷史與寄信工作 | 操作者/來源/時間/雜湊與結果在稽核；歷史及通知表 0 筆 | 通過 |
| AC-010 | 全校正測試、API/Web 建置、文件內容/連結檢查 | 有證據且不發布 | 72 後端、3 UI 通過；API 0 警告/0 錯誤；Web 建置成功；連結結果見 evidence/checks.md | 通過 |

## 執行命令與證據
- 核心實作前：新增 CalibrationImportTests，再跑測試，因尚無 CalibrationImportOptions/Service 出現 CS0246（預期紅燈）。
- `dotnet test tests/MesSpc.Calibration.Tests --no-restore --logger "console;verbosity=minimal" --logger "trx;LogFileName=calibration-import.trx" --results-directory specs/20260911-instrument-calibration-import/evidence` → **72 通過、0 失敗、0 略過**；含原 42 項校正回歸。[TRX](evidence/calibration-import.trx)
- `dotnet build backend/MesSpc.Api --no-restore --verbosity minimal` → **0 警告、0 錯誤**。
- `npm run build:test`（frontend/mes-spc-web）→ **成功**，2395 modules；既有大 bundle 警示仍在。
- `node node_modules/@playwright/test/cli.js test --config playwright.calibration.config.ts` → **3 通過，exit 0**；桌面勾選/必要欄位/送出、無權限隱藏、手機交易失敗顯示。
- 已視覺檢查 [桌面](evidence/preview-desktop.png)、[手機](evidence/preview-mobile.png)；手機表格可水平捲動、欄位及確認按鈕可操作。
- 實際來源 SHA256：`604CA8F6E6CC0D12ED64B45E2EEF6A13F09357E0C206405B2E790F74212E5B81`；測試前後 bytes 相等，未改檔。
- HTTP 測試使用 WebApplicationFactory + 與專案相同版本的 Microsoft.AspNetCore.Mvc.Testing 10.0.9；隔離主機不執行產品 Program 的 migration、種子、背景通知。使用暫時記憶體加密 provider，不讀寫使用者金鑰。
- 初次本機只有 Testing 8.0.0 快取，與 .NET 10 PipeWriter 不相容；已改用 10.0.9 並完整重測。最終沒有因此留下產品相容性繞路。

## 未執行項目、限制及後續
- 瀏覽器測試的 API 全部 mock；HTTP 測試則是實際 MVC/JWT/SQLite。兩層分別通過，不等同實際 IIS/AD/SQL Server 的端到端驗收。
- 測試站已發布：IIS 以 `app_offline.htm` 確認指向 `D:\SPC\release\test`；備份 `20260912-145908`；保留 appsettings／web.config；API `0.1.57`／`test`；匯入 access 未登入 401，preview/commit 無檔 415；校正摘要未登入 401；Web `/calibration-instruments` 與新資產 200。證據：[deployment-result.json](evidence/deployment-result.json)。
- 尚未執行：測試站實際帳號登入/SQL Server 交易與競爭匯入；正式資料庫、正式 IIS、真信不在本次授權。T-007 保留未完成，不標示已上線。
- 原量測寫入與 SPC 計算未修改；既有 MeasurementWritePathDoesNotUseCalibrationTypes 回歸通過，但未重做真實量測畫面端到端。
- 前端測試啟動時既有 EquipmentPointsView.vue 出現 tr 直屬 table 的 Vue 警示，與本次元件無關；本次未修改該檔。
## 基準與變更紀錄更新位置
[需求索引](../../docs/requirements.md)、[基準](../../docs/SPC_REQUIREMENTS_BASELINE_2026-08-07.md)、[變更紀錄](../../CHANGELOG_CUSTOM.md)、[開發目的](../../ai_docs/10_change_log.md)
