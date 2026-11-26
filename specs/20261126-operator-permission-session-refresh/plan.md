# 技術計畫
- 功能 ID：SPC-OPERATOR-PERMISSION-SESSION-REFRESH-1126
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案

- API：驗證 JWT 後依 `NameIdentifier` 與帳號查回啟用中的 Operator，使用目前角色與授權，不延長 JWT 有效期；`AuthController` 回傳本人的最新資料。
- Web：集中封裝 session refresh；路由守衛在權限判斷前同步；本人儲存後立即同步；App 監聽登入階段變更以重算選單。
- 測試：API controller 測試、Web session helper／作業人員頁 E2E、既有導覽與設定回歸。
- 版本：API `0.1.178 → 0.1.179`；Web `0.1.200 → 0.1.201`。

## 實作方式與需求對應

1. 先建立 refresh 有效、停用及錯誤身分測試（R-001、R-004）。
2. `GET /api/v1/auth/session` 僅接受已驗證 JWT，回傳資料庫最新角色／權限（R-001、R-005）。
3. `syncCurrentSession` 驗證回應與登入階段後更新本機權限；較早的回應不覆蓋已套用的較新回應（R-001、R-004）。
4. 路由守衛刷新後再判斷角色與頁面權限；非 401 暫時失敗沿用現有 session（R-003）。
5. 作業人員頁只在儲存目標 ID 等於目前登入 ID 時立即刷新，避免管理者切換為他人身分（R-002）。
6. `setAuthSession`／`clearAuthSession` 派送同分頁事件，App 立即重算選單（R-002）。

## API、檔案格式及資料模型

- `GET /api/v1/auth/session`
  - 輸入：無 body；Authorization Bearer 必填。
  - 成功：`{ user: { id, username, operatorCode, displayName, role, permissions } }`，禁止快取。
  - 失敗：停用、刪除、ID／帳號不一致回 401。
- 無 schema migration、無業務資料異動。

## 相容性與風險

- 滾動發布時舊 API 可能暫回 404，Web 對非 401 refresh 失敗保留既有 session，不阻擋既有操作。
- 401 沿用 API client 全域導回 Portal 登入流程。
- refresh 不經 OperatorsController，不可新增或撤除任何權限，也不繞過稽核明示授權。

## 驗證安排

- AC-001／004：API 測試驗證目前角色與權限、停用拒絕及登入憑證到期時間保持。
- AC-002：Playwright 儲存本人後觀察 PUT、session 查詢與選單更新。
- AC-003：路由／session helper 測試驗證刷新結果先於路由權限判斷。
- AC-005：既有安全稽核測試及 Operators 設定回歸。
- build：`dotnet test`、`npm` 相關 Node/Playwright、API/Web Release build。

## 發布、備份及回復

- 測試通過後依規則直接發布 SPC 測試站，核對 Web／API 版本、首頁資產 MIME、health/version、同源與直接 API。
- 無資料庫變更；備份現行 API 與 Web 發布檔。回復時還原兩者，版本紀錄如實保留。
- 正式站仍待使用者另行授權。
