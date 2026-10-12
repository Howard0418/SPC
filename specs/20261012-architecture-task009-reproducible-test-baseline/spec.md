# 小型變更：測試與可重建基線
- 功能 ID：ARCH-TASK-009-REPRODUCIBLE-TEST-BASELINE-20261012
- 版本：1
- 狀態：已完成
- 專案／授權依據／需求基準：SPC 架構改善 TASK-009；依 `TODO.md` 架構改善排序與 `docs/requirements.md` 現行需求索引。

## 問題、預期行為與範圍
- 現況：後端測試、後端 build、前端 build 與測試站 smoke 目前多由人工逐次執行；近期多次平行執行 `dotnet test` 與 `dotnet build` 會搶同一個 Debug DLL 鎖，造成非功能性失敗。
- 問題：缺少一個可重跑、順序固定、可選 smoke 的基線驗證入口，導致每個小工作驗證命令略有差異，也較難判斷失敗是否來自程式或執行方式。
- 預期：提供一支 PowerShell 基線驗證腳本，依序執行後端測試、後端 build、前端 build 與可選 URL smoke；任何步驟失敗即停止並回傳非 0 exit code。
- 範圍：只新增工具與文件；不修改應用程式邏輯、不發布、不清資料、不下載新套件。

## 需求與驗收
- R-001：基線腳本需可從 repo root 執行，預設跑後端測試、後端 build 與前端 build。
- R-002：基線腳本需支援 `-TestFilter` 指定後端測試 filter，避免小工作驗證時跑非必要測試。
- R-003：基線腳本需支援 `-SmokeUrl`，逐一檢查 HTTP 狀態碼 2xx～3xx 視為通過。
- R-004：基線腳本需支援 `-SkipFrontend` 與 `-SkipTests`，讓後端-only 或文件型小工作可明確縮小驗證範圍。
- AC-001：Given 指定 `-TestFilter UserAccessTests -SkipFrontend`，When 執行基線腳本，Then 後端測試與 build 依序通過且不執行前端 build。
- AC-002：Given 指定 `-SkipTests -SkipFrontend -SmokeUrl http://172.16.110.27:8084/api/health`，When 執行基線腳本，Then 後端 build 與 smoke 通過。
- AC-003：Given smoke URL 回非 2xx～3xx 或連線失敗，When 執行基線腳本，Then 腳本回非 0 exit code 並顯示失敗 URL。

## 計畫與任務
- 受影響檔案：
  - `tools/testing/Invoke-SpcBaseline.ps1`
  - `specs/20261012-architecture-task009-reproducible-test-baseline/spec.md`
  - `specs/20261012-architecture-task009-reproducible-test-baseline/verification.md`
  - `docs/requirements.md`
  - `CHANGELOG_CUSTOM.md`
  - `TODO.md`
  - `ai_docs/10_change_log.md`
- [x] T-001：新增基線驗證腳本與參數。
- [x] T-002：執行後端測試/build 與 smoke 驗證腳本可重跑。
- [x] T-003：同步有效需求、變更紀錄、TODO 與驗證結果。

## 驗證
- Happy Path：指定後端測試 filter 與 skip frontend 時，測試/build 通過。
- Boundary Case：只跑 build + smoke 時，能驗證既有測試站 health。
- Invalid Input：失敗 URL 會使腳本非 0 結束。
- Regression Risk：本次不改應用程式，發布不適用。
- 實際結果及證據：基線腳本兩種模式驗證通過；詳見 `verification.md`。
- 發布狀態：工具與文件型小工作，發布不適用。
- 限制或未決問題：本次不建立 CI pipeline、不重整全部測試資料，也不清理既有未追蹤證據檔。
