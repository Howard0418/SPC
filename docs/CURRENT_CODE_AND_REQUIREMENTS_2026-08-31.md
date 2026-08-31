# MES SPC 現行程式碼與需求整理

> 盤點日期：2026-08-31
> 適用範圍：`D:\SPC` 內的 MES SPC Web、API、資料庫 migration 與維護工具
> 業務規則主基準：[`SPC_REQUIREMENTS_BASELINE_2026-08-07.md`](./SPC_REQUIREMENTS_BASELINE_2026-08-07.md)
> 已完成變更紀錄：[`CHANGELOG_CUSTOM.md`](../CHANGELOG_CUSTOM.md)

## 1. 系統目的與邊界

本系統負責產品、製程及藥液量測資料的接收、追溯、SPC 計算、異常判定與圖表查詢。PMR Portal 負責藥液量測輸入；TransFiles 負責 Excel 轉檔；兩者透過 SPC API 與本系統整合，但不屬於本儲存庫的前端畫面。

現行正式管制類型為 `PRODUCT`、`PROCESS`、`CHEM`。舊代碼只供相容轉換，不得作為新資料值。製程與品質特性是可重用主檔；管制類型、單位、規格、圖表設定及線別／槽體關聯均由 SPC 管制項目負責。

## 2. 程式架構

| 區域 | 技術與入口 | 主要責任 |
|---|---|---|
| Web | Vue 3、Vite、Tailwind、ECharts；`frontend/mes-spc-web/src` | 主檔維護、上傳預覽、SPC 管制圖、報表與系統設定 |
| API | ASP.NET Core 10；`backend/MesSpc.Api/Program.cs` | REST API、驗證授權、匯入、量測、SPC 計算與排程 |
| 資料層 | EF Core；`Infrastructure/Data/AppDbContext.cs` | SQL Server 為預設，保留 SQLite 支援；migration 位於 `Migrations` |
| SPC 引擎 | `backend/MesSpc.Api/SpcEngine` | I-MR、Xbar-R、Xbar-S、計數型圖表、能力指標及 WE 八大規則 |
| 維護工具 | `tools/ChemicalFormulaMigration` | 藥液公式盤點、驗證、指定規則同步與受控資料修補 |
| 測試 | `tests/MesSpc.Api.Tests`、`tests/MES.SPC.E2ETests` | API／領域測試及 Playwright 端對端流程 |

主要資料流程：

1. Portal 或匯入檔送出量測資料與管制項目識別。
2. API 驗證主檔、單位及必要欄位，建立或更新 `VariableMeasurements`。
3. `SpcService` 依管制項目設定計算統計量、規格結果及 WE 規則異常。
4. Web 查詢 API，顯示量測、管制界線、規格界線、能力指標及警示。

## 3. 現行有效需求

### 3.1 主檔與顯示

- 系統代號是 API、資料庫關聯與稽核識別，一般使用者畫面以中英文名稱顯示。
- 線別、槽體、分析項目依各層 `SequenceNo` 排序，同序時以中文名稱穩定排序。
- 同一品質特性可由不同管制項目設定不同單位及規格。
- 所有管制項目固定套用全系統 Western Electric 八大規則，不提供項目專屬開關。

### 3.2 藥液量測與追溯

- Portal 線別選單只列出具有啟用中 `CHEM` 管制項目的線別。
- 每筆量測以 `ppcId` 反查製程、線別、槽體、品質特性及單位；找不到主檔時顯示 `-`，不得以系統代號替代正常名稱。
- 藥液量測須保留主要滴定值、第二滴定值及兩者的複驗值；單滴定與歷史資料仍須相容。
- `AdjustAmount` 是最長 1000 字元的文字，可保存「DP333：33 L」或「排液：254 L 補水：254 L」等複合結果；API 需去除頭尾空白，空白值存為 `null`。
- 既有數值 `AdjustAmount` 透過 `StoreAdjustmentAmountAsText` migration 轉為文字欄位。

### 3.3 匯入與資料安全

- 匯入前必須預覽並分層驗證線別、槽體、品質特性、單位及 SPC 管制項目。
- 未知線別、空白量測值、名稱衝突或缺少主檔是阻擋錯誤，不得直接正式匯入。
- 上傳以內容雜湊防止重複批次；正式匯入採 upsert 並保留批次追溯。
- 資料庫套用型工具預設 dry-run；正式套用必須同時提供 `--apply` 與相符的 `--confirm-database=<資料庫名>`。
- 特定公式修補必須唯一命中目標、先驗證代表性邊界值，並在寫入前輸出可回復的欄位備份。

### 3.4 管制圖

- 查詢明細需分開呈現管制界線（UCL／CL／LCL）與規格界線（USL／Target／LSL）。
- 規格值優先使用本次查詢結果，缺值才回退至管制項目主檔。
- 明細摘要的管制界線與規格界線顯示至小數點後兩位；計算精度與圖表資料不得因此改變。
- 主要查詢按鈕用語統一為「查詢總表」。

## 4. 本次工作區功能整理

| 功能 | 程式位置 | 狀態 |
|---|---|---|
| 調整量文字化 | Entity、API DTO、上傳服務、SPC DTO、EF migration | 已完成程式與 schema 對齊 |
| 雙滴定值回查 | `ManualMeasurementsV1Controller` | 已回傳第二滴定值及其複驗值 |
| 管制圖規格摘要 | `SpcChartView.vue` | 已新增 USL／Target／LSL 並統一兩位小數 |
| 藥液規則單筆執行 | `ChemicalFormulaMigration` | 已支援 `--rule=<規則 ID>` |
| C5 清潔槽修補 | `ChemicalFormulaMigration` | 已加入唯一性、邊界驗證、dry-run、確認與 rollback 檔 |
| 依賴補齊 | API 專案檔 | 已明列 OpenAPI 與 SQLite native bundle 依賴 |

## 5. 建置、測試與操作

```powershell
# 後端與測試
dotnet restore .\MES_SPC.slnx
dotnet build .\MES_SPC.slnx --no-restore
dotnet test .\tests\MesSpc.Api.Tests\MesSpc.Api.Tests.csproj --no-build

# 前端
Set-Location .\frontend\mes-spc-web
npm install
npm run build
```

藥液維護工具應先 dry-run；以下僅示意參數，不代表授權操作任何環境：

```powershell
dotnet run --project .\tools\ChemicalFormulaMigration -- --rule=C3:7
dotnet run --project .\tools\ChemicalFormulaMigration -- --patch-c5-cleaner
```

## 6. 已知限制與後續事項

- 正式發布仍須依需求基準逐一執行後端 publish、前端 build，並在目標網址驗證；本文件與 Git commit 不等同正式部署。
- SQLite 與 SQL Server migration 分流仍須持續維護及驗證。
- 舊測試仍包含項目專屬規則的歷史情境，後續應依「全系統 WE 八大規則」重寫，而非擴充舊行為。
- 名稱完全相同時的主檔警告／唯一性檢查、Portal 英文名稱與 `SequenceNo` 完整傳遞仍是待辦。
- 套用資料庫 migration、公式修補、正式匯入或清除資料前，必須另行確認目標環境、影響筆數與備份。
