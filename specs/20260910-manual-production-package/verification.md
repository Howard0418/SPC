# 驗證紀錄
- 功能 ID：20260910-manual-production-package
- 規格版本：1
- 日期／環境：2026-09-10／本機開發環境
- 實作狀態：完成
- 驗證狀態：通過
- 發布狀態：已建立人工部署包，未發布正式 IIS。

| 驗收 ID | 命令或操作 | 預期結果 | 實際結果及證據 | 狀態 |
|---|---|---|---|---|
| AC-001 | Release/production 建置並列出發布包 | 四個獨立包及 manifest | `SPC_API` 96 檔、`SPC_Web` 5 檔、`Portal_API` 84 檔、`Portal_Web` 116 檔；每包均有 `manifest.json`。 | 通過 |
| AC-002 | 掃描包內排除項目及逐檔 SHA-256 | 無設定、uploads、憑證 | 排除掃描 `PASS`；四個 manifest 雜湊皆 `HASH_PASS`。 | 通過 |
| AC-003 | 檢查 README | 有部署與回復步驟 | 根目錄 `README.md` 已記載備份、`app_offline.htm`、保留設定/uploads、非刪除式覆蓋、App Pool、health 與回復步驟。 | 通過 |

## 交付與限制
- 發布包：[Portal-SPC-20260910-ManualRelease](../../release-packages/Portal-SPC-20260910-ManualRelease)，部署指引：[README.md](../../release-packages/Portal-SPC-20260910-ManualRelease/README.md)。
- 已完成本機 Release/production 建置；SPC Web 僅有既有 bundle 大於 500 kB 警告。
- 未連線、備份、修改或發布任何正式 IIS、資料庫、量測資料與 migration；正式部署由使用者於各自伺服器手動執行。