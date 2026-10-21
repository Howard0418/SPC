# Pkcs修補驗證
2026-10-21；006安全套件子步驟完成，正式發布未執行。
- Pkcs直接參照10.0.13；官方NuGet版本頁與net10相容性已確認。線上`dotnet list package --include-transitive --vulnerable --source https://api.nuget.org/v3/index.json`未回報已知弱點。離線restore曾停用audit僅為重用快取，未以此冒稱audit成功。
- 修改前後API均169 passed/6 failed/8 skipped，已在baseline.log證明相同6項。修正4個測試檔：舊簽章SSO先有既有操作者；message封裝；共用WE與拒絕項目專屬規則；補CHEM大類與既有製程名稱顯示契約；InMemory交易警告只在刪除單元fixture忽略，驗證payload.actor=qa-user，CreatedBy仍為既有System，不宣稱修復通用稽核欄位或測過交易rollback。
- 最終API175 passed/0 failed/8 skipped；8項需另外opt-in/外部依賴，未視為通過。Chemical59 passed/0 failed/0 skipped，合計234 passed。未改生產業務邏輯、EF版本、schema或角色。
- Release publish成功，deps解析Pkcs10.0.13且無9.0.4引用；API0.1.68.0。
- 已核對SpcSingleTest/D:/SPC/release/test/backend/PMR_SPC_TEST，備份受限ACL與逐檔hash後發布，health=test/0.1.68。既有appsettings/web.config及量測/公式/F歷史/policy/migration資料指紋不變；Web0.1.69不動。確切備份與檔數在release-staging/spc-pkcs-security-20261021/deploy-proof.json，不提交。
- 回復同批backend備份，保留環境設定/private-data；不還原DB。正式仍0.1.60，未停正式服務或套用4筆DDL。文件/diff通過，Git同步；主要4件/TransFiles1件不變。
