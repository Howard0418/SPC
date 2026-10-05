# 驗證紀錄
- 功能 ID：20260930-particle-monitoring
- 規格版本：6
- 日期／環境：2026-10-01 / 本機開發環境
- 實作狀態：完成；資料模型、匯入、查詢、C/U-chart、TransFiles 及前端工作台均完成
- 驗證狀態：Particle 後端、TransFiles、前端建置、測試庫 schema、實際畫面與測試站 smoke 通過
- 發布狀態：SPC backend/frontend 已發布測試站；正式站未發布

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001～AC-008 | 整體功能驗收 | Long Format、preview/confirm、查詢、比較、C/U-chart 與 UI 可用 | T-009～T-015 完成，分層測試及測試站 smoke 通過 | 通過 |
| AC-002／AC-003／AC-005 | `dotnet test tests/MesSpc.Api.Tests/MesSpc.Api.Tests.csproj --filter ParticleMeasurementModelTests --no-restore -p:UseSharedCompilation=false` | Long Format 欄位、索引、冪等鍵與外鍵符合規格 | 1 passed | 通過 |
| T-009 | `dotnet build backend/MesSpc.Api/MesSpc.Api.csproj --no-restore -p:UseSharedCompilation=false` | 後端可編譯 | 0 warnings／0 errors | 通過 |

## 未執行項目、限制及後續
- 已修改程式與產生 migration；未將 migration 套用至任何資料庫，未發布。
- 已完成唯讀盤點既有 DUST 資料入口與相容策略；未連線資料庫查實際資料列。
- 2026-10-01 使用者已確認草稿方向；Particle DB schema、欄位型別、查詢索引及來源儲存格冪等鍵已完成設計。
- 已完成 Excel 橫向轉 Long Format、Particle preview／confirm、錯誤碼與重複處理契約設計；文件內容與 T-005 狀態一致。
- 已完成原始量測查詢、趨勢與位置比較 API 契約；文件內容與 T-006 狀態一致。
- 已完成 C／U／I-MR 評估；原第一版 C-chart 已依後續確認擴充為 C/U 可選，I-MR 仍不自動 fallback。
- 已記錄實作前必測風險：bigint Count、同時間重測、20 點門檻、序列隔離及規格／管制線分離。
- 2026-10-01 使用者已確認進入實作；T-009 已完成，測試 API 啟動時已自動套用 migration。
- migration scope 以文字檢查確認不含 `ChemicalFTable`、`ControlLimitSegments` 或 `Calibration` 操作。
- Release publish 至 `release-staging/particle-model-20261001/backend-publish` 成功；測試站備份為同目錄 `backend-backup`，保留 appsettings／web.config／logs／uploads／private-data。
- 測試站部署 DLL 與 staging SHA-256 相同；`/api/version` 回 `0.1.59`、`environment=test`，`/health` HTTP 200，`app_offline.htm` 已移除。
- 測試庫唯讀確認 `ParticleMeasurements`、`CK_ParticleMeasurements_Count_NonNegative` 與 `20261001002307_AddParticleMeasurements` history 均存在。
- T-010：Particle upload 與模型測試共 3 passed；後端 build 0 warnings／0 errors。
- T-010 已發布測試 backend，備份 `release-staging/particle-upload-20261001/backend-backup`；DLL hash 一致、version／health 通過、新 preview endpoint 未登入回 401。
- 下一步實作 Particle 原始量測、趨勢與位置比較 API。
- T-011：Particle 相關測試 5 passed；後端 build 0 warnings／0 errors。
- T-011 測試站備份 `release-staging/particle-query-20261001/backend-backup`；DLL hash 一致、version／health 通過、trend 未登入回 401。
- 下一步實作 Particle C-chart adapter／SPC API。
- T-012：Particle 相關測試 9 passed；後端 build 0 warnings／0 errors。
- 驗證涵蓋 20 點門檻、LCL=0、同時間重測、bigint 精度拒絕及 sequence 隔離。
- T-012 測試站備份 `release-staging/particle-spc-20261001/backend-backup`；DLL hash 一致、version／health 通過、SPC endpoint 未登入回 401。
- 下一步調整 TransFiles 產生 Particle Long Format preview payload。
- T-012A：C/U Particle 相關測試 12 passed；後端 build 0 warnings／0 errors。
- `AddParticleSamplingVolumeUnit` migration 僅新增 nullable nvarchar(32) 單欄；測試庫欄位與 history 唯讀確認各 1。
- T-012A 測試站備份 `release-staging/particle-u-chart-20261001/backend-backup`；DLL hash、version、health 與 U-chart 未登入 401 smoke 通過。
- T-013：TransFiles 專屬與上傳回歸 39 passed；完整 discover 44 passed／2 failed，2 項為既有範例 Excel fixture 缺失。未打包 EXE。
- T-014／T-015：`npm run build:test` 通過；新工作台已發布測試站前端，備份 `release-staging/particle-frontend-20261001-103955/frontend-backup`。
- 最終回歸：Particle 後端 12 passed、後端 build 0 warnings／0 errors、TransFiles 39 passed、前端 build 通過。
- 本機免登入實際畫面確認桌面控制列、C/U 選擇、警示、圖表容器與原始資料表無重疊；Playwright runner 兩次未正常結束，故未列為通過。
- 最終測試站 smoke：frontend build/deploy index hash 一致，`/particle-monitoring` 200，新資產 `index-DpuHJjWg.js`；API health 200、version `0.1.59/test`。
- 測試站瀏覽器依既有 SSO 導至 Portal 登入；未持有測試帳密，登入後串接資料的人工驗收未執行。正式站未發布。
- 使用者後續授權打包與發布；TransFiles `2026.10.01.1` 主 EXE 已備份更新，dist／主程式 SHA-256 一致，GUI 啟動與版本標題驗證通過。
- 完整人工驗收步驟與預期結果見 [acceptance-guide.md](acceptance-guide.md)。

## 基準與變更紀錄更新位置
- 已更新 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、任務與進度紀錄。
