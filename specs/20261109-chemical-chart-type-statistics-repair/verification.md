# 驗證紀錄

## 2026-11-09 唯讀盤點

- 以 Receipt Payload 的固定工作代號 `SPC-CHEM-OCT-PROD-VALUE-CORRECTION-20261105` 查得 493 筆工作：333 Completed、160 Failed；建立時間完全相同，範圍可精確重現。
- 160 筆失敗集中於 25 個 `ChartTypeId IS NULL` 的既有藥液 PPC，與原更正報告一致；未混入另外 3 個歷史失敗項目。
- 25 項在測試區皆有唯一自然鍵對應，但測試區圖型同樣為空，因此測試區不能單獨作為型別來源。
- 23 項的 `QualityCharacteristics.DefaultChartTypeId` 為啟用中的 `I_MR`；其餘 CN 過硫酸鈉 2 項雖無預設值，但正式同特性／同單位／同控制範圍的既有項目唯一為 `I_MR`。
- 所有可用正式同類型證據均為 `I_MR`，未發現 `XBAR_R` 或其他候選衝突。

## 自動化測試

- `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter "ChemicalChartTypeRepairPlannerTests|OctoberProductionChemicalCorrectionPlannerTests" --no-restore`
- 結果：11 passed、0 failed、0 skipped。
- 覆蓋：唯一 I-MR、已正確冪等、既有其他圖型拒絕、缺候選拒絕、混合候選與重複 PPC 拒絕。
- `dotnet build tools/OctoberProductionChemicalCorrection/OctoberProductionChemicalCorrection.csproj --no-restore`：成功，0 error；只有既有 NuGet 弱點來源無法連線警告。

## 受控工具預演

- 正式唯讀 `--plan`：`ppcs=25; already=0; failedJobs=160`，與固定範圍一致。
- 未設定正式核准開關時執行 `--apply`：在備份或交易前由硬性防護拒絕，輸出 `EXPECTED_APPLY_GUARD_BLOCKED`；正式資料未異動。
- 工具另驗證設定環境、實際資料庫、伺服器、25／160／493固定數量；正式套用模式包含 `COPY_ONLY/CHECKSUM` 與 `RESTORE VERIFYONLY`，且只允許空值轉啟用中的 `I_MR`。

## 資料與發布狀態

- 正式 `PMR_SPC_2026`：本輪只有唯讀查詢，未更新 PPC、工作、量測、公式、F 表或收據。
- 測試 `PMR_SPC_TEST`：本輪只有唯讀自然鍵比對，未更新。
- Web/API：沒有程式發布；不適用測試站 Web/API 連線驗證。
- 中央進度狀態仍為未完成，核心／SPC計數不變；因狀態未變，本輪不覆寫原進度活頁簿。

## 待辦／阻擋

- 受控工具、唯讀計畫及未授權拒絕已完成。正式 25 項主檔異動和既有160筆統計工作重跑仍需使用者另行明示授權；授權前不執行備份或寫入。
