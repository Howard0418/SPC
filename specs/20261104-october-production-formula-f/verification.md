# 驗證紀錄

- 功能 ID：`SPC-CHEM-F-CONSISTENCY-1103-TASK-001-PROD-OCT`
- 規格版本：2
- 日期／環境：2026-11-04／production，`172.16.110.16`／`PMR_SPC_2026`
- 實作狀態：可確認範圍完成；6 個 PPC／日期衝突群組共 12 列隔離
- 驗證狀態：通過
- 發布狀態：無網站程式發布；正式 API 維持 `0.1.102`，Web／Portal 均未變更

## 正式結果

| 項目 | 計畫 | 首次套用 | 相同清單重送 |
|---|---:|---:|---:|
| 來源列 | 1211 | 1199 套用、12 隔離 | 同清單 |
| 正式 PPC | 80 | 80 | 0 新增 |
| 完整 F 快照 | 3 | 重用 1、建立 2 | 0 新增 |
| 公式歷史版本 | 81 | 建立 81 | 0 新增 |
| 未啟用排程修訂 | 80 | 建立 80 | 0 新增 |
| 日別有效區間 | 1039 | 建立 1039 | 0 新增 |
| Excel／引擎計算核對 | 1211 | 全部在 `1e-8` 容差內 | 1211 再驗 |

正式計數由 F 歷史 2→4、公式歷史 5→86、政策 0→80、修訂 0→80、有效區間 0→1039；所有政策 `IsEnabled=false`，故只建立十月不可變歷史與日期資料，不切換正式目前設定。

## 驗收結果

| 驗收 ID | 命令或操作 | 實際結果及證據 | 狀態 |
|---|---|---|---|
| AC-001 | production／DB／API guard | 實際 API `/api/health`、`/api/version` 均回 `production`／`0.1.102`；runner 只接受 `production`、指定主機與 `PMR_SPC_2026`，錯環境測試拒絕。 | 通過 |
| AC-002 | `COPY_ONLY + CHECKSUM`、`RESTORE VERIFYONLY WITH CHECKSUM` | `PMR_SPC_2026_pre_oct_formula_f_20261104-173405.bak` 建立且 checksum／verifyOnly 皆為 true；備份失敗不得進交易。 | 通過 |
| AC-003 | 正式自然鍵與同日衝突檢查 | 80 PPC 唯一映射；ST2 三項於 10/01、10/14 的 OPEN／MIDDLE 完整 F 不同，6 群組共 12 列整組隔離，其餘 1199 列套用。 | 通過（隔離） |
| AC-004 | 完整矩陣試算與 F 參照 | 1211 次來源公式／完整八格 F 計算均在 `1e-8` 內；十月 B3 `.99／.995／1.02`、B5 `.983／1` 依日保存，`F!B3` 等符號參照未展開為固定常數。 | 通過 |
| AC-005 | 套用後立即相同清單重送 | `VERIFIED_NO_CHANGE`；F、公式、修訂、區間新增皆 0，計數維持 4／86／80／1039。 | 通過 |
| AC-006 | 目前 PPC／啟用 F 指紋 | `currentPpcUnchanged=true`、`activeFUnchanged=true`；目前啟用 B3=.99 及現行公式主檔未被十月歷史倒寫。 | 通過 |
| AC-007 | 量測／上傳／憑證／統計／警報等保護指紋 | `protectedTablesUnchanged=true`、`measurementDataWritten=false`；量測、Upload、Receipt、Trace、Statistics、Alert 均未寫入。 | 通過 |

## 自動化測試與建置

- `OctoberProductionFormulaFPlannerTests`／`ImporterTests` 覆蓋完整 F、群組隔離、正式目標、語意冪等、目前主檔保持及異質排程拒絕。
- `OctoberProductionFormulaF*|ChemicalFTableServiceTests|ChemicalAnalysisFormulaVersionServiceTests|ChemicalFormulaScheduleTests` 共 34 項通過。
- Release build：0 warnings／0 errors。
- 正式寫入後由同一 SHA-256 manifest 重跑，回報 `F=0; formulas=0; schedules=0; periods=0; excluded=12`。

## 衝突隔離與使用者需補規則

以下日期同一 PPC 同時出現 OPEN／MIDDLE 不同完整 F 快照，現行一個 PPC／日期只能有一套排程，故不能猜測或只取其中一班：

| PPC | 線別／槽位／項目 | 日期 |
|---:|---|---|
| 2471 | ST2／TANK-000285／CHAR-000826／% | 2026-10-01、2026-10-14 |
| 2472 | ST2／TANK-000286／CHAR-000826／% | 2026-10-01、2026-10-14 |
| 2473 | ST2／ST2-酸洗／CHAR-000830／% | 2026-10-01、2026-10-14 |

後續需由使用者確認該日應採 OPEN、MIDDLE，或提供可區分班別／時間的有效規則；確認後只補這 6 個 PPC／日期群組，不重送已完成區間。

## 來源、限制與回復

- 使用者明示「不用預檢」。執行時即時來源分享路徑不可達，因此未做新的業務預檢；改用先前已核對且五個檔案 SHA-256、manifest `57fe52a0...b49b` 均吻合的唯讀證據。環境、DB、API、備份、來源 hash、交易、重複與保護資料護欄沒有省略。
- 本件只匯入十月濃度公式歷史與 F 歷史；4 項 NA、調整／調整量公式、11/03 目前版 87 項正式主檔、EXE、藥液與咬蝕量測不屬本次完成範圍。
- 回復優先依本件 before image／新增歷史 ID 受控處理；整庫還原前須先核對備份後的其他正式寫入。完整備份位於正式 SQL Server 備份目錄，已通過 checksum／VERIFYONLY。

## 進度表同步

原 `進度表.xlsx` 已備份後從原路徑更新：同一父工作第 265 列補記正式結果，11/04 事件列新增狀態及第 60 個原生里程碑；核心未完成 45、涉及 SPC 22、完成 189、人工 11／待核銷 4 均不變。只有 14 個指定儲存格及 1 個新圖形變更，原 59 圖形、合併區、凍結窗格與列印範圍保持；輸出 SHA-256 `1bb462ca...efd08`。Artifact 重開／公式錯誤 0、OpenXML 0 錯誤、LibreOffice 14 頁 A3 列印通過；Microsoft Excel 人工開啟未執行。回復檔：`進度表_十月公式F正式前_20261104-174229.xlsx`。
