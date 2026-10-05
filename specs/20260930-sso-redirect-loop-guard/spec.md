# 小型變更：SPC SSO 重導迴圈保護
- 功能 ID：20260930-sso-redirect-loop-guard
- 版本：1
- 狀態：完成
- 專案／授權依據／需求基準：SPC；使用者回報 SPC 偶爾畫面閃爍、網址一直重讀，並同意加入 401 redirect 防抖與 SSO 迴圈保護；需求索引見 `docs/requirements.md`。

## 問題、預期行為與範圍
- 問題：API 回 401 時，前端清 token 後導 `/login`，Login 立刻導 Portal，Portal 回 `/portal-sso` 後再回原頁；若多個 API 同時 401 或剛完成 SSO 後又遇到舊請求 401，可能形成閃爍與網址反覆重讀。
- 預期：401 重導短時間內只觸發一次；剛完成 SSO 的短時間內不因舊 401 請求再次導回登入。
- 範圍：僅 SPC 前端登入/axios 流程；不改後端、不改資料庫、不改 Portal。

## 需求與驗收
- R-001：401 redirect 需防抖，避免多個同時失敗請求重複導頁。
- AC-001：多個 401 回應同時發生時，只做一次 `/login?redirect=...`。
- R-002：`/portal-sso` 成功寫入 token 後需有短暫保護窗，避免舊請求 401 立刻清掉新 token。
- AC-002：SSO 成功後短時間內收到 401，不會立即再次導 Portal 造成閃爍。

## 計畫與任務
- 受影響檔案：
  - `frontend/mes-spc-web/src/api/client.js`
  - `frontend/mes-spc-web/src/views/PortalSsoView.vue`
- [x] T-001：實作 R-001/R-002。
- [x] T-002：驗證 AC-001/AC-002。
- [x] T-003：同步有效需求與變更紀錄。

## 驗證
- 方式、環境：前端 build；必要時以人工測試 Portal SSO 重入。
- 實際結果及證據：`npm run build` 通過。
- 發布狀態：已發布 SPC 測試站前端；備份 `frontend.backup-sso-loop-guard-20260930-140731`；首頁載入新資產 `index-CUp_gYIn.js`。
- 限制或未決問題：無法用真實 Portal/AD 自動重現偶發舊 token 競態，需使用者驗收。
