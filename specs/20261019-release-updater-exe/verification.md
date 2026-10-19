# 驗證紀錄
2026-10-19；版本1.0.0；僅工具，不更新產品網站／DB。

| 驗收 | 結果 |
| --- | --- |
| AC1 | Framework64 csc成功產出x64單EXE，內嵌五份既有引擎＋桌面橋接／profile／SHA256；無SDK／PowerShell7執行需求。UAC manifest要求Administrator。 |
| AC2 | WinForms背景程序與UTF8日誌；離線螢幕外渲染已檢視，預設Test／SPC、未選包時操作停用，版面完整。 |
| AC3 | C#測試涵蓋無預檢、缺任一授權、manifest變更／失敗重設均不得Apply；完成碼非0不記錄預檢成功。 |
| AC4 | Rollback仍要求兩項授權及確認，不受已套用migration的Plan條件阻擋；實際回復沿用既有引擎。未執行實際Rollback。 |
| AC5 | Base64 JSON參數roundtrip（Unicode、quote、換行／命令文字）通過；內建檔案逐份hash，抽取目錄只授Administrators／SYSTEM；子程序PSModulePath隔離為Windows PowerShell，未修改全機環境或執行原則。 |
| AC6 | 原生worker與正式wrapper補上失敗碼傳遞；新增4項PowerShell測試確認worker、wrapper、橋接失敗與非法模式均非0，不誤報成功。GUI執行中禁止關閉／重入，沒有強殺程序程式碼。 |

- C#核心20項通過；新增PowerShell4項及既有35項在PowerShell7／Windows5.1通過；PowerShell parser語法檢查及文件連結檢查通過。
- EXE實際Plan：Test SPC→PMR_SPC_TEST；Production Portal（WinRM）→PMR_PORTAL_UAT，均結束碼0且GUI記錄預檢成功；僅暫存與讀取，未Apply／Rollback。
- EXE負向：有效manifest但缺ZIP，結束碼1，GUI記錄失敗且不啟用更新。
- 整合測試修正Windows5.1 UTF8 BOM、EncodedCommand錯誤串流顯示、PowerShell7繼承PSModulePath導致Get-FileHash不存在、巢狀script失敗碼遺失；先新增對應測試再修復必要wrapper，未更動部署邏輯。
- CLIXML只濾除progress，錯誤／未知或格式錯誤紀錄仍顯示；相關3項測試通過。
- Authenticode=NotSigned，未宣稱簽章；目前無簽章憑證。

## 交付
- EXE：D:/SPC/release-staging/ReleaseUpdater-1.0.0/PMR.ReleaseUpdater.exe，82,944 bytes。
- SHA256：b469cd5e21c392ed840d9fad3af6510c89c983056bad633255a8d7868f0e41bb。
- ZIP：D:/SPC/release-staging/PMR.ReleaseUpdater-1.0.0.zip，只含EXE、使用說明、SHA256SUMS；不含實際交付包／資料庫／設定／測試輸出。
- 畫面與Plan輸出留ignored _build，不提交Git。內建快取位於ProgramData/PMR-Release-Updater，未自動刪除。

正式／測試站發布不適用（工具變更）；真正Apply／Rollback、DB備份／migration、登入後業務驗收本次未執行。回復工具為改用前版EXE／來源，不操作網站或DB。
