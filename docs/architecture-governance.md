# SPC 架構治理基線

更新日期：2026-10-12

## 目的

本文件定義 SPC 大型模組拆分的治理入口。大型模組不得在未建立規格、測試與 rollback 說明時一次性大拆；每次拆分只處理一個責任邊界，並先用 `tools/testing/Invoke-SpcBaseline.ps1` 與相關測試建立可重跑基線。

## 盤點工具

執行：

```powershell
pwsh -NoProfile -File tools\architecture\Get-LargeModules.ps1 -Threshold 700 -Top 20
```

用途：
- 列出 `backend/MesSpc.Api` 與 `frontend/mes-spc-web/src` 內 `.cs`、`.vue`、`.ts`、`.js` 檔案行數。
- 超過門檻時回傳非 0 exit code，供人工檢查或未來 CI 使用。
- 需要暫時通過盤點時，可提高 `-Threshold`，但不得把高門檻當成完成拆分。

## 拆分候選

| 模組 | 目前責任 | 建議第一步 |
| --- | --- | --- |
| `backend/MesSpc.Api/Controllers/SpcController.cs` | 管制圖查詢、點位排除/備註、原始資料匯出、部分設定寫入 | 先按 API 群組拆出點位互動 controller 或查詢 DTO mapper；不得同時改計算邏輯 |
| `backend/MesSpc.Api/Services/UploadService.cs` | 匯入 staging、驗證、確認寫入、主檔補建、Portal 日報 audit | 先抽出 row validation 或 duplicate key 判斷服務；不得同時改 Confirm 寫入口徑 |
| `backend/MesSpc.Api/Services/SpcService.cs` | 查詢、圖表組裝、SPC 計算協調、警報寄送 | 先抽出查詢/DTO 組裝或警報寄送協調；不得同時改計算引擎 |
| `backend/MesSpc.Api/Controllers/ManualMeasurementsV1Controller.cs` | 手動日報查詢、建立、更新、刪除、calendar 與最新資料查詢 | 先抽出日報 key/班別階段解析或 delete audit；不得同時改匯入口徑 |

## 拆分規則

- 每次只拆一個責任邊界，並建立對應 `specs/`。
- 涉及計算、匯入、資料寫入或權限時，先補自動化測試，再改核心邏輯。
- 拆分後 public API response shape 必須保持相容；若要改 contract，另立功能需求。
- 不以「移動檔案」本身視為完成；必須降低重複、改善可測性或收斂責任邊界。
- 若同時需要 schema migration、正式資料修補或 IIS/發布變更，拆分工作需延後或拆成獨立小工作。

## 最低驗證

| 類型 | 最低驗證 |
| --- | --- |
| Controller 拆分 | 對應 controller tests 或 HTTP tests；`dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` |
| 匯入/寫入服務拆分 | 相關 service tests；至少涵蓋 happy path、invalid input、duplicate/regression |
| SPC 計算協調拆分 | 相關 calculation/chart tests；不得只靠 build |
| 前端大型 view 拆分 | `npm run build`，並檢查主要工作流畫面入口 |
| 純治理/工具變更 | 腳本或文件檢查即可，發布不適用 |

## 後續建議順序

1. `SpcController` 點位互動 API：範圍小、已有點位排除/備註測試。
2. `UploadService` row validation：風險高但可用 service tests 鎖行為。
3. `ManualMeasurementsV1Controller` 日報 key/班別階段解析：需先補覆蓋刪除與更新路徑。
4. `SpcService` 查詢/DTO 組裝：最後做，因牽涉圖表與計算邊界。
