# 小型變更：SMTP、檔案與背景排程可靠性
- 功能 ID：ARCH-TASK-007-SMTP-FILE-SCHEDULER-RELIABILITY-20261012
- 版本：1
- 狀態：已完成
- 專案／授權依據／需求基準：SPC 架構改善 TASK-007；依 `TODO.md` 架構改善排序與 `docs/requirements.md` 現行需求索引。

## 問題、預期行為與範圍
- 現況：`SpcReportSchedulerService` 每分鐘觸發 SPC 項目管制總覽週報/月報，SMTP 寄送由 `IEmailNotificationService.SendReportEmailAsync` 回傳成功或失敗。
- 問題：排程目前未檢查各收件者寄送結果，可能在全部 SMTP 寄送失敗時仍更新 `LastWeeklySentAt` 或 `LastMonthlySentAt`，造成該期報表不再於後續分鐘重試。
- 預期：至少一位收件者寄送成功後才更新對應 LastSent；若全部寄送失敗，保留 LastSent 不變，讓下一次排程迴圈仍可重試。
- 範圍：只調整 SPC 報表背景排程的寄送成功判斷與可測性；不新增資料表、不修改 SMTP 設定 UI、不變更報表內容、不操作正式站。

## 需求與驗收
- R-001：週報或月報到期且有有效收件者時，系統需依每位收件者的寄送結果判斷是否完成本期寄送。
- R-002：全部收件者寄送失敗時，不得更新 LastSent 欄位，排程應可於後續迴圈重試。
- R-003：至少一位收件者寄送成功時，才更新對應 LastSent 欄位，避免同一日重複寄送。
- AC-001：Given 週報到期且兩位有效收件者皆寄送失敗，When 執行報表排程處理，Then `LastWeeklySentAt` 維持 null 且回傳 sent count 0。
- AC-002：Given 月報到期且一位寄送成功、一位失敗，When 執行報表排程處理，Then `LastMonthlySentAt` 更新為目前 UTC 日期且回傳 sent count 1。
- AC-003：Given 排程未到設定時間，When 執行報表排程處理，Then 不產生報表、不寄信、不更新 LastSent。

## 計畫與任務
- 受影響檔案：
  - `backend/MesSpc.Api/Services/SpcReportSchedulerService.cs`
  - `backend/MesSpc.Api/Services/SpcReportScheduleProcessor.cs`
  - `backend/MesSpc.Api/Services/SpcOverviewReportService.cs`
  - `backend/MesSpc.Api/Program.cs`
  - `tests/MesSpc.Api.Tests/SpcReportScheduleProcessorTests.cs`
- [x] T-001：先建立 `SpcReportScheduleProcessor` 單元測試覆蓋 AC-001～AC-003。
- [x] T-002：抽出可測 processor 與報表產生介面，HostedService 僅保留觸發與錯誤記錄。
- [x] T-003：同步有效需求、變更紀錄、TODO 與驗證結果。

## 驗證
- Happy Path：月報部分成功時更新 LastSent 並回傳成功寄送數。
- Boundary Case：未到排程分鐘時不寄送、不產生報表。
- Invalid Input：全部寄送失敗時不更新 LastSent，保留重試機會。
- Regression Risk：需確認後端 build 通過，且排程 HostedService 仍由 DI 正常取得 processor。
- 實際結果及證據：`SpcReportScheduleProcessorTests` 3 passed；後端 build 0 warnings / 0 errors。
- 發布狀態：已發布 SPC 測試站 backend；正式站未發布。
- 限制或未決問題：本次不建立持久化 outbox、不新增 retry backoff 或寄送歷程資料表，後續可於稽核/追溯任務再規劃。
