# 小型變更：大型模組拆分與架構治理
- 功能 ID：ARCH-TASK-010-LARGE-MODULE-GOVERNANCE-20261012
- 版本：1
- 狀態：已完成
- 專案／授權依據／需求基準：SPC 架構改善 TASK-010；依 `TODO.md` 架構改善排序與 `docs/requirements.md` 現行需求索引。

## 問題、預期行為與範圍
- 現況：`SpcController`、`UploadService`、`SpcService`、`ManualMeasurementsV1Controller` 等檔案長期承載多個責任，後續若直接拆分核心流程，容易碰到計算、匯入、手動日報與圖表行為回歸。
- 問題：目前缺少可重跑的大型模組盤點入口與拆分治理規則，導致每次重構前都要臨時判斷哪些檔案過大、拆分順序與驗證要求。
- 預期：先建立大型模組盤點腳本與治理文件，固定門檻、責任邊界與拆分順序；後續真正拆分核心檔案時，需用此文件與腳本作為入口。
- 範圍：只新增工具與文件；不拆核心類別、不修改 API 行為、不新增測試資料、不發布。

## 需求與驗收
- R-001：提供可從 repo root 執行的大型模組盤點腳本，列出指定路徑內 `.cs` / `.vue` / `.ts` / `.js` 檔案行數。
- R-002：盤點腳本需支援 `-Threshold` 與 `-Top`，並以 non-zero exit code 標示超過門檻的檔案，供後續 CI 或人工檢查使用。
- R-003：治理文件需列出目前拆分候選、建議責任邊界、拆分順序與每類拆分的最低驗證要求。
- AC-001：Given 預設門檻 700 行，When 執行大型模組盤點腳本，Then 能列出超門檻模組並回傳非 0 exit code。
- AC-002：Given 提高門檻到 10000 行，When 執行大型模組盤點腳本，Then 無超門檻模組且回傳 0 exit code。
- AC-003：Given 文件審查，When 閱讀 `docs/architecture-governance.md`，Then 可找到 `SpcController`、`UploadService`、`SpcService`、`ManualMeasurementsV1Controller` 的治理建議與驗證要求。

## 計畫與任務
- 受影響檔案：
  - `tools/architecture/Get-LargeModules.ps1`
  - `docs/architecture-governance.md`
  - `specs/20261012-architecture-task010-large-module-governance/spec.md`
  - `specs/20261012-architecture-task010-large-module-governance/verification.md`
  - `docs/requirements.md`
  - `CHANGELOG_CUSTOM.md`
  - `TODO.md`
  - `ai_docs/10_change_log.md`
- [x] T-001：新增大型模組盤點腳本。
- [x] T-002：新增架構治理文件，記錄候選模組、責任邊界與最低驗證要求。
- [x] T-003：執行腳本與文件關鍵字驗證，並同步有效需求、變更紀錄、TODO。

## 驗證
- Happy Path：高門檻模式無超標並回傳 0。
- Boundary Case：預設門檻模式可抓出既有大型模組並回傳非 0。
- Invalid Input：路徑不存在時腳本應回非 0 並顯示錯誤。
- Regression Risk：本次不改應用程式，發布不適用。
- 實際結果及證據：詳見 `verification.md`；門檻 700 可抓出 11 個超標模組並回傳非 0，門檻 10000 回傳 0，治理文件關鍵字檢查通過。
- 發布狀態：工具與文件型小工作，發布不適用。
- 限制或未決問題：本次只建立治理基線，不實際拆分核心程式；後續拆分仍須一個小工作一個邊界，並搭配對應測試。
