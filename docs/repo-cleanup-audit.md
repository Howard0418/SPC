# 專案多餘檔案盤點報告

日期：2026-07-09

## 結論

目前專案確實有偏多不應進入版本控管的檔案。最大宗不是業務程式碼，而是相依套件、前後端建置產物、IDE 暫存、發佈資料夾、測試產物與工具暫存。

這份報告只做盤點與建議，不直接刪除檔案。

## 已確認的主要項目

| 類型 | 目前 Git 追蹤數量 | 建議 |
| --- | ---: | --- |
| `node_modules` | 11204 | 從版本控管移除，保留 `package-lock.json` |
| `bin` | 2425 | 從版本控管移除 |
| `obj` | 210 | 從版本控管移除 |
| `dist` | 4436 | 若不是用 Git 版控部署成品，應移除 |
| `publish` | 353 | 若不是用 Git 版控部署成品，應移除 |
| `.vs` / `.dotnet` / `.dotnet_home` | 50 | 從版本控管移除 |
| `EmailOutbox` | 7 | 視為執行產物，建議移除 |
| `*.log` | 14 | 從版本控管移除 |
| `test-artifacts` | 599 | 需要確認是否仍作為測試基準資料 |
| `sample-data` | 3 | 需要確認是否為正式範本 |
| `MigrationsSqlite_bkp` | 7 | 需要確認是否仍有保留價值 |
| `ai_docs` | 36 | 可保留，但建議確認是否仍符合現況 |

## 可安全移出版本控管

以下項目通常不應由 Git 追蹤。建議做法是先加入 `.gitignore`，再用 `git rm --cached` 從版本控管移除，但不刪除本機實體檔案。

- `frontend/mes-spc-web/node_modules/**`
- `tools/node_modules/**`
- `**/bin/**`
- `**/obj/**`
- `.vs/**`
- `.dotnet/**`
- `.dotnet_home/**`
- `frontend/mes-spc-web/dist/**`
- `publish/**`
- `backend/**/publish/**`
- `EmailOutbox/**`
- `*.log`

## 需要你確認後再處理

這些檔案可能是舊版改版留下，也可能仍有對照、測試或交付用途。建議先確認用途再決定移除或封存。

- `sample-data/**`
- `test-artifacts/**`
- `MigrationsSqlite_bkp/**`
- `ai_docs/**`
- `dump_db_scratch.ps1`
- `scratch_inspect_db.ps1`
- `restore_all.bat`
- `restore_preview.bat`
- `read_db_groups.js`
- `read_excel_structure.js`
- `publish.ps1`
- `publish.bat`
- `run_tests.ps1`
- `backend/MesSpc.Api/run_migration.bat`

## 應保留

以下是目前仍屬於專案主要結構或可追溯文件的內容。

- `backend/MesSpc.Api/**`，排除 `bin`、`obj`、`publish`
- `backend/MesSpc.Api.Tests/**`，排除 `bin`、`obj`
- `backend/MesSpc.slnx`
- `backend/**/*.csproj`
- `frontend/mes-spc-web/src/**`
- `frontend/mes-spc-web/package.json`
- `frontend/mes-spc-web/package-lock.json`
- `frontend/mes-spc-web/vite.config.*`
- `docs/**`
- `database_rules/**`
- `backend_rules/**`
- `deployment_rules/**`

## 建議清理流程

1. 新增根目錄 `.gitignore`，先防止之後繼續把建置產物與套件加回來。
2. 只針對明確產物執行 `git rm --cached`，先從 Git 移除追蹤，但保留本機檔案。
3. 執行 `npm run build` 與 `dotnet build backend\MesSpc.slnx`，確認移除追蹤後仍可重建。
4. 再逐項確認 `sample-data`、`test-artifacts`、`ai_docs`、暫存腳本與備份 Migration 是否要封存或刪除。
5. 最後才考慮清理本機實體資料夾，例如 `node_modules`、`bin`、`obj`、`dist`、`publish`。

## 建議的 `.gitignore` 範圍

```gitignore
# Node / frontend
node_modules/
**/node_modules/
frontend/mes-spc-web/dist/
npm-debug.log*
yarn-debug.log*
yarn-error.log*

# .NET
**/bin/
**/obj/
**/publish/
publish/

# IDE / local runtime
.vs/
.vscode/
.dotnet/
.dotnet_home/

# Logs and generated output
*.log
EmailOutbox/
test-artifacts/

# OS
Thumbs.db
.DS_Store
```

## 我建議下一步

先執行第一段安全清理：新增 `.gitignore`，並將 `node_modules`、`bin`、`obj`、`dist`、`publish`、`.vs`、`.dotnet`、`EmailOutbox`、`*.log` 從 Git 追蹤移除。

這一步不會刪除本機檔案，只是讓版本庫回到比較乾淨、可維護的狀態。
