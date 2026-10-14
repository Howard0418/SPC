# 小型變更：咬蝕量預覽 500 診斷與可讀錯誤
- 功能 ID：SPC-ETCH-PREVIEW-500-DIAGNOSTICS-20261014
- 版本：1
- 狀態：完成
- 專案／授權依據／需求基準：SPC；使用者 2026-10-14 回報 TransFiles 咬蝕量匯入仍出現 `[預覽阻擋] ... HTTP 500: SPC預覽拒絕，未匯入。`；對應 `docs/requirements.md` 中 TransFiles 直接匯入後 SPC 須提供錯誤追蹤與可修正訊息。

## 問題、預期行為與範圍
TransFiles 透過 Portal 呼叫 SPC 咬蝕量預覽時，若 SPC 內部因主檔缺漏、既有資料結構或批次資訊不一致而拒絕，應回傳可讀 400/409 訊息，不應讓使用者只看到 HTTP 500。

本次範圍限 SPC 咬蝕量預覽/確認服務的可預期錯誤處理與測試；不修改咬蝕量計算公式、不匯入正式資料、不改 TransFiles 來源解析規則。

## 需求與驗收
- R-001：SPC 咬蝕量預覽遇到未知或停用線別時，需回 409 可讀訊息，包含線別代碼。
- R-002：SPC 咬蝕量預覽遇到既有資料但找不到原 UploadBatch 時，需回 409 可讀訊息，不得冒出 HTTP 500。
- AC-001：單元測試覆蓋 QE1/QE2 及未知線別的預覽錯誤，不產生 UploadBatch 或 VariableMeasurement。
- AC-002：相關測試與後端 build 通過後刷新 SPC 測試站 backend。

## 計畫與任務
- 受影響檔案：`backend/MesSpc.Api/Services/EtchReportSyncService.cs`、`tests/MesSpc.Chemical.Tests/EtchTests.cs`、需求與變更紀錄。
- [x] T-001：將主檔與既有批次缺漏改為明確 `InvalidOperationException`。
- [x] T-002：補測試並執行相關驗證。
- [x] T-003：同步有效需求與變更紀錄，發布 SPC 測試站 backend。

## 驗證
- 方式、環境：
  - `dotnet test tests/MesSpc.Chemical.Tests/MesSpc.Chemical.Tests.csproj --no-restore --filter EtchTests -p:UseSharedCompilation=false`
  - `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`
  - `dotnet publish backend/MesSpc.Api/MesSpc.Api.csproj -c Release -o release/test/backend.publish-tmp --no-restore -p:UseSharedCompilation=false`
- 實際結果及證據：EtchTests 10 passed；backend build 0 warnings / 0 errors；publish 成功。第一次測試與 build 併行時遇 SourceLink obj 檔案鎖，改為單獨重跑測試後通過。
- 發布狀態：已刷新 SPC 測試站 backend，備份 `release/test/backend.backup-20261014-etch-preview-500-diagnostics`；已回收 `SpcSingleTest` AppPool，`app_offline.htm` 與暫存 publish 目錄已移除。PowerShell/curl 對 `https://SPCTEST.pmr.com.tw/api/version` 仍受 Windows Schannel `SEC_E_NO_CREDENTIALS` 限制，需以瀏覽器或受信任憑證環境補做外部 smoke。正式站未發布。
- 限制或未決問題：若測試站資料本身已有同日同線不同內容，仍會以嚴格匯入規則拒絕覆寫，需依錯誤訊息處理資料或人工確認；本次修正的目標是避免可預期主檔/既有資料問題變成 HTTP 500。
