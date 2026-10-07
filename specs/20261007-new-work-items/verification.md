# Portal 生日/團保與 SPC 咬蝕 X- 小工作規劃驗證

日期：2026-10-07

## 驗證項目

- AC-001：已在 `TODO.md` 加入 Portal 生日通知、團保專區與 SPC 咬蝕 X- 不列入 SPC 小工作。
- AC-002：各小工作已記錄狀態、理由、初步範圍、BDD 驗收方向與風險。
- AC-003：已同步 `docs/requirements.md`、`CHANGELOG_CUSTOM.md`、`ai_docs/10_change_log.md`。
- AC-004：本次未修改 Portal/SPC 業務程式、API、資料庫或 UI。

## 測試矩陣

- Happy Path：確認工作池新增三類需求，且排序位於 IIS/架構改善之前。
- Boundary Case：本次為規劃文件，build 與發布不適用。
- Invalid Input：未讀取或修改 `D:\PmrPortal\GroupInsurance` 實體檔案，未處理未授權功能實作。
- Regression Risk：未修改 runtime 程式，無功能回歸風險；後續實作需各自測試。

## 發布

不適用；本次未修改應用程式。

## 結論

DONE（僅排程與發想）。
