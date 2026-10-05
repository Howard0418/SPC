# 驗證紀錄
- 功能 ID：20260911-instrument-calibration
- 規格版本：4.1
- 日期／環境：2026-09-11／本機 `D:\SPC`、`D:\PmrPortal`
- 實作狀態：本機實作完成（正式庫未套用 migration）
- 驗證狀態：待驗證（單元已跑；測試站已發布；瀏覽器端到端、真信、正式庫未執行）
- 發布狀態：已發布測試站（`release/test`）；正式環境未發布；未寄真信

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | Portal 是否自建儀器表；SPC 改資料後摘要同源 | 無第二份寫入表；摘要同源 | Portal `src` 無 `CalibrationInstrument` 實體；代理僅 GET summary。雙系統同資料 E2E **未執行** | 部分 |
| AC-002 | 建立／缺欄／重複編號 | 合法儲存、非法拒絕 | `NormalizesCodeAndRejectsDuplicatesAndStaleUpdates`、`ManualDueChangeRequiresReasonAndAllowsBlankLastDate` | 通過（單元） |
| AC-003 | 兩次校正、證書、異動 | 歷史與附件可查 | 服務測試有歷史與 Audit；`CertificateValidatesContent`；畫面上傳 **未執行** | 部分 |
| AC-004 | 模擬 31／30／8／7／0 天 | 僅對應階段 | `CatchUpSelectsOnlyCurrentStage` | 通過（單元） |
| AC-005 | 台北 08:00 前不掃描、之後補查 | 不依賴登入 | `ShouldScan`、`BeforeTaipeiEightDoesNotEnqueue`；BackgroundService 仍輪詢，SMTP 受 `DeliveryEnabled` 關閉 | 通過（單元） |
| AC-006 | 重跑排程 | 已成功不重寄 | `RepeatedScanSendsCurrentStageOnceAndSkipsMissedStage` | 通過（單元） |
| AC-007 | 合格更新下次日期 | 歷史保留 | `PassedUpdatesDueAndFailedPreservesItAndHistory` | 通過（單元） |
| AC-008 | 停用／報廢不寄 | 無信件 | `DisabledAndRetiredNeverSend`、`InactiveNeverSends` | 通過（單元） |
| AC-009 | 品保／非品保首頁 | 僅品保見摘要 | 程式：`Index.cshtml` 以 `IsQualityAssurance` 包住卡片；兩帳號登入 **未執行** | 部分 |
| AC-010 | 逾期儀器走既有量測 | 可送出 | `MeasurementWritePathDoesNotUseCalibrationTypes`；實際送出畫面 **未執行** | 部分 |
| AC-011 | 台北日、當日非逾期、窗口＝max 天數 | 與決議一一致 | `UsesTaipeiDateAtUtcBoundary`、`UpcomingWindowFollowsMaxReminderDays` | 通過（單元） |
| AC-012 | 逾期 1／8 天階段 | OVERDUE-1／8 | `CatchUpSelectsOnlyCurrentStage` | 通過（單元） |
| AC-013 | 8/31＋1 月＝9/30；改期要原因 | 對齊／拒絕 | `MonthlyCycleClampsEndOfMonth`、`ManualDueChangeRequiresReasonAndAllowsBlankLastDate` | 通過（單元） |
| AC-014 | 送校中入摘要不寄信；不合格不延期 | 與決議四一致 | `CountSummary`、`InCalibrationDoesNotSend`、不合格不延期服務測試 | 通過（單元） |
| AC-015 | Viewer 不能維護；品保才看摘要；可取消保管人 | 與決議五一致 | `EditorDefaultPagesIncludeCalibrationManage`、`ExcludingCustodianWithNoRecipientsIsVisibleIssue`；Portal 畫面 **未執行** | 部分 |
| AC-016 | 三次退避後停止；改期取消 Pending | 與決議六一致 | `AfterThreeRetriesDoesNotSendAgain`、`ChangingDueCancelsPendingButPreservesSent` | 通過（單元） |

本輪命令與結果（2026-09-11）：

- `dotnet test tests/MesSpc.Calibration.Tests/MesSpc.Calibration.Tests.csproj` → **42 通過、0 失敗**
- 測試站發布備份識別碼 `20260911-140728`（僅 `release/test`）
- Smoke：SPC API `http://172.16.110.27:8081/api/version` 200（test）；校正 API 未登入 401；Portal `8091/health` 200（test）；SPC Web `:8083` 200；Portal 首頁未登入 302
- 測試庫 `PMR_SPC_TEST` 已有校正資料表與 `20260911040000_AddCalibrationModule`
- 未開啟 `Calibration:DeliveryEnabled`；正式庫與正式 IIS 未發布

## 未執行項目、限制及後續
- 未執行：瀏覽器端到端（品保／非品保帳號）、真 SMTP、正式 IIS、正式庫 migration。
- 測試站已發布；有未執行必要驗收，不得標功能完成／已上線。

## 基準與變更紀錄更新位置
- [docs/requirements.md](../../docs/requirements.md)
- [SPC 基準 3.6](../../docs/SPC_REQUIREMENTS_BASELINE_2026-08-07.md)
- [PmrPortal 引用](../../../PmrPortal/specs/20260911-instrument-calibration/README.md)
- `CHANGELOG_CUSTOM.md`（兩專案）
