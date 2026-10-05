# 功能規格：Portal 與 SPC 手動正式發布包
- 功能 ID：20260910-manual-production-package
- 版本：1
- 狀態：實作中
- 涉及專案：SPC、PmrPortal
- 授權依據：2026-09-10 使用者需求；只建立本機發布包，由使用者手動部署至不同正式伺服器。

## 目的、現況與證據
- SPC 與 Portal 正式站位於不同伺服器；本機 IIS 僅確認為測試站，不得直接部署正式環境。
- API 發布輸出預設包含 `appsettings.json`，直接複製可能覆蓋不同伺服器的正式連線與機密設定。

## 範圍與非範圍
- 建立 SPC API、SPC Web、Portal API、Portal Web 四個獨立 Release 資料夾，以及 manifest 和人工部署 README。
- 每個 API 包排除 `appsettings*.json`、`uploads`、憑證與站台特有內容；Web 包同樣排除設定檔。
- 不連線、備份、修改或發布任何正式 IIS、資料庫與量測資料；不執行 migration。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 產生四個彼此獨立的發布包。 | AC-001 | 四個包各含可部署產物與 manifest。 |
| R-002 | 發布包不可帶入測試或正式站的設定、附件、憑證。 | AC-002 | 包內沒有 `appsettings*.json`、`uploads` 或 `.pfx` 檔。 |
| R-003 | 提供可安全手動部署與回復的指引。 | AC-003 | README 載明備份、離線頁、排除檔案、覆蓋、回收 App Pool、health 驗證及回復。 |

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-09-10 | 初版。 |