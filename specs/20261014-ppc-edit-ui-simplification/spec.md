# 小型變更：SPC 管制項目編輯畫面直覺化
- 功能 ID：SPC-PPC-EDIT-UI-SIMPLIFICATION-20261014
- 版本：1
- 狀態：DONE
- 專案／授權依據／需求基準：使用者 2026-10-14 確認依建議版面調整 SPC 管制項目新增／編輯畫面；參考 `docs/requirements.md` 的 SPC 槽位顯示一致需求。

## 問題、預期行為與範圍
- 現況：`SPC 管制項目` 新增／編輯抽屜同時存在快速模式與進階精靈模式，導致顯示方式、管制圖類型、進階摘要與實際設定欄位重複；畫面仍有「槽體」字樣。
- 預期：新增／編輯抽屜改為固定分頁，欄位依使用情境分成基本資料、規格與抽樣、圖表設定、藥液公式與進階設定；顯示方式與管制圖類型各只在圖表設定分頁出現一次；畫面文字統一使用「槽位」。
- 範圍：僅調整 SPC 前端 `PartProcessCharacteristicsView.vue` 的編輯抽屜文案與版面。不修改 API、資料庫、儲存 payload、管制圖計算或權限。

## 需求與驗收
- R-001：編輯抽屜不得再以快速／進階模式切換同一批欄位。
- R-002：`顯示方式` 與 `管制圖類型` 不得在同一表單重複出現。
- R-003：藥液管制定位、提示、錯誤訊息與公式說明統一顯示「槽位」。
- R-004：藥液公式與進階 JSON 不應干擾一般基本資料與規格設定。
- AC-001：開啟新增或編輯管制項目時，可看到固定分頁：基本資料、規格與抽樣、圖表設定、藥液公式、進階設定。
- AC-002：基本資料分頁不顯示圖表顯示方式或管制圖類型；圖表設定分頁各只顯示一次。
- AC-003：畫面不再出現「槽體」文案，保留資料欄位與儲存行為不變。
- AC-004：前端 production build 通過。

## 計畫與任務
- 受影響檔案：`frontend/mes-spc-web/src/views/PartProcessCharacteristicsView.vue`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`。
- [x] T-001：建立分頁式編輯抽屜，移除快速／進階模式切換入口。
- [x] T-002：收斂顯示方式、管制圖類型、藥液公式與進階 JSON 所在區域。
- [x] T-003：統一「槽位」文案。
- [x] T-004：執行前端 build 並同步有效需求與變更紀錄。

## 驗證
- 方式、環境：`D:\SPC\frontend\mes-spc-web` 執行 `npm run build`。
- 實際結果及證據：build 通過；Vite 仍提示既有 chunk size warning。
- 發布狀態：已刷新 SPC 測試站 frontend，備份 `release/test/frontend.backup-ppc-edit-ui-20261014-105341`；正式站未發布。PowerShell 對 `https://SPCTEST.pmr.com.tw/` 仍發生 SSL 連線失敗，需以瀏覽器或受信任憑證環境再做外部 smoke。
- 限制或未決問題：本次不改 API 或資料庫，因此欄位名稱仍沿用既有 `tankId` 等內部資料契約。
