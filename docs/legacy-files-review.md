# 舊檔案與工具腳本清理建議

日期：2026-07-10

## 結論

第二階段盤點後，專案裡確實還有多個改版過程留下的暫存工具、舊測試、舊 Migration 備份與硬編碼路徑。

這份文件先提出建議；2026-07-10 已完成第一批低風險刪除。

## 已移除

這些檔案不是正式流程的一部分，或內容已明確標示不再需要。

| 檔案 | 判斷 |
| --- | --- |
| `read_db_groups.js` | 內容只有 `Scratch file, not needed anymore` |
| `read_excel_structure.js` | 內容只有 `Scratch file, not needed anymore` |
| `restore_preview.bat` | 內容只有 `Scratch file, not needed anymore` |
| `restore_all.bat` | 會執行 `git checkout --` 與刪除 config，屬於高風險舊還原腳本 |
| `backend/MesSpc.Api/build_output.bat` | 只是把 build 輸出導到 log，已被標準 build 指令取代 |
| `backend/MesSpc.Api/run_migration.bat` | 寫死 `AddRecheckAdjustFields` migration 名稱，容易誤用 |
| `tools/read_excel.js` | 寫死個人桌面路徑，屬於一次性 Excel 探查腳本 |

## 已移除或待改成安全範本

這些檔案含有環境專屬路徑或明文資料庫連線資訊。若要保留用途，應改成 `.example` 或讀取環境變數。

| 檔案 | 問題 | 建議 |
| --- | --- | --- |
| `dump_db_scratch.ps1` | 含 SQL Server IP、資料庫名稱、帳號與密碼 | 從主線移除 |
| `scratch_inspect_db.ps1` | 含 SQL Server IP、資料庫名稱、帳號與密碼 | 從主線移除 |
| `tests/MesSpc.Api.Tests/AnalyzeExcelTest.cs` | 含硬編碼 DB 連線字串與個人 scratch 輸出路徑 | 若保留測試專案，改為環境變數與 Skip |
| `backend/MesSpc.Api.Tests/AnalyzeExcelTest.cs` | 含硬編碼 `D:\SPC\sample-data\SPC系統建置.xlsx` 與個人 scratch 輸出路徑 | 已從正式測試專案移除 |

## 建議保留但整理

這些檔案仍可能有用途，但目前需要命名或位置整理。

| 檔案 | 判斷 | 建議 |
| --- | --- | --- |
| `publish.ps1` | 可用的一鍵發佈腳本，但會自動 `npm version patch`，可能造成不預期版本異動 | 保留前先改成需參數確認才遞增版本 |
| `publish.bat` | 舊版發佈腳本，可用但與 `publish.ps1` 功能重疊 | 若保留，建議只保留一個正式發佈入口 |
| `run_tests.ps1` | 指向 `tests/MesSpc.Api.Tests`，不是目前 `backend/MesSpc.slnx` 使用的測試專案 | 更新路徑或移除 |
| `scripts/run-e2e-ui.ps1` | 指向 E2E 測試專案，仍有用途 | 保留 |
| `scripts/SmartDevOpsWatcher.ps1` | 寫死舊桌面專案路徑 | 若仍要用，改成相對路徑或參數 |

## 需要確認的資料夾

| 資料夾 | 判斷 | 建議 |
| --- | --- | --- |
| `MigrationsSqlite_bkp/**` | 舊 SQLite migration 備份；正式 migration 已在 `backend/MesSpc.Api/Migrations/**` | 若沒有回復 SQLite 需求，移除 |
| `sample-data/**` | 目前只有 3 個檔案，但多處引用不存在的 `SPC系統建置.xlsx` 與 `批次轉換結果_藥液_計量型匯入.xlsx` | 保留正式範本，移除或補齊失效引用 |
| `ai_docs/**` | 仍被 `agent_skills`、`prompts` 與文件規則引用 | 暫時保留，但後續要更新過時內容 |
| `tests/MesSpc.Api.Tests/**` | 根目錄 `MES_SPC.slnx` 有引用，但 `backend/MesSpc.slnx` 沒有；內含較多整合測試 | 不直接刪，先決定是否併回正式 solution |
| `backend/MesSpc.Api.Tests/**` | `backend/MesSpc.slnx` 正式引用；目前 build 會跑到 | 保留，但移除一次性 `AnalyzeExcelTest.cs` |

## 額外風險

盤點時發現除了暫存腳本外，正式設定與部分程式仍有環境專屬路徑或明文連線字串：

- `backend/MesSpc.Api/appsettings.json`
- `backend/MesSpc.Api/appsettings.Development.json`
- `backend/MesSpc.Api/Controllers/SettingsController.cs`
- `backend/MesSpc.Api/Controllers/AlertDashboardController.cs`
- `backend/MesSpc.Api/Services/SmtpEmailNotificationService.cs`
- `frontend/mes-spc-web/src/views/AlertsView.vue`
- `frontend/mes-spc-web/spc_manual_prompt_guide.html`

這些不一定是「多餘檔案」，但建議列為下一階段安全與環境設定整理。

## 本次已處理

已刪除以下低風險檔案：

- `read_db_groups.js`
- `read_excel_structure.js`
- `restore_preview.bat`
- `restore_all.bat`
- `dump_db_scratch.ps1`
- `scratch_inspect_db.ps1`
- `tools/read_excel.js`
- `backend/MesSpc.Api/build_output.bat`
- `backend/MesSpc.Api/run_migration.bat`
- `backend/MesSpc.Api.Tests/AnalyzeExcelTest.cs`

## 建議下一步

第一批低風險移除已完成。下一步建議處理安全與環境設定：

1. 移除或環境變數化 `appsettings*.json` 的明文資料庫連線字串。
2. 整理 `SettingsController`、`AlertDashboardController`、`SmtpEmailNotificationService` 的個人路徑預設值。
3. 檢查 `backend/MesSpc.Api.Tests/UnitTest1.cs` 的硬編碼個人 Excel 路徑。
4. 判斷 `tests/MesSpc.Api.Tests/**` 是否併回正式 solution 或封存。
5. 暫時保留 `ai_docs`、`sample-data`、E2E 測試與發佈腳本，等下一輪再整理。
