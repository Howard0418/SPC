# FORMULA-IMPORT-005 驗證紀錄（部分完成）

日期：2026-11-01

## 已完成

- GUI 日報捷徑與 `scheduled_import.py` 的藥液／咬蝕量預設來源，已一致改為 `\\172.16.119.200\LineData`；使用者原本可自行選擇的路徑功能不變。
- `Test-Path` 實測藥液來源可讀，偵測到 801 個 `.xlsx`。
- `2026-10-30.xlsx` 只讀解析：12 張工作表均為未填值樣板，因此不呼叫 API、不寫入資料庫。
- `2026-10-27.xlsx` 只讀結構核對：多張工作表存在內部 `B4` 日期與 H／I 欄原始值／分析值，證實實際來源是矩陣式日報，不是目前欄位標題式 parser 支援的格式。
- `python -m unittest test_scheduled_import.py test_batch_convert_gui.py test_chemical_daily_parser.py test_chemical_versioning.py test_chemical_direct_writer.py`：62 passed。

## 未完成／阻擋

既有 `chemical_daily_parser.py` 只支援有「量測日期時間／分析項目／量測值／公式／F 值」標題的一維表格；真實日報是合併欄位、日期在工作表內、F 值在 F 表參照的矩陣格式。直接把這類資料列為可匯入將猜測欄位與日期，違反資料正確性規則。

因此本件不發布、不打包、不 preview、不 prepare、不 confirm，也不寫入測試或正式資料庫。後續 T3b 必須先實作矩陣式解析與代表性輸出測試，才可進行測試 API 預覽。
