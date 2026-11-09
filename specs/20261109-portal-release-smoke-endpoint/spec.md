# Portal 正式發布 smoke 端點隔離修正

- 日期：2026-11-09
- 狀態：完成；發布工具契約修正與Portal生日賀卡正式R4發布驗證通過
- 工作代號：PORTAL-BIRTHDAY-SIZE-1105-TASK-001（正式發布阻擋修正）
- 主系統：SPC 發布工具
- 相依系統：Portal Web／API
- 資料庫影響：無 schema 或資料寫入；發布流程仍執行既有 COPY_ONLY／CHECKSUM／VERIFYONLY 備份
- 發布影響：修正工具後，以新 Release ID 重試已授權的生日賀卡正式 Web 發布；API 版本與檔案保持
- rollback：發布交易沿用既有網站、IIS 與資料庫備份；失敗即回復舊檔，並恢復原應用程式集區狀態

## 需求

Portal 發布 manifest 不得引用 SPC 專用的咬蝕量預覽端點。Portal 的匿名授權 smoke 必須使用 Portal 自有、唯讀且可穩定驗證 401 的端點。
Portal Production API 基底必須直接指向實際 HTTPS 8443 端點，不得使用會回 307 且與 `MaximumRedirection=0` 契約衝突的 HTTP 81 端點。

## BDD 驗收

```gherkin
Given 系統產生 Portal Production 發布 manifest
When 讀取 smoke 與 rollbackSmoke
Then 不得包含 /api/etch-import-test/preview
And 應包含 GET /api/PortalLinks 並預期 HTTP 401
And API smoke 應直接使用 https://login.pmr.com.tw:8443

Given 生日賀卡正式候選已通過測試且使用者已授權正式發布
When 以新 Release ID 執行正式發布
Then Portal API 維持原版本及雜湊
And Portal Web 更新為候選版本
And Web、靜態資產、API health、環境標籤及瀏覽器使用的 API 基底皆驗證成功
```

## 測試風險

- Happy Path：Portal manifest 產生正確 401 GET smoke，正式發布完成。
- Boundary：SPC manifest 仍保留其原有咬蝕量 preview smoke，不受本修正影響。
- Invalid Input：Portal manifest 若再出現 SPC 端點，契約測試失敗。
- Regression：發布工具的環境、主機、hash、授權與 rollback 規則維持原狀。
