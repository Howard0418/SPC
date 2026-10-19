# 驗證紀錄
2026-10-19；僅工具，非產品更新。

| 驗收 | 結果 |
| --- | --- |
| AC1 | 四固定profile；環境／系統／host錯誤、跨環境manifest、所有站台內staging、傳輸來源hash竄改均拒絕。 |
| AC2 | 新遠端測試14項（含WhatIf無incoming／連線、傳輸順序、失敗關閉session）；既有21項回歸通過。 |
| AC3 | 固定hostname、Kerberos目前帳號、實際主機重驗、收件SHA256與私有ACL；Apply／Rollback缺應用或DB授權在傳輸前拒絕。未執行實際Apply／Rollback。 |
| AC4 | 真實SPC Test Plan確認同一SpcSingleTest站台／pool對backend＋frontend兩目錄；Portal測試pool=PmrPortal，未套用正式pool假設。 |
| AC5 | 四組實際Plan全部通過：Test SPC→PMR_SPC_TEST；Test Portal→PMR_PORTAL_TEST；Production SPC→PMR_SPC_2026；Production Portal→PMR_PORTAL_UAT。 |

實際Plan透過目前工作機PC-PA16560048的32位元客戶端啟動64位元Windows PowerShell；正式兩站透過WinRM／Kerberos與固定主機執行。測試機先唯讀核對IIS bindings、physicalPath、pool及非敏感DB名稱，未輸出連線字串或密碼。

實際預檢過程發現 powershell.exe -File 無法將字串 Confirm:$false 繫結為switch，新增可信任worker於PowerShell內傳真布林值後，四組Plan均成功；亦修正Windows PS5獨立程序需明確載入System.IO.Compression。

Plan僅寫入私有incoming交付暫存與transfer紀錄，未建立網站／SQL備份、停止任何pool、覆寫網站或執行migration。交付／證據在ignored release-staging/environment-switch與目標incoming保留，不提交。兩個測試站原DLL仍為SPC0.1.59、Portal API1.0.177／Web1.0.220。

PowerShell7及Windows PowerShell5.1隔離測試均通過：既有21、新增14；語法與文件連結檢查通過。32位元Windows PowerShell的測試程序使用process-only ExecutionPolicy Bypass，沒有變更機器執行原則；正式工作器不繞過執行原則。

未驗證：真正Apply／Rollback、實際SQL備份／migration、登入後業務操作。產品程式未修改，測試站／正式站發布不適用。Rollback為還原工具來源至前一提交，保留incoming及既有備份。
