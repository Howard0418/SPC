# PMR 系統更新工具 EXE

版本1.1.0｜[規格](../../specs/20261019-release-updater-exe/spec.md)｜[環境與遠端發布規則](remote-release-runbook.md)

## 開啟與更新

1. 雙擊 `PMR.ReleaseUpdater.exe`，依Windows提示確認系統管理員權限。
2. 選擇測試／正式環境及SPC／Portal，確認畫面顯示的主機與資料庫。
3. 選擇完整交付目錄的 `manifest.json`；對應ZIP與已審查SQL須保留於原目錄。
4. 按「執行預檢」，等候成功結果；這一步不更新站台或資料庫，但會暫存交付檔。
5. 確認本次應用與資料庫／啟動授權後勾選兩項，再按「開始更新」；確認視窗會列環境、系統、主機、DB、release ID及版本，確認後才執行。
6. 等候完成，再登入做功能驗收。執行期間不能從程式內關閉，不會強殺發布子程序。

交付包或環境／系統變更會取消預檢與授權狀態；manifest被外部改寫時須重新預檢。缺檔、hash或主機不符均沿用引擎拒絕；錯誤結束碼不會被當成成功。

## 回復

選擇同一批原manifest，勾選兩項授權後按「回復此批應用」，核對確認視窗。同批備份、現有版本與hash仍由引擎檢查；不需要為已執行migration的舊包重新做更新用Plan。

回復只處理應用，資料庫schema與新增檔保留；整庫還原及不相容migration不由此按鈕自動執行，仍依發布手冊另行核對。

## 執行需求與交付包

- Windows x64、.NET Framework4.8、Windows PowerShell5.1；不需安裝SDK或PowerShell7。
- 使用啟動程式的Windows帳號（UAC後身分），須具備對應WinRM／IIS／DB權限。工具不儲存帳密、不修改TrustedHosts或執行原則。
- EXE內含發布引擎，單一檔可執行；依版本準備的交付包仍另外提供，EXE可製作交付包，但不把測試前端改成正式。
- 程式目前未經Authenticode簽章；應以交付來源與 `SHA256SUMS.txt` 核對。不要宣稱已簽章。
- 內建工具抽取至僅Administrators／SYSTEM可存取的ProgramData/PMR-Release-Updater目錄；交付暫存與備份位置見遠端手冊，均不自動清除。

## 建置與驗證

執行[Build-ReleaseUpdater.ps1](../../tools/release/desktop/Build-ReleaseUpdater.ps1)。GUI使用Framework內建x64 csc；開發端另需.NET8 SDK及runtime／trim套件以建置自包含metadata helper，操作端不需SDK。預設輸出 `D:\SPC\release-staging\ReleaseUpdater-1.1.0`，包含EXE、使用說明及SHA256；_build含編譯測試／資源與畫面證據，不需交付。EXE及ZIP不提交Git。

核心20項、橋接新增4項與既有35項回歸通過；EXE已完成測試SPC與遠端正式Portal的僅Plan驗證，缺ZIP的負向結果也已驗證。實際Apply／Rollback未於本次執行，不等同正式更新演練完成。

## 製作交付包（1.1.0）

沒有 manifest 時，主畫面按「製作交付包…」，選 API／Web 發布資料夾，讀取來源 metadata，填寫 Web 版本，再按「從目標讀取現行版本」或填入實際舊版本。選輸出位置及唯一批號，若有 migration 再提供已審查 SQL／ID；按製作即產生 manifest、ZIP、逐檔 hash 並返回主畫面，接著自行預檢。

API 版本、完整來源 commit 及 EF migration 清單以靜態 PE metadata 讀取，不載入 API 或開啟 DB。缺少 commit 時需開發者提供真實值。SPC 舊 Web 依備份 hash 驗證。既有輸出拒絕覆蓋；先建立 .incomplete 目錄，驗證完成才更名。製作不會自動 Plan／Apply，亦不更新 IIS 或資料庫。

1.1.0 驗證：C#24、metadata6、PowerShell44項通過；SPC／Portal 實際來源成功產包，靜態 migration 清單與 EF 實際清單一致（50／27筆）。實際 EXE 測試站版本只讀查詢及產包成功，兩個視窗完成版面檢視。本次未執行 Apply／Rollback；站台發布不適用。
