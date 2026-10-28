# 計畫與回復
1. 先補C#查詢整合／精度與保存證據測試，以及Portal複驗0／初驗NG／未知UI測試。
2. SPC新增唯讀歷史判定投影，批次讀取當頁measurement、trace／receipt／effectivity／version及既有計算結果，避免逐點查詢；重用既有trace證據校驗，不執行計算公式。
3. Portal主要欄位複驗優先，新增判定與初驗欄，未知明示、取位說明及來源tooltip；不改寫入與NG計算。
4. 相關回歸與API/Web build，核對IIS及備份後發布SPC測試backend、Portal測試API/Web。SPC frontend無程式變更，不另重建。
5. 同步文件／原Excel／狀態，必要檔案分repo提交上傳。Rollback使用本件測試發布前備份回復同一測試目錄，保留appsettings/web.config/uploads；無DB回復。
