# 小型變更：製程總覽 OOS 口徑收斂
- 功能 ID：SPC-20261005-PROCESS-SUMMARY-OOS
- 版本：1
- 狀態：完成
- 專案／授權依據／需求基準：使用者於 2026-10-05 回報製程總覽 OOS 也有不一致，需確認並修正；延續 `SPC-20261005-PROCESS-TOTAL-COUNT`，且不得影響藥液功能與資料計算。

## 問題、預期行為與範圍
- 問題：製程總覽 `OosCount` 目前仍以 raw data 的 `IsOutOfSpec` 計算；製圖畫面則以 `chartData.points` 的 `outOfSpec` 等點位旗標呈現。Xbar 類製程圖會將多筆 raw data 聚合為一個管制點，因此總覽 OOS 可能與製圖點位不一致。
- 預期行為：製程 `PROCESS` 總覽的 OOS/OOC 及百分比使用製圖管制點口徑；藥液 `CHEM` 維持 raw data 口徑。
- 範圍：僅調整後端總覽統計；不修改藥液公式、藥液計算、資料庫結構、前端畫面與 SPC 統計公式。

## 需求與驗收
- R-001：製程總覽 `OosCount` 使用 `chartData.points` 中 `outOfSpec=true` 的點數。
- R-002：製程總覽 `OocCount` 使用 `chartData.points` 中 `outOfControl=true` 或 `violatedRules` 非空的點數。
- R-003：製程總覽百分比分母使用製程管制點數；藥液仍使用 raw data 筆數。
- AC-001：後端 build 通過。
- AC-002：測試站 `/api/version` 回應 `environment=test`。
- AC-003：程式差異可確認只影響製程總覽 OOS/OOC 統計與文件紀錄。

## 計畫與任務
- 受影響檔案：`backend/MesSpc.Api/Services/SpcService.cs`、`TODO.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`、本規格。
- [x] T-001：實作 R-001/R-003。
- [x] T-002：驗證 AC-001/AC-003。
- [x] T-003：同步有效需求與變更紀錄。

## 驗證
- 方式、環境：`dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false`，發布 SPC 測試站 backend 後檢查 `/api/version`。
- 實際結果及證據：後端 build 成功，0 warnings / 0 errors；`http://172.16.110.27:8081/api/version` 回 `{"version":"0.1.59","environment":"test"}`。
- 發布狀態：已發布 SPC 測試站 backend；備份 `release/test/backend.backup-process-oos-scope-193639`。正式站未發布。
- 限制或未決問題：人工畫面仍需使用者在測試站確認指定製程項目總覽 OOS/OOC 與製圖點位一致。
