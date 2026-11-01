# FORMULA-IMPORT-004 驗證

日期：2026-11-01

## 結果

| 驗收 | 結果 | 證據 |
|---|---|---|
| AC-001 | 通過 | `test_connection_settings.py` 的唯讀登入測試驗證：先讀 SPC `/version`，才建立 Portal client、登入並取得權杖。 |
| AC-002 | 通過 | 同一隔離測試確認主檔預檢直接呼叫 `https://spctest.pmr.com.tw/api/v1/import-precheck/master-data?scope=CHEM`，帶 Bearer 權杖。 |
| AC-003 | 通過 | `test_chemical_direct_writer.py` 驗證公式 API 僅允許測試／正式白名單，正式未明確啟用即拒絕；本件未修改任何應用程式、IIS、資料庫或正式站。 |

## 指令

```text
python -m unittest test_connection_settings.py test_chemical_direct_writer.py
```

結果：15 passed。

## 限制

- 未以真人帳號登入、未呼叫 preview／prepare／confirm，未寫入測試或正式資料庫。
- `FORMULA-IMPORT-005` 仍待操作人員在測試工具手動預覽與驗收，不能以本件取代。

## 進度表與回復

- 已於 2026-11-01 備份後同步 `進度表.xlsx` 的「小工作總表」與藥液日報階段紀錄：FORMULA-IMPORT-004 為已完成且目前待辦為否，核心未完成計數為 28；FORMULA-IMPORT-005 保持進行中。
- 備份：`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表-FORMULA-IMPORT-004-20261101-001.xlsx`。
- 副本輸出、原檔重新讀取及 OpenXML SDK 結構檢查均通過（`OpenXmlErrors=0`）。如需回復，將上述備份覆蓋原檔。
