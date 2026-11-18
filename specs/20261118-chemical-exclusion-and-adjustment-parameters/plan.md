# 技術計畫
- 功能 ID：20261118-chemical-exclusion-and-adjustment-parameters
- 規格版本：1
- 規格：[spec.md](spec.md)

## 系統責任與受影響檔案
- Portal：勾選／下拉、查詢標示、代理 DTO、前後端驗證。
- SPC API：資料模型、migration、保存／查詢、排除映射、統計與圖表來源。
- SPC／Portal 新調整公式頁：四欄顯示與維護。
- 資料庫：先盤點既有欄位，再決定新增排除與參數欄位或版本表；禁止重複 schema。
- TransFiles：非範圍；舊匯入未帶旗標視為正常納入。

## 實作方式與需求對應
1. 先執行 `PORTAL-SPC-CHEM-EXCLUSION-1118-TASK-001`：以失敗測試固定契約與排除口徑，再做 migration、SPC 計算／圖表、Portal UI 與追溯。（R-001～004、006）
2. 前件穩定後才執行 `PORTAL-CHEM-ADJUST-PARAMETERS-1118-TASK-001`：盤點既有 C17～C19 與公式頁模型，避免重複資料源，再建立欄位、驗證與稽核。（R-005～006）
3. 參數欄位完成後執行 `PORTAL-CHEM-ADJUST-REMARK-1118-TASK-001`：備註沿用同一分析項目與版本／稽核契約，避免同頁或同一 migration 併行衝突。（R-006～007）
4. 每次只做一件，不與其他 schema／公式工作併行。

## API、檔案格式及資料模型
- 建議 DTO：`excludeFromSpc: bool`、`exclusionReasonCode: string?`，伺服器 allow-list 驗證。
- 建議原因碼：`CURRENT_BATH_NO_OPEN_LINE`、`CLOSE_LINE`、`INSPECTION_NO_OPEN_LINE`。
- 舊資料預設 `false`，migration 不回判舊資料。
- 調整參數以 decimal 保存，槽容單位 L；另外兩欄單位先依公式／Excel確認。
- 若已有 `TankCapacityL`／控制值持久化欄，直接共用。
- 備註為 nullable Unicode 文字，trim 後最多 500 字；空白正規化為 null，不允許 HTML，顯示時編碼。

## 相容性與風險
- 必須從 SPC 資料源與計算層統一排除，不能只在前端隱藏點。
- 圖表、Cpk、摘要、匯出與重算可能走不同查詢路徑，須做端到端矩陣。
- schema migration 先測試庫備份與回復；正式另行授權。
- 舊 Portal／TransFiles 不傳新欄位仍可保存。
- 不重新顯示既有隱藏調整／調整量欄。

## 驗證安排
- Happy：正常／排除／取消排除、三原因、參數與備註保存重載。
- Boundary：空原因、未知代碼、空／0／極大參數。
- Invalid：舊 token、非法 enum、無權限、備註超過 500 字、部分批次失敗。
- Regression：三位計算、複驗、查詢滴定值、未排除藥液及非藥液圖表、舊匯入。
- 自動化涵蓋 SPC calculator/service/API、Portal service/UI 與 migration；人工依實際頁面截圖核對。

## 發布、備份及回復
- 本次只規劃，不建置、不發布、不改版本。
- 未來每件測試通過後依規則遞增 API／Web 版本並發布測試站，完成 Web／資產／API／瀏覽器 API 基底驗證。
- 正式另行授權；migration 前備份。回復以前版 API／Web 加安全 migration 策略處理。
