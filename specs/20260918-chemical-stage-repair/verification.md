# 驗證（2026-09-18）
- 正式：PMR_SPC_2026缺SamplingStage欄位；只唯讀盤點，未修改任何正式資料或schema。
- 測試：PMR_SPC_TEST，460筆交易修正成功，取樣階段OPEN262/CLOSE198。既有MIDDLE與其他線別未列入UPDATE。
- 受影響完整資料before/after/result見../../release-staging/chemical-stage-repair-20260918/test-20260918-091428/。其他所有業務欄位逐筆相同；RowVersion由SQL自動遞增，屬預期行為。
- 第一次交易檢核把RowVersion視為不變欄位而報錯，已完整rollback；第二次允許系統RowVersion變動後成功commit。
- 更新後相同修正條件0筆，避免重跑重複改動。保留早班技術代碼SamplingPhase=OPEN，階段SamplingStage獨立OPEN/CLOSE；此為現行兩維度應用契約，不再把階段稱晚班。
- 正式修正待相容性範圍確認，不宣稱兩庫都完成。未發布任何網站版本。

## 正式升級已核准後的環境核對
- 使用者已同意限定正式相容性升級及460筆修正，無需再次授權。
- 正式SPC：PMR-SPC-SRV.pmr.com.tw / 172.16.119.140:8081，IIS實際C:/inetpub/wwwroot/production/backend；實際SQL目標172.16.110.16/PMR_SPC_2026，environment=production。
- API實際版本0.1.57，DLL ProductVersion commit f782d370d281edb26f17a2c743d81b25fa7cc974；本機及遠端release-manifest仍記載0.1.49/7309663，屬過期文件，未據此部署。
- 透過正式DNS名稱的既有WinRM驗證已可讀取部署檔；未改TrustedHosts或權限。正式DLL不含SamplingStage/ChemicalSamplingStage，需程式相容更新。
- 以正式PDB source document SHA256逐檔比對：138份業務來源，Git f782d370匹配110份、本機補足16份，仍12份不符（含AuthController、ManualMeasurementsV1Controller、MasterDataV2Controller、Models、AppDbContext、Snapshot、Program、ChameleonStatusService、SpcPagePermissions、SpcService、UploadService、SpcModels）。缺清單位於../../release-staging/chemical-stage-repair-20260918/production-baseline/unavailable-source.json。
- 正式主機指定C:/SPC、D:/SPC無來源；已知備份目錄僅前端備份，無核實後端來源。不能用現有整包測試程式代替限定功能升級。
- 正式Portal網址/主機尚未定位：本機IIS全為測試站，舊正式設定與文件已過期，已向使用者詢問正確位置。
- 本次只讀環境/PDB比對及準備來源檔，未執行正式schema migration、資料更新或發布。待正式版來源位置及Portal位置提供後接續；測試庫460筆已修正結果維持。
