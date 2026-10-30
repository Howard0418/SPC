# 小型變更：校驗管理未知 Editor JWT 授權收斂

- 功能 ID：CAL-VERIFY-001
- 日期：2026-10-31
- 狀態：DONE；測試 API 0.1.93 已發布，正式站未更新
- 優先級：P1（權限隔離）
- 主系統：SPC；Portal 僅提供既有 SSO 身分，不修改 Portal。
- 資料庫影響：無 schema／資料寫入。
- 發布影響：僅 SPC 測試 API；正式站另行授權。

## 現況與結論

`AuthorizationAccess` 在 JWT 已驗證但資料庫找不到對應 Operator 時，仍把 JWT 的 Editor role 視為 active；`CalibrationRules` 對沒有權限 JSON 的 Editor 授予全部頁面權限。因此未知帳號持合法簽章 Editor JWT 時可進入校驗寫入端點。既有完整 HTTP 測試預期 403，且正常 SSO 會先建立／取得 Operator 再簽發 Token；未知資料列不屬正常登入路徑。

## 需求與 BDD

- R-001：校驗管理寫入權限必須同時具備有效 JWT、存在且啟用的 SPC Operator、Editor role及 `calibration.manage` 權限。
- R-002：未知、停用、Viewer、明確撤銷頁面權限者均須 fail closed；不得只信任 JWT role。
- R-003：既有啟用 Editor、JWT claim fallback 與明確權限 JSON 行為保持。

- AC-001：Given 合法簽章 Editor JWT 但 SPC 無對應 Operator，When 呼叫校驗 preview／commit，Then 回 403。
- AC-002：Given 啟用 Editor 且有 `calibration.manage`，When 查權限，Then 仍允許。
- AC-003：Given 停用／Viewer／明確不含校驗權限，Then 仍拒絕；其他 SPC 功能不受影響。

## 實作、測試與回復

- 先補 `AuthorizationAccessTests` 未知 Editor 測試，再讓 `ResolveAsync` 對查無 Operator 回傳 inactive，維持已驗證身分資訊供診斷但不可管理校驗。
- 執行 AuthorizationAccess、CalibrationImport HTTP 權限回歸與 API build；通過後發布 SPC 測試 API並驗證 health/version。
- 回復方式：還原本次程式與測試提交，重新發布前一版測試 API備份。正式站不發布。

## 測試規劃

- Happy Path：存在、啟用、Editor且有權限。
- Boundary：Operator 無權限 JSON但 JWT 有校驗 claim；資料庫明確權限優先。
- Invalid Input：未知帳號、未驗證、Viewer、停用、錯誤權限 JSON。
- Regression Risk：SSO正常已建 Operator的使用者與其他非校驗 API不變。

## 驗證結果

- 新測試先重現 `Expected false / Actual true`，修正後 AuthorizationAccess 5/5 通過。
- 校驗匯入 preview／commit 未授權矩陣 5/5 通過，含 unknown Editor 403。
- API Release build 0 warning／0 error。
- 已發布 SPC 測試 API 0.1.93；首頁、`/spc`、同源 `/api/health` 與 `/api/version` 均 HTTP 200。正式站未更新。
- 測試發布備份：`D:\SPC\release-staging\cal-verify-001-20261031\backup`。
