# 功能規格：Chameleon 主機連線設定
- 功能 ID：20260910-chameleon-source-settings
- 版本：1
- 狀態：實作中
- 涉及專案：SPC
- 授權依據：2026-09-10 使用者需求。

## 目的、現況與證據
- 已確認需求：提供 SPC 頁面讓管理者輸入 Chameleon 主機 IP/API 位址。
- 程式觀察：設備監控的 `ChameleonStatusService` 只讀取 `appsettings` 的 `Chameleon:Sources`，畫面無設定入口且修改需接觸伺服器設定檔。

## 範圍與非範圍
- 新增受 Editor 權限保護的「Chameleon 連線設定」頁面，管理來源代碼、顯示名稱、主機 IP/API 位址、啟用狀態與資料新鮮度秒數。
- 設定存於 SPC 資料庫，儲存後立刻供設備狀態與點位 API 使用；既有 `appsettings` 在尚無資料庫設定時仍作 fallback。
- 不傳送設備命令、不保存帳密，不變更既有設備點位與量測資料。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | Editor 可輸入 Chameleon IP 或完整 API 位址並管理多個來源。 | AC-001 | 可新增、修改、啟用/停用來源，IP 自動正規化為 `http://IP/api/v1`。 |
| R-002 | 非 Editor 不得讀取或修改 Chameleon 連線設定。 | AC-002 | API 對未授權/Viewer 回應拒絕，設定頁受既有 Editor 路由保護。 |
| R-003 | 儲存後監控服務立刻採用資料庫設定，無資料時維持 appsettings fallback。 | AC-003 | 設定後快取失效並可由設備監控讀取新來源；沒有資料庫來源時現有設定仍可用。 |

## 邊界
- 只接受 `http` 或 `https` 的絕對網址；純 IP 或 `IP:port` 自動加上 `http://` 與 `/api/v1`。
- 無 API path 的完整主機網址同樣補上 `/api/v1`；已有 path 時不改動。
- 不將此資料庫設定包進手動正式發布包，也不改寫 IIS 環境變數。

## 規格版本紀錄
| 版本 | 日期 | 修改原因 |
|---|---|---|
| 1 | 2026-09-10 | 初版。 |