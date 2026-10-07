# TODO 只顯示未完成小工作驗證

日期：2026-10-07

## 驗證項目

- AC-001：`TODO.md` 已改名為「SPC 全系統未完成小工作池」，並新增只顯示未完成規則。
- AC-002：已完成的 SPC-1002、點位排除、藥液公式與 Portal 已完成項目已移除。
- AC-003：Portal 待規格、SPC 咬蝕 X-、IIS、全專案 AI+BDD、KM 教育訓練、正式機更新工具、架構改善待確認項目仍保留。
- AC-004：已同步 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`。

## 測試矩陣

- Happy Path：目前工作池只列待處理項目。
- Boundary Case：`TASK-001 設定與密鑰安全` 標示已完成但待使用者確認，仍保留。
- Invalid Input：未刪除 specs/changelog 歷史紀錄。
- Regression Risk：文件型整理，無 runtime 風險。

## 發布

不適用。

## 結論

DONE。
