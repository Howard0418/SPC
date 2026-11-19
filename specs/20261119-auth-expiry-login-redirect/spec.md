# 登入過期自動返回登入頁

- 工作代號：`SPC-AUTH-EXPIRY-REDIRECT-1119-TASK-001`
- 主系統：SPC Web；相依系統：Portal SSO
- 資料庫影響：無
- 發布影響：SPC Web 測試區；正式另行授權
- Rollback：回復前一版 Web 靜態資產

## Requirement

API 回傳 401 時，不得讓使用者停留在無法操作的錯誤頁。系統需保留安全的站內返回路徑、清除失效登入並導向 Portal 登入；重新登入後返回原頁。

## BDD

- Given 使用者 Token 已過期，When 任一受保護 API 回傳 401，Then 清除本機登入並自動返回 Portal 登入頁。
- Given 使用者剛完成 SSO 且並行請求短暫出現 401，When 隨後有成功 API 回應，Then 取消待執行的登入跳轉，避免登入循環。
- Given 原頁含查詢參數與 hash，When 登入逾期，Then 保存完整站內路徑，登入後返回該頁。
- Given 外部或雙斜線路徑，When 建立返回路徑，Then 不接受為登入後導向位置。

## 測試規劃

- Happy Path：過期 Token＋401 自動跳 Portal 登入。
- Boundary：SSO 寬限期內的 401 不再永久停留。
- Invalid Input：外部返回網址維持拒絕。
- Regression Risk：Portal SSO 初次登入、並行初始化請求、直接登入。
