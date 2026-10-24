# I18N-TASK-05E：匯入批次與預覽 UI 三語化
- 日期：2026-10-25；狀態：DONE，測試站已發布；[驗證](verification.md)。
- 授權：使用者「開始下一項」；依工作池 05E 及國際化契約，僅處理此件。
- 基準：[國際化任務](../20261022-i18n-task01/tasks.md)、[TransFiles 匯入分工](../20261014-spc-import-via-transfiles-only-plan/spec.md)、[翻譯契約](../../docs/i18n/contract.md)。

## 範圍與不變條件
- 主系統 SPC：公開 `/uploads`、`/uploads/:batchId/preview` 的固定文字、動態提示及既有主檔補建確認／結果提示提供繁中、簡中、英文。
- TransFiles／Portal 無程式變更；未接線的舊 VariableUploadView／AttributeUploadView 不重新接線或擴張翻譯。
- 不變：API URL、query、request body、權限、狀態 enum、原始檔名／主檔代碼／來源欄位／錯誤原文／建議／數值及時間解析。日期仍沿用原呈現方法；不依語言重新解析。
- 不新增 Excel 上傳、網頁確認匯入、覆蓋或刪除操作。既有 create-missing-mappings 主檔補建仍維持原操作，並非量測確認匯入。
- DB/schema、API、SSO、IIS binding 不變；只更新測試前端，正式未授權。
- 翻譯保留 draft／uncertain，人工審核留 TASK-20。

## BDD 驗收
1. Given Editor 開啟批次清單／錯誤預覽，When 切換三語，Then 固定標題、欄位、空白提示與對應狀態即時改變，URL／既有資料不變且不重新查詢或寫入。
2. Given 原始檔名、錯誤、建議或 payload 含中文、HTML 外觀與 i18n 特殊字元，When 顯示任一語言，Then 原文以純文字保留，0 及空白單位不丟失。
3. Given 未匯入且缺少設定的預覽，When 取消或確認既有補建操作，Then 取消不送出、確認只呼叫原 create-missing-mappings 端點，確認與結果提示採當下語言，保留既有重驗證流程；不提供上傳／確認匯入。
4. Given Imported 批次且有／無 chart target，When 查看，Then 製圖按鈕原路由 query 不變／無 target 仍停用。Viewer 原權限限制不變。
5. Given 1920／1366／1280／390 寬度，When 使用三語，Then 導覽、重新整理、預覽及既有補建操作可達，寬表於內部捲動。

## 測試矩陣
- Happy Path：清單→預覽→製圖、三語切換、補建成功／取消。
- Boundary：空清單、空預覽、數值 0、空白單位、缺製圖目標、手機長英文。
- Invalid Input：API 失敗原文、來源含 HTML/i18n 符號、無效日期依原 fallback。
- Regression Risk：語言切換不能重載或觸發寫入；補建 DTO／端點與權限保持；不恢復網頁匯入。

## 發布與回復
- 通過字典／模板／瀏覽器／build 後發布 `release/test/frontend`，核對 SpcSingleTest 實際路徑與 AppEnvironment=test；API 沿用。
- 發布前完整備份前端與雜湊；回復將本次備份的舊 index 與原資產覆回同一測試目錄，保留設定；不操作正式站。
