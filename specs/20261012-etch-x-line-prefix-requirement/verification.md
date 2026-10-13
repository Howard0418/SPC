# 驗證紀錄：咬蝕量 X- 線別不匯入 SPC

## SPC-ETCH-X-TASK-001：規格與測試案例

日期：2026-10-13

### 驗收結果

| 驗收 ID | 結果 | 驗證方式 |
|---|---|---|
| AC-001 | 通過 | 規格明定 `X-PT-01` 等 `X-` 線別在預覽與確認匯入皆不得建立 SPC 量測資料。 |
| AC-002 | 通過 | 規格明定 `PT-01`、`PT-02`、`QE-01`、`QE-02` 等未帶 `X-` 線別仍依既有規則匯入。 |
| AC-003 | 通過 | 規格明定 `X-QE-01`、`X-QE-02` 同樣適用 `X-` 排除規則。 |
| AC-004 | 通過 | 規格明定不得將 `X-PT-01-2` 去除前綴後匯入 SPC。 |
| AC-005 | 通過 | 規格補上 TransFiles/Portal 預覽需顯示排除狀態與原因。 |

### 文件檢查

- 已補齊 TransFiles、Portal、SPC 系統責任與資料契約。
- 已補齊 Happy Path、Boundary Case、Invalid Input、Regression Risk 與 Direct SPC API 測試矩陣。
- 已補齊 rollback/補救方式，正式站與正式資料不在本批授權內。

### 建置、測試與發布

- 本 Task 為規格與測試案例文件型小工作，未修改程式。
- 自動化測試：不適用；TASK-002 實作時需建立對應自動化測試。
- 建置：不適用。
- SPC 測試站發布：不適用。
- 正式站發布：未發布。

### 後續

- `SPC-ETCH-X-TASK-002` 可依本規格實作預覽、確認匯入與 SPC 資料排除。

## SPC-ETCH-X-TASK-002：咬蝕 X- 線別排除實作與測試

日期：2026-10-13

### 驗收結果

| 驗收 ID | 結果 | 驗證方式 |
|---|---|---|
| AC-001 | 通過 | TransFiles 讀取 `A2/G2/R2/X2` 線別標題，`X-PT-01` 等 `X-` 線別加入 `excluded`，不產生 SPC `reports`。 |
| AC-002 | 通過 | 未帶 `X-` 的固定區塊仍沿用既有 `PT1/PT2/QE1/QE2` report 流程；既有測試通過。 |
| AC-003 | 通過 | `X-QE-01`、`X-QE-02` 以同一 `is_excluded_line_label` 判定排除。 |
| AC-004 | 通過 | `X-PT-01-2` 類型由 `X-` 前綴判定排除，不會去掉前綴後送 SPC。 |
| AC-005 | 通過 | JSON 輸出保留 `excluded`；Excel 輸出新增「排除清單」工作表，含原始線別、儲存格與原因。 |

### 自動化驗證

- `python -m unittest test_etch_reports.py test_etch_upload.py`：11 tests passed。
- `python -m py_compile etch_reports.py etch_upload.py`：通過。

### 真實來源唯讀驗證

- 指令：`extract_reports('D:\\SPC\\docs\\SPC文件\\PD-3-581-06B-咬蝕量_2026.xlsx')`
- 結果：`reports=114`、`excluded=8`、`quarantined=11`、`skipped=161`。
- `excluded` 包含：`X-PT-01`、`X-PT-02`、`X-QE-01`、`X-QE-02`。

### 發布

- 修改範圍：TransFiles 轉檔工具原始碼與文件。
- 未打包 EXE。
- 未連線 Portal/SPC。
- 未寫入資料。
- 未發布 SPC 測試站或正式站；SPC/Portal API 未修改，發布不適用。
