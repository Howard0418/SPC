# FORMULA-IMPORT-003 驗證

日期：2026-10-31

## 完成內容

- `scheduled_import.py` 對每一筆藥液來源列，先以公式快照比對再判斷量測業務鍵是否重複。
- 新版公式、規格、範圍、計算結果或 F 值會在 `chemicalVersionHistory` 追加不可變事件，記錄前後版本、差異欄位、來源、工作表與來源列。
- 相同量測業務鍵仍為 `skipped_duplicate`，不會因版本稽核而重新變成可寫入資料。
- 每次執行 JSON 報告新增 `audit`：目標環境、執行者、confirm 授權來源（如有）、是否請求寫入、是否啟用外部呼叫與公式版本事件摘要。報告不輸出權杖、密碼或連線字串。

## BDD 結果

| 情境 | 結果 |
|---|---|
| 同一量測鍵、F 值由 0.990 改為 0.995 | 通過：第二列為 `skipped_duplicate`，版本歷史依序為 `new`、`changed`。 |
| dry-run／test | 通過：回傳 `audit.requestedBy`、`writeRequested=false`、`externalCallEnabled=false` 與一筆新版本事件。 |
| 正式 confirm 防護與既有環境白名單 | 通過：既有測試保持通過。 |

## 自動化驗證

| 指令 | 結果 |
|---|---|
| `python -m unittest test_scheduled_import.py test_chemical_versioning.py test_chemical_direct_writer.py` | 23 passed |
| `python -m unittest`（排除兩個缺少既有範例 Excel 的測試） | 108 passed |

完整探索測試共 110 項，其中 `test_0910_conversion`、`test_batch_conversion` 各因工作區缺少既有必要樣本 `2026-4-24-0910.xlsx` 失敗，與本件無關；未新增忽略規則或偽造樣本檔。

## 發布與回復

- TransFiles：未打包、未發布；本件只完成本機程式與測試，未呼叫 preview／prepare／confirm、未寫入測試或正式資料庫。
- SPC／Portal：未修改、未發布。
- 回復：還原 `scheduled_import.py`、`test_scheduled_import.py` 與本規格；版本稽核狀態僅寫入使用者實際執行排程時指定的 state 檔，本次測試皆使用暫存資料夾。
