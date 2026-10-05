# 計畫
SPC服務增加只讀preview，strict import endpoint沿用交易/鎖/雜湊。Portal增加獨立嚴格服務（完整點位比較、先檢查後儲存）與協調controller，使用既有日報計算、登入Cookie及SPC token。TransFiles使用cookie client、固定測試位址、先預覽全部，衝突阻擋確認；每次確認再由伺服器嚴格比較。
先建立核心測試：preview不寫、相同略過/衝突拒絕、部分失敗重試、環境拒絕；client模擬HTTP測試不寫外部資料。部署備份及設定雜湊保留。真實站只讀既有9月預覽可在使用者登入後驗證；不自行建立帳號。

## 實際API契約
Portal POST /api/etch-import-test/preview、/confirm：Cookie品保角色，完整日報JSON（reportDate,lineCode,lineSpeed,operatorName,points,provenance）。preview回portal/spc各new或unchanged；任一衝突409。confirm回portalId/status/SUCCESS/unchanged/spc；遠端失敗保留Pending並回非2xx。確認時重讀並交易鎖定，不依賴預覽過時資料。
SPC POST /api/v1/etch-import-test/preview、/confirm：JWT Admin/Editor，回{success,data:{batchId,unchanged,pointCount},message}。與既有允許修改的PUT etch-reports分開。
TransFiles固定Portal http://172.16.110.27:8091，禁止重新導向，cookie/密碼僅記憶體；重試需再次預覽/確認。預覽快照不可被替换，紀錄檔不含權杖。
兩端必須通過實際SQL連線guard，未增加DDL/資料庫帳密至工具；伺服器寫入及成功狀態記錄登入操作者，SPC保留來源/批次稽核。
