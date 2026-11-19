# 藥液量測調整公式移至 SPC 系統

## 工作代號

`SPC-CHEM-ADJUST-PAGE-MOVE-1119-TASK-001`

## 系統範圍

- 主系統：SPC Web。
- 相依系統：Portal Web（入口與 SSO）、Portal API（既有角色 Email 通知）、SPC API（主檔與儲存）。
- 資料庫影響：無 schema migration；沿用 SPC `PartProcessCharacteristic.ChemicalAnalysisConfigJson`。
- 發布影響：SPC Web 與 Portal Web 測試站；不發布正式站。
- Rollback：還原 SPC 路由／頁面／選單及 Portal 入口連結，保留既有資料與 API。

## 需求

1. 「藥液量測調整公式」實際維護畫面須位於 SPC 系統，不再由 Portal Razor Page 承載。
2. SPC 編輯者可依線別／機台查看固定格線，維護藥液、槽容、添加管制值、稀釋管制值及備註。
3. 保留 Excel 多列貼上、預覽、批次儲存，以及空白欄位不覆蓋既有資料的既有行為。
4. Portal 品保專區卡片與舊網址只負責經 SSO 導向 SPC 新頁面。
5. 儲存仍經既有 Portal 受控代理，以保留品保／製程角色 Email 通知；不得改寫規格、範圍或公式。
6. 未登入或權限不足時沿用既有 SSO／權限處理，不新增公開寫入入口。

## BDD 驗收

### Scenario 1：由入口網站進入 SPC 維護頁

- Given 使用者具有品保與 SPC 編輯權限
- When 點擊品保專區的「藥液量測調整公式」
- Then 系統經 `/Spc/Launch` 開啟 SPC `/chemical-adjustment-parameters`
- And 維護畫面顯示 SPC 導覽與版本資訊

### Scenario 2：貼上並保存參數

- Given SPC 畫面已載入某線別的固定槽位／分析項目格線
- When 使用者從第一個可編輯格貼上含標題或不含標題的多列資料並預覽
- Then 系統依欄位及格線順序配對
- And 無效正數、字數超限或超出格線時不得保存
- When 預覽無錯誤並批次儲存
- Then 只更新藥液、槽容、添加／稀釋管制值與備註
- And 已驗收的角色 Email 通知條件維持不變

### Scenario 3：舊頁相容

- Given 使用者開啟既有 `/Spc/QualityAssurance`
- When 頁面處理請求
- Then 不再顯示 Portal 維護 UI
- And 導向 SSO launch 後的 SPC 新頁面

## 測試矩陣

- Happy Path：主檔載入、線別切換、五欄貼上、預覽、批次保存。
- Boundary：100 字藥液、500 字備註、最後一列貼上。
- Invalid Input：0／負數／非數字、超出格線、未選線別。
- Regression：Portal SSO、角色 Email 通知、既有 SPC API 儲存契約、入口網站其他品保卡片。

## 發布規則

- 測試通過後遞增 SPC Web 與 Portal Web 版本並直接發布測試站。
- 部署後核對前台版本、主要 JS/CSS、SPC API health/version/environment，以及瀏覽器使用的 API 基底。
- 正式站須另行授權。
