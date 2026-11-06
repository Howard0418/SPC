# 技術計畫

1. 建立固定 controller/action 對應表，拒絕未知寫入而非記錄任意資料。
2. 以 ASP.NET Core action filter 包住既有 scoped `AppDbContext` 的交易；2xx 時 stage 稽核後一起 commit，非 2xx 或例外時 rollback。
3. 僅記 actor、目標 ID、HTTP 方法、trace 與 `ChangedFields` 摘要；不序列化 request／response。
4. 先以單元測試覆蓋對應表與未納管端點，再建置、執行後端測試；成功後才發布測試站。

回復：將 `Audit:BusinessDataEnabled` 關閉或回復 API 套件；AuditEvents 為追加證據，絕不刪除。
