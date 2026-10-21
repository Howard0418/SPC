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
3. 使用者已確認沒有演練環境。替代提案為：核對可執行的整機回復方式及最新備份後，在核准維護窗只升級第一台，逐站驗收通過才安排下一台。安裝與整機回復均未經隔離演練；首次真機升級不是演練，須先取得使用者對此限制及停機範圍的明確確認。下載驗簽不等於安裝或回復驗證。
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
- 任一台失敗即停止後續，保存日誌並由可執行回復的人員按事先核對的整機回復方案處理（目前未經演練）；新舊Hosting EXE只是維修材料，不等於可直接降ANCM。不要任意卸載共用模組或整庫還原。
- 回復需保留停寫期間業務一致性及本次之前的當日應用/金鑰變更；主機回復完成且整機逐站驗證成功才解除維護。

## 請使用者最後確認內容
- 先授權哪台/哪些主機、維護時段、允許的短暫全機IIS停機/是否含必要OS重啟。
- 維護前提供/核對快照或整機備份識別、時點及回復執行方式/負責人；無須再提供不存在的演練環境。確認接受未經隔離安裝/回復演練的限制後，才採逐台維護替代方案。
- 本階段不視為安裝授權，SEC-FOLLOWUP-004仍未完成。

## 2026-10-21 維護授權更新
- 使用者同意逐台方式，指定當日18:00（Asia/Taipei）開始，允許必要IIS及主機重啟；不在18:00前安裝/停站。
- 備份命名方式為 `backup_日期時間`，回復負責人為使用者本人；此為命名規則，不是已核對的實際備份識別。執行前核對第一台最新備份完整名稱/時間及本人可執行回復的方式。
- 無隔離演練環境之限制已告知並獲確認。當前沒有建立排程、自動安裝或執行重啟；到時需在有效工作階段完成執行前核對。

## 使用者委託指定回復方案（2026-10-21）
使用者要求代為指定，已確認宿主機管理可達；不再把pmr-km-backups的8/4網站檔案當整機備份。
- PC-PA16560048：實體ASUS，C=Disk0、D=Disk1/NTFS/Healthy，D約882GiB可用，WinRE Enabled；64位wbadmin可用，get versions未找到備份。先建立C及allCritical系統映像至D:/WindowsImageBackup（原位置不存在，受限ACL），另保存D上六個IIS站台與KM storage。此為系統卷回復+應用檔案備份，不是D全卷映像；不可把C/D位於同一硬碟的假設套用（已確認不同DiskNumber）。
- 本機備份使用VSS copy，不清交易記錄、不刪舊備份、不停站/重啟；不安裝新工具。準備階段可先執行耗時系統映像，18:00安裝前核對最新狀態，若其後有需納入的C/金鑰變更則更新備份。
- PMR-SPC-SRV：客體名PMR-SPC、VM GUID d3e5f990-4b43-485e-8036-c95a27ebf8aa，宿主PMR-SRV-04.pmr.com.tw。
- PMR-WEB：客體名PMR-WEB、VM GUID 813fcee5-5bed-478a-8fa5-d100985f898b，宿主PMR-SRV-01.pmr.com.tw。
- 兩VM之Guest Parameters與宿主Get-VM GUID一致，checkpoint目前Production（可退回standard），指定18:00停寫後採ProductionOnly建立維護前checkpoint，失敗不得退回standard。事先核對CSV真實儲存空間，不以宿主C槽空間代替；不建立/套用checkpoint或改VM設定於核對階段。
- 名稱使用PMR_PREUPDATE_主機名_yyyyMMdd_HHmm，維護批次為20261021_1800；實際備份時點另記，不能提前把未建立checkpoint標完成。VM checkpoint是同儲存回復點，不是異地完整備份。
- 本機回復由本人透過已啟用WinRE「系統映像修復」選D映像，搭配同批D應用備份與DB一致性核對；兩VM由既有宿主維運連線按精確GUID/checkpoint還原，還原本身破壞性，失敗時先停止後續更新並核對業務一致性，不自行整庫回復。
- 尚無實際整機還原演練，限制已告知/接受；備份完成、目錄可列與WBAdmin/Hyper-V metadata只是可用性核對，不冒稱還原成功。
官方依據：Microsoft wbadmin start backup及Hyper-V checkpoints文件（含vssCopy/allCritical與ProductionOnly語意）。
