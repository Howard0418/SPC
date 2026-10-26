# 計畫
1. 盤點50個固定例外與公開設備狀態；原校驗Status/Code與既有權限基準比對。
2. 核心先測：保留CalibrationException身分／Status／Code，真隔離HTTP錯誤metadata與設備狀態，不呼叫真外部服務。
3. 明示Attach與固定資源／安全原欄位參數，controller保留原code；設備record增量metadata，逐列內容留C4C。
4. 原必要回歸／frontend ledger/build、資料與控制流程比對；核測試DB/IIS、備份發布與真health/hash/同源。
5. 文件同步、只提交本項必要檔案並push，接續C4C。
