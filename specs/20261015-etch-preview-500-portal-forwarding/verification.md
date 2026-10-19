# 驗證紀錄
- 功能 ID：SPC-ETCH-PREVIEW-500-PORTAL-FORWARDING-20261015
- 狀態：DONE；2026-10-19 原始碼錯誤路徑自動化驗收通過

| 驗收 | 結果 |
|---|---|
| AC-001 | Portal `EtchStrictTests` 5 passed，涵蓋 503 安全診斷欄位轉送與無效 JSON fallback。 |
| AC-002 | 2026-10-19 本機 HTTPError 注入實際 TransFiles client/GUI 失敗分支，500/503 code/trace 出現在提示、操作日誌及 JSON LOG；通過。 |
| AC-003 | Portal API build 0 warnings / 0 errors；維持 preview-only 流程。 |

發布：2026-10-15 已發布 Portal 測試 API，遠端備份 `D:\PmrPortal\publish\portal-api.backup-etch-preview-forwarding-20261015-110036`；保留 `appsettings.json` 與 `web.config`。對外空白登入 smoke 回預期 HTTP 400。正式站未發布。

## 2026-10-19 自動化驗收證據
- 命令：`python tools/testing/verify_etch_preview_diagnostics.py --transfiles-root <TransFiles來源目錄>`，4 tests passed。
- 執行實際 `EtchImportClient.request/preview`、`ConversionGUI.import_complete_reports`、`write_preview_failure_log`；HTTP opener 注入 HTTPError，root.after/messagebox 以替身執行 callback 並捕捉提示，不建立 GUI 視窗。
- 500/503 分別保留 `ETCH_PREVIEW_SERVER_FAILURE`、`ETCH_PREVIEW_DATABASE_FAILURE` 與 `offline-trace-123`；JSON LOG、GUI 日誌與失敗提示皆可見。
- 每個案例只對第一筆 PT1 發送模擬預覽，不送第二筆 PT2；確認對話與 confirm 均未呼叫；真實 confirm 閘門也拒絕失敗的 preview。
- 400/409 保留可讀訊息；無效 JSON 的原始內容不外洩；機敏 token 片段在 JSON LOG、日誌與提示均遮罩；LOG `writtenToDatabase=false`。
- Portal 回歸命令：`dotnet test tests/PmrPortal.Etch.Tests/PmrPortal.Etch.Tests.csproj --no-restore --filter FullyQualifiedName~EtchStrictTests -p:UseSharedCompilation=false`，5 passed、0 failed、0 skipped。測試含安全欄位解析、無效 JSON fallback、25/50 點匯入/重試/衝突與正式環境拒絕；使用測試本機記憶體 SQLite。
- Happy Path：500/503 診斷保留通過；Boundary：首筆停止與確認閘門通過；Invalid Input：非 JSON fallback 通過；Regression Risk：400/409、遮罩與 Portal 既有行為回歸通過。
- 限制：此為來源 checkout 的自動化測試，未讓遠端 SPC 發生 500/503，未執行已打包 EXE 的人工故障視窗驗收；不將這些未執行項目描述為通過。成功路徑的兩站 130 份預覽/匯入另見前件紀錄。
- 無應用程式變更，不重跑完整 build、不打包、不重新發布；沿用 2026-10-15 Portal 測試 API 發布。正式站未操作。
- 原始 JSON/暫存測試輸出不提交；測試 LOG 在 TemporaryDirectory 結束後清理。文件與測試工具回復可 revert 本次提交。
