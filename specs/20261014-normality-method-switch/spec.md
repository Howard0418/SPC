# 小型變更：SPC 常態檢定方法切換

日期：2026-10-14
狀態：DONE
主系統：SPC
相依系統：無
資料庫影響：無
發布影響：需發布 SPC 測試站 backend/frontend；正式站另行授權
Rollback：還原本次後端、前端與文件提交，重新發布 backend/frontend

## Requirement

使用者希望可在同一筆 SPC raw data 下切換常態檢定方法，比對現行 Adjusted Jarque-Bera 與 Anderson-Darling 的 p-value 與判定結果，方便確認與 Minitab 的差異。

## Scope

- 後端 `/api/v1/spc/chart` 支援 `normalityMethod` 查詢參數。
- 支援方法：`AJB` / `AdjustedJarqueBera`、`AD` / `AndersonDarling`。
- 預設維持 Adjusted Jarque-Bera，避免既有查詢結果突然改變。
- 前端 SPC 管制圖與量測趨勢圖新增「常態檢定方法」選項，查詢時帶入後端。
- 不修改資料庫 schema，不改 raw data、不改管制界線與能力指標。

## Acceptance Criteria

- AC-001：未指定方法時，常態檢定仍回傳 `Adjusted Jarque-Bera`。
- AC-002：指定 Anderson-Darling 時，後端回傳 `TestName = Anderson-Darling`、p-value 介於 0 到 1，且依 0.05 判定常態。
- AC-003：SPC 管制圖與量測趨勢圖可切換常態檢定方法，重新查詢後畫面顯示對應方法名稱與 p-value。
- AC-004：樣本數不足或無變異資料仍回傳既有無法檢定訊息，不發生例外。

## Verification

- 後端單元測試：`dotnet test tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --filter NormalityTest --no-restore -p:UseSharedCompilation=false`，8 passed。
- 後端 build：`dotnet build backend\MesSpc.Api\MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`，0 warnings / 0 errors。
- 前端 build：`npm run build`，通過；既有 chunk size warning 可接受。
