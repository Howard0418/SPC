# FORMULA-IMPORT-005 驗證紀錄（部分完成）

日期：2026-11-01

## 已完成

- GUI 日報捷徑與 `scheduled_import.py` 的藥液／咬蝕量預設來源，已一致改為 `\\172.16.119.200\LineData`；使用者原本可自行選擇的路徑功能不變。
- `Test-Path` 實測藥液來源可讀，偵測到 801 個 `.xlsx`。
- `2026-10-30.xlsx` 只讀解析：12 張工作表均為未填值樣板，因此不呼叫 API、不寫入資料庫。
- `2026-10-27.xlsx` 只讀結構核對：多張工作表存在內部 `B4` 日期與 H／I 欄原始值／分析值，證實實際來源是矩陣式日報，不是目前欄位標題式 parser 支援的格式。
- 矩陣式 parser 現已讀取第 6 列結構、`B4` 日期、B／C／D／E／G／H／I／M／N 欄與 Excel 公式中明確的 F 表引用。表尾簽核文字不會被當成量測；缺日期、槽位、公式快取或 F 值一律保留為人工複核。
- `2026-10-27.xlsx` 只讀解析：111 筆，其中 51 筆 `ready_for_preview`、60 筆 `manual_review`；不呼叫 API、不寫入資料庫。
- `python -m unittest test_chemical_daily_parser.py test_scheduled_import.py test_batch_convert_gui.py test_chemical_versioning.py test_chemical_direct_writer.py`：65 passed。

## 未完成／阻擋

完整 801 檔乾跑超過目前執行環境的可等待時間而中斷，未產生完成報告；中斷前後均未呼叫 API 或資料庫。後續應以打包工具或本機長時間程序完成乾跑並檢視逐檔報告。

因此本件不發布、不打包、不 preview、不 prepare、不 confirm，也不寫入測試或正式資料庫。後續 T3b 先以 51 筆 `ready_for_preview` 的代表性批次完成測試 API 預覽與人工驗收；60 筆人工複核資料不可自動寫入。
