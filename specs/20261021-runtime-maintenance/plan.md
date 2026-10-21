# 逐機維護計畫（待具體授權）

| 次序 | 主機 | 預定安裝 | 整機IIS影響 |
|---|---|---|---|
| 1 | PC-PA16560048 | 8.0.32 Hosting，完成後10.0.13 Hosting，使共用ANCM最後維持20.x | KM正式1.6.4、KM-Test1.6.7、Portal測試190/233、SPC測試API0.1.67及全部IIS站台 |
| 2 | PMR-SPC-SRV | 10.0.13 Hosting | SPC正式API/Web及全部IIS站台 |
| 3 | PMR-WEB | 8.0.32 Hosting | Portal正式API1.0.182.1/Web225、Default Web Site及全部IIS站台 |

只執行獲核准主機，逐台完成驗收後才進下一台。首台即含KM正式，不能在「測試站自動發布」授權下進行共用系統安裝。未核准維護窗前不停止WAS/IIS或執行EXE；不自行OS重啟。

## 執行前最後門檻
1. 核對機器身分、套件SHA512/簽章、目前Runtime/ANCM、IIS站台與pool狀態；保留所有原始狀態。確認無另一發布/DB/防毒工作進行。
2. 記錄使用者可回復備份識別/時點/回復方式；在停止業務寫入後取得最新一致快照或系統層備份，保存IIS設定、應用設定/附件、DataProtection/keyring及資料庫備份。只保留網站資料夾不能回復Runtime/ANCM。
3. 使用隔離複本/可復原環境驗證安裝與回復，再安排真實主機。現在未提供隔離複本，不能把下載驗簽當安裝演練完成。
4. 從下載根傳送必要兩份或一份EXE至固定維護目錄，重新hash核對；不傳送應用設定或憑證。維護/安裝日誌須限Administrators/SYSTEM，不提交Git。

## 安裝操作契約（尚未執行）
- 依Microsoft文件，管理員以`/install /quiet /norestart`啟動指定Hosting Bundle；如用Start-Process，使用WindowStyle Hidden、Wait及PassThru記錄exit code。所有包已驗簽，但未執行help/layout/安裝。
- 明確保留x86/x64 Runtime及ANCM/SharedFramework，不用OPT_NO_*略過所需元件；既有同版本band安裝選項可能保留，執行前核對registry選項並在本次安裝明確指定所需0值。
- exit0僅代表安裝成功；3010代表需重啟，不能當成已完成驗收；其他非0停止後續主機。禁止忽略錯誤碼/安裝後自動回報成功。
- Hosting安裝可能需要WAS/W3SVC重啟，只有在核准停機後執行，會影響全機IIS。若需要OS重啟，需另有明確授權及可重新連線驗證安排，不能因norestart就假設不需要重啟。
- 不移除.NET6/9、不改SDK/NuGet、不發布新版網站、不執行migration/改防毒。

## 驗收與回復
- 每台核對NETCore/AspNetCore x64/x86及實際ANCM DLL，pool/站台還原原狀；檢查安裝前存在的reboot/file rename旗標及新事件，不能把原問題當本次新增。
- 逐站Login/health與匿名隔離、Portal→SPC/KM真實SSO；KM只讀文件/附件。Portal測試需保留已驗證SMTP密碼可解密與Email，不寄信；nonce表/DBUTC消費應正常。不得以修改正式資料作測試。
- 任一台失敗即停止後續，保存日誌並按已驗證的整機回復方案處理；新舊Hosting EXE只是維修材料，不等於可直接降ANCM。不要任意卸載共用模組或整庫還原。
- 回復需保留停寫期間業務一致性及本次之前的當日應用/金鑰變更；主機回復完成且整機逐站驗證成功才解除維護。

## 請使用者最後確認內容
- 先授權哪台/哪些主機、維護時段、允許的短暫全機IIS停機/是否含必要OS重啟。
- 維護前提供/核對快照或整機備份識別及回復執行方式、隔離演練環境。
- 本階段不視為安裝授權，SEC-FOLLOWUP-004仍未完成。
