# SEC-FOLLOWUP-001 主機安全/Runtime唯讀評估
日期：2026-10-21（Asia/Taipei）；狀態：DONE。僅盤點及決策，不代表任何升級或掃毒整合已執行。

## 實際主機結果
| 主機/用途 | Runtime x64/x86（NETCore及AspNetCore） | ANCM檔案版本 | Defender/其他防毒 | 應用版本 |
|---|---|---|---|---|
| PC-PA16560048：SPC/Portal測試、KM正式及測試 | 8.0.31、9.0.20、10.0.12，另6.0.16；x64/x86皆存在 | 16.0.23083.16 | WinDefend/WdNisSvc停止，WinDefend Manual；Get-MpComputerStatus不可用。SecurityCenter登錄WithSecure Client Security Premium/Windows Defender；WithSecure多個Hoster及Protected AV Hoster服務Running | KM正式1.6.4、測試1.6.6；SPC測試API0.1.67；Portal測試API1.0.190/Web1.0.233 |
| PMR-SPC-SRV：SPC正式 | 10.0.11及6.0.16；x64/x86皆存在 | 20.0.26205.11 | Running、AV及Realtime true、Normal；簽章1.461.118.0 | API0.1.60/net10 |
| PMR-WEB：Portal正式 | 8.0.25；x64/x86皆存在 | 18.0.26044.25 | Running、AV及Realtime true、Normal；簽章1.461.112.0 | API1.0.182.1/Web1.0.225/net8 |

- 三台目標IIS均Started，盤點pool均64bit、maxProcesses=1；不把單程序結果當成共享nonce已安全。
- 本機OS登錄ProductName=Windows 10 Enterprise、Build26200.9550（產品字串與build可能不一致，不據此斷言OS生命週期）；兩正式主機Windows Server2025 Standard/build26100.33438。
- 本機有WindowsDesktop 8/9/10；本次只評估網站8/10，不移除其他SDK/runtime或判斷其全部使用者。
- 本機Defender政策key存在，但DisableAntiSpyware/DisableAntiVirus值未見；不能推定防毒停止原因。WithSecure服務運行不等於已驗證檔案掃描API或授權。
- 探針：release-staging/security-followups/inventory.ps1以64位Windows PowerShell執行；原始inventory-20261021.txt保留本機，不提交。補查x86/OS/SecurityCenter及WithSecure服務輸出僅作證據，不輸出帳密。

## 官方基準（查詢日2026-10-21）
- [Microsoft .NET8下載](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)：8.0.32（2026-10-13），ANCM18.0.26278.32。
- [.NET10下載](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)：10.0.13（2026-10-13）。安裝包版本與ANCM DLL版本不能用相同數字直接相比。
- [官方支援政策](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)：8及9支援至2026-11-10；10至2028-11-14；6已不支援。保留舊runtime直到依賴確認，不直接卸載。
- [Hosting Bundle/IIS官方維護](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0)：包含Runtime/ANCM，安裝後可能需要IIS/WAS重啟，屬整機共用維護。SDK、NuGet套件修補與shared runtime/ANCM不可互相替代。
- [Defender MpCmdRun官方說明](https://learn.microsoft.com/en-us/defender-endpoint/command-line-arguments-microsoft-defender-antivirus)：掃描工具存在不等於服務可用；不因檔案存在宣稱已能掃毒。本機未執行掃描或啟用Defender。

## 最小維護決策與後續門檻
1. 002先規劃隔離/未掃成功不可正式保存之契約，再確認WithSecure受支援掃描介面、執行帳號及錯誤/逾時/感染測試；不另起第二防毒引擎或外傳附件。
2. 004逐機維護：PC-PA16560048須同時涵蓋正式KM及所有測試站，先演練8.0.32/10.0.13與共用ANCM；PMR-SPC-SRV目標10.0.13；PMR-WEB目標8.0.32。9.0.21及6的清理由使用者/依賴盤點另定，不能順手升級卸載。套件下載/簽章SHA256/離線安裝參數與舊版本安裝包留存屬004執行前門檻，本次不下載或執行。
3. 安裝前需有可驗證的主機快照/系統層回復或已演練之安裝卸載方案、IIS/app/config/keyring及DB備份；目前未證明具備主機層回復。網站資料夾備份不能回復Runtime/ANCM。逐台確認維護窗及所有受影響站台，正式共用維護另行具體授權。
4. 升級後核對NETCore/AspNetCore x64/x86、ANCM實體DLL版本與pool、逐站health/Login/SSO及檔案操作；Portal重啟後SMTP密碼仍可解密及手動Email保留，合成資料驗證且不寄信。失敗按演練的主機層方案回復，不隨意移除最新Runtime或還原整庫。
5. 005 KM 1.6.6正式發布先比對1.6.4來源/DB差異與002/003依賴、附件/金鑰/角色保護及SSO；不假設1.6.4→1.6.6只有DLL變動。
6. 006 SPC/Portal正式新版需先比對實際schema/EF history、版本policy、writer權限與代理契約。SPC來源新增20261019235606/20261020040256/20261020052530，僅確認來源存在，這次未重新查正式DB，不能宣稱正式pending清單已核准。正式Portal此次公告hotfix不包含其他新版。Portal net8在支援到期前需另案規劃net10遷移與相容測試，不能靠安裝Runtime10讓net8自動跨主版。

## 驗收與回復
- Happy：三主機可達、IIS/pool/runtime/ANCM/Defender及應用版號皆核對；成功。
- Boundary：區分32位操作端/64位IIS、x86/x64並存，以及本機同時有KM正式站；已核對。
- Invalid：本機Get-MpComputerStatus失敗如實記錄，未當成安全正常；服務/產品存在也未當成可呼叫掃描保證。
- Regression：只讀服務、登錄/檔案metadata及官方頁，無DB寫入/停站/策略修改/安裝；未展開002～006。
- 文件連結/數量/狀態核對通過。build/test/發布不適用（無產品程式變更），文件回復為revert本件提交；原始證據不提交。
- 完成001後剩5件（002～006）；TransFiles舊驗收紀錄另列，不計入本批5件。
