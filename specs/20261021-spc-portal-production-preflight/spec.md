# SEC-FOLLOWUP-006 SPC/Portal正式更新前核對
2026-10-21；狀態IN PROGRESS，未發布。使用者要求執行下個工作，005/004保留待辦；本次只唯讀核對，無併行正式變更。

## 範圍/相依
- 主協調SPC；Portal為公式輸入/代理相依，TransFiles僅核對直寫協定，不改EXE或個人授權。
- SPC正式PMR-SPC-SRV，C:/inetpub/wwwroot/production/backend及frontend；Portal正式PMR-WEB，C:/inetpub/wwwroot/publish/portal-api及portal-web。不得套本機測試路徑或D:/Sites/PmrPortal。
- 正式發布已有意向授權；使用者已另行同意具體4筆SPC migration/角色與程序範圍，不承襲或混用KM nonce授權。
- 前置004維護未完成；005未發布，002仍受阻。本次不修改DB/IIS/環境設定。

## BDD/任務
- Given正式DB When唯讀比對migration/物件 Then記錄實際缺項，不以來源存在代表已套用。
- GivenAPI啟動會遷移 When未完成DDL審查/備份 Then不得直接以新DLL啟動正式站。
- [x] 版本/正式DB名稱/遷移歷史與必要物件唯讀核對。
- [ ] 比對完整payload與來源基準、前端正式API端點、設定/SSO/金鑰保存及相依契約。
- [x] 審查4筆DDL新增物件/權限/歷史資料影響，見[具體範圍](database-scope.md)。
- [x] 取得4筆DDL具體授權；正式COPY_ONLY/CHECKSUM備份及VERIFYONLY，正式備份複本4筆Up/重跑/原60表資料指紋與約束通過。
- [ ] 新舊匯入/公式/製圖業務相容性驗證及正式發布前Pkcs既有弱點處理。
- [ ] 必要測試、各自發布及真人登入/公式/圖表驗收。
- Happy核對/備份/複本/分站部署；Boundary歷史資料不回填及公式policy預設停用；Invalid目標/備份/遷移差異即停止；Regression原匯入/角色/SSO/SMTP/公告/圖表。
- 回復：同批應用備份並保留環境設定/附件/金鑰，新增schema保留；不自動Down、drop或整庫還原。直寫角色不得臨時放大帳號權限。
- [已執行核對](verification.md)。未改程式，build/測試站發布不適用；未完成全部發布驗證，不標DONE。

## 本次網站優先授權更新
- 使用者明示Runtime暫不升級、網站先發布。004改暫緩，不再作006硬性前置；先核對現有Runtime可載入交付runtimeconfig、完成既有驗證與正式發布檢查。先005單件結束，再006；只停對應網站pool，不變更共用Runtime/IIS binding或OS。
- 仍只執行已授權4筆SPC migration及Portal既有schema相容核對，不啟用公式policy、不授權正式個人writer、不搬測試資料至正式。

## 正式靜態站API相容修正（006發布阻擋）
- R1後端0.1.68/4筆schema已成功，瀏覽器揭露SpcWeb無/api代理，R1前端已單獨回復0.1.63。不安裝IIS模組、不改binding/驗證/SSO key。
- BDD Given Test且帶舊絕對API設定 When初始化client Then仍固定/api；Given Production明確指定本案既有正式端點http://172.16.119.140:8081/api When初始化client Then只允此端點（或明確同源/api）；其他主機/路徑/帳密/query/hash/協定均拒絕，不能外送JWT。環境寫入閘門/Authorization/401流程不變。
- 先新增Node單元測試紅燈再加resolveApiBase helper；Happy正式白名單，Boundary同源/尾斜線/測試忽略，Invalid測試站/其他主機/userinfo等，Regression既有同源API測試。Web升0.1.70，先備份發布測試前端、再正式R2完整受控包（API沿用0.1.68、pending0），最後正常SSO/圖表讀取驗證。
