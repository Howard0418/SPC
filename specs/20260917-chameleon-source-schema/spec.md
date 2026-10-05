# 設備即時狀態來源資料表修復
- 功能 ID：20260917-chameleon-source-schema；版本 1；狀態：修復、驗證與測試發布完成。
- 授權：使用者回報無效物件 ChameleonSourceSettings，依 AGENTS 修復與測試發布。
## 現況、範圍
ChameleonStatusService 已查詢資料庫來源設定，但沒有建立該表的 EF migration。前次藥液發布帶出工作區既有服務，僅套用藥液 migration，造成測試站來源查詢失敗。
此次新增專屬 migration 與 snapshot，建立既有模型需要的 ChameleonSourceSettings 及 SourceId 唯一索引。保留目前資料庫來源優先、空表回退 appsettings 規則；不改 PLC/Chameleon 設備、不新增來源、不改正式庫。
## 需求與驗收
- R-001/AC-001：PMR_SPC_TEST 透過 EF migration 建立來源表及唯一索引，只有此 migration 的 DDL，不混入其他模型差異。
- R-002/AC-002：空表可讀既有設定，資料庫來源可正常讀取；設備狀態端點不再回無效物件名稱。來源無法連線時沿用既有 unavailable 狀態，不能誤稱設備在線。
- R-003/AC-003：只發布 SPC 測試 API，備份且保留原設定；不改 Portal/前端及既有量測。
## 發布／回復
核對測試 IIS 路徑、PMR_SPC_TEST、待套用 migration；備份應用與測試庫，啟動 API 執行 migration。回復以一致應用/資料庫備份進行，不直接刪除可能已有來源設定的資料表。
