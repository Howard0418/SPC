# Runtime/Hosting維護準備與驗證
日期2026-10-21；SEC-FOLLOWUP-004尚未完成，沒有執行任何安裝。

## 已備妥交付物
- 7份官方新舊Hosting Bundle均以release metadata SHA512核對一致、Authenticode Valid且簽署者Microsoft Corporation。
- 位置：D:/SPC/release-staging/runtime-maintenance-20261021/packages。原始selected-packages.json/package-proof.json及EXE不提交Git；本頁記錄可審查檔案識別。

| 安裝包 | 用途 | 大小MiB | SHA256 |
|---|---|---:|---|
| dotnet-hosting-8.0.32-win.exe | new | 106.7 | 3DFD552D4C1F776EE4E971B8794AC09E2FD39A493399FC5AB777F14745F863DC |
| dotnet-hosting-8.0.31-win.exe | previous-local | 106.7 | A6A40D15773F25A0DDE8826C2A6D652FC104F54B574535F3E84AD716D00F98CE |
| dotnet-hosting-8.0.25-win.exe | previous-portal | 106.7 | DDDF646A3F27F1381AD1506DC1B7D9B8A577D97D5FBC17E17657888DFD07B0BF |
| dotnet-hosting-10.0.13-win.exe | new | 112.2 | 6CD3B2E13583BCD46BE09F932159A4C2C840D685D451E33E7000F6337CDE6C06 |
| dotnet-hosting-10.0.12-win.exe | previous-local | 112.1 | CCC5B497C6F179D4ACF996FF6EB7D015959BC85F87DDE4CE5EE5C426847576E1 |
| dotnet-hosting-10.0.11-win.exe | previous-spc | 111.9 | BE37354242659A50ED036ACE7049095B2B1DA91A3437EE64A17F1EF83695D171 |
| dotnet-hosting-6.0.16-win.exe | previous-local-ancm-candidate | 68.6 | A6FBC64CAB98C8FB094100669F8E1F410E7BA316398C0DA99BBC8812BC568EEC |

## 官方依據
- [8.0下載](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)、[8.0 metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json)，目前8.0.32，ANCM18.0.26278.32。
- [10.0下載](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)、[10.0 metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json)，目前10.0.13，ANCM20.0.26277.13。
- [6.0 metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/6.0/releases.json)僅備6.0.16舊包作舊模組回復辨識候選，不代表要安裝已不受支援版本。保留舊包不等於已完成降版演練。
- [Hosting Bundle共用模組及IIS重啟](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-10.0)、[Windows安裝參數/exit code](https://learn.microsoft.com/en-us/dotnet/core/install/windows)。

## 現況及回復證據
- 使用者明確回覆「三台皆有可回復備份」。尚未取得備份產品/識別/時點/復原操作證據，不假稱代理驗證或執行過整機回復。
- 維護前仍需記錄對應備份/快照識別、取得停寫後最新一致備份及可執行回復的人員/方式；不可拿過舊快照回復而遺失本日Portal金鑰/設定或KM新程式/文件。
- 三機Windows CBS/Update重啟旗標均false，但PendingFileRenameOperations均存在。未清除或修改旗標；不能保證安裝後無需OS重啟。
- 本機32位相容IIS子應用查詢一度COM失敗，改64位Windows PowerShell核對子應用0；兩正式主機查詢無列出子應用。整機IIS維護仍包含Default Web Site/所有既有站台。
- 未安裝、未停止站台/服務、未建立新快照或整機備份、未變更DB/防毒/應用版本。本件準備完成但升級未完成，剩4件不變。
- 文件內容/路徑/7包數量/簽章/hash核對通過；build不適用。安裝/演練/升級後驗證尚未執行。

## 無演練環境之方案調整
- 使用者確認「沒有演練環境」。改提出逐台維護/每台驗收後再繼續方案，仍待確認此限制、備份識別/回復方式及具體停機範圍。
- 未完成隔離安裝或回復演練，不宣稱回復已驗證；本次僅文件調整，build與網站發布不適用，沒有執行安裝或停站。

- 後續授權：使用者指定2026-10-21 18:00台灣時間、允許必要重啟，備份命名backup_日期時間、本人負責回復。僅記錄授權；實際備份識別/時點及回復方式尚未核對，無安裝/停站/排程。

## 15:30 備份方案實際進度（取代先前尚無方案狀態）
- 使用者委託代為指定回復方案；已核對本機實體雙磁碟、WinRE及64位WBAdmin，系統映像備份已啟動至D:/WindowsImageBackup，C及allCritical、VSS copy。15:30日誌顯示C約20%，仍執行中；尚未通過完成碼及版本目錄核對，不標示備份完成。未停站或重啟。
- 已核對兩正式VM之客體/宿主GUID、VSS整合健康及CSV約6376GiB可用；尚未建立checkpoint。18:00停寫後採ProductionOnly，失敗不退回standard；實際識別與時點屆時記錄。詳細回復方式見[計畫](plan.md)。
- D應用/附件備份及完成後目錄核對仍待；無整機還原演練，不宣稱可回復實測。Runtime仍未安裝，004未完成。
