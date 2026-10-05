# 小型變更：製程總覽資料數口徑收斂
- 功能 ID：SPC-20261005-PROCESS-TOTAL-COUNT
- 版本：1
- 狀態：完成
- 專案／授權依據／需求基準：使用者於 2026-10-05 確認製程總覽匯入資料數需與製圖管制點數一致，且不得影響藥液功能與資料計算；依 `docs/requirements.md`、`CHANGELOG_CUSTOM.md` 既有 SPC 測試問題修正延伸。

## 問題、預期行為與範圍
- 問題：前次為避免影響藥液，將總覽 `TotalCount` 管制點數邏輯限制為 `PROC`，但目前系統標準製程代碼為 `PROCESS`，導致製程總覽仍可能回退 raw data 筆數，與製圖 `chartData.points` 管制點數不一致。
- 預期行為：只有製程 `PROCESS` 總覽 `匯入資料數` 使用管制圖點數；藥液 `CHEM` 維持 raw data 筆數。
- 範圍：僅調整後端總覽資料數判斷與文件紀錄；不調整藥液計算、資料庫結構、前端畫面與 SPC 統計公式。

## 需求與驗收
- R-001：製程總覽 `TotalCount` 判斷需使用標準化後的 control scope，支援既有 `PROC` 別名轉為 `PROCESS`。
- R-002：藥液 `CHEM` 的 `TotalCount` 仍使用 raw data 筆數，不套用製程管制點數邏輯。
- AC-001：後端 build 通過。
- AC-002：測試站 `/api/version` 回應 `environment=test`。
- AC-003：程式差異可確認只影響製程總覽 `TotalCount` 判斷與文件紀錄。

## 計畫與任務
- 受影響檔案：`backend/MesSpc.Api/Services/SpcService.cs`、`TODO.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`、本規格。
- [x] T-001：實作 R-001/R-002。
- [x] T-002：驗證 AC-001/AC-002/AC-003。
- [x] T-003：同步有效需求與變更紀錄。

## 驗證
- 方式、環境：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`，發布 SPC 測試站 backend 後檢查 `/api/version`。
- 實際結果及證據：後端 build 成功，0 warnings / 0 errors；`http://172.16.110.27:8081/api/version` 回 `{"version":"0.1.59","environment":"test"}`。
- 發布狀態：已發布 SPC 測試站 backend；備份 `release/test/backend.backup-process-total-count-scope-170030`。正式站未發布。
- 限制或未決問題：人工畫面仍需使用者在測試站確認指定製程項目總覽與製圖數字一致。
