# FORMULA-IMPORT-004 計畫

## 影響分析

| 項目 | 結果 |
|---|---|
| TransFiles | 核對既有 `connection_settings.py`、`connection_test.py`、`batch_convert_gui.py` 及 `chemical_direct_writer.py`。 |
| Portal | 僅既有登入／`spc-launch` 權杖契約；不修改。 |
| SPC API | 既有白名單直接呼叫；不修改。 |
| 資料庫／IIS／正式環境 | 無影響。 |

## 驗證安排

1. 執行連線設定隔離測試，確認 Portal 只發行權杖、SPC 預檢直連且不觸發 preview／confirm。
2. 執行日期公式 API 白名單測試，確認測試／正式網址不可混用、正式預設封鎖。
3. 不進行真人登入或寫入。真人測試預覽留在 `FORMULA-IMPORT-005`。

## 發布

無應用程式改動，SPC 測試站／正式站發布不適用。TransFiles 不打包。
