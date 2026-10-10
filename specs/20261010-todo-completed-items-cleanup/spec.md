# 小型變更：TODO 已完成項目整理
- 功能 ID：TODO-COMPLETED-ITEMS-CLEANUP-20261010
- 版本：1
- 狀態：DONE
- 專案／授權依據／需求基準：SPC；使用者要求「繼續」下一個小工作；依 `TODO.md` 只顯示未完成、待執行、待確認小工作的既有規則整理。

## 問題、預期行為與範圍

`TODO.md` 仍列出已完成或取消的工作：`SPC-CHART-POINT-REMARK-TASK-001`、`PORTAL-BIRTHDAY-REPLAN-TASK-001`、`PORTAL-BIRTHDAY-REPLAN-TASK-002`、`PORTAL-BIRTHDAY-REPLAN-TASK-003` 與 `PORTAL-GROUP-INSURANCE-CANCELLED`。這些項目已有 specs、需求索引與 `CHANGELOG_CUSTOM.md` 可追溯，不應保留在未完成工作池。

本次只整理文件，不修改 SPC/Portal 程式、資料庫或 IIS，不發布測試站或正式站。

## 需求與驗收
- R-001：`TODO.md` 只保留未完成、待執行、待確認或外部阻擋項目。
- AC-001：已完成或取消項目自 `TODO.md` 未完成排序區移除，且可由 `docs/requirements.md`、`CHANGELOG_CUSTOM.md` 或對應 specs 追溯。

## 計畫與任務
- 受影響檔案：`TODO.md`、`docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`、本規格。
- [x] T-001：整理 `TODO.md` 已完成/取消項目。
- [x] T-002：驗證 `TODO.md` 未完成排序區不再列出上述完成項目。
- [x] T-003：同步有效需求與變更紀錄。

## 驗證
- 方式、環境：文件內容檢查與 `rg` 關鍵字檢查。
- 實際結果及證據：`TODO.md` 未完成排序區已移除已完成/取消項目；完成狀態保留於 `docs/requirements.md`、`CHANGELOG_CUSTOM.md` 與對應 specs。
- 發布狀態：文件型小工作，建置、打包與發布不適用；正式站未發布。
- 限制或未決問題：`IIS-TASK-006/T-007` 因 HTTPS binding/cert、測試帳號與瀏覽器環境仍保留待補驗；咬蝕 X- 需求未完整仍暫緩。
