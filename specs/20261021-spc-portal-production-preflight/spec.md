# SEC-FOLLOWUP-006 SPC/Portal正式更新前核對
2026-10-21；狀態IN PROGRESS，未發布。使用者要求執行下個工作，005/004保留待辦；本次只唯讀核對，無併行正式變更。

## 範圍/相依
- 主協調SPC；Portal為公式輸入/代理相依，TransFiles僅核對直寫協定，不改EXE或個人授權。
- SPC正式PMR-SPC-SRV，C:/inetpub/wwwroot/production/backend及frontend；Portal正式PMR-WEB，C:/inetpub/wwwroot/publish/portal-api及portal-web。不得套本機測試路徑或D:/Sites/PmrPortal。
- 正式發布已有意向授權；本次發現的具體4筆SPC migration/角色與程序範圍需另行確認，不承襲KM nonce授權。
- 前置004維護未完成；005未發布，002仍受阻。本次不修改DB/IIS/環境設定。

## BDD/任務
- Given正式DB When唯讀比對migration/物件 Then記錄實際缺項，不以來源存在代表已套用。
- GivenAPI啟動會遷移 When未完成DDL審查/備份 Then不得直接以新DLL啟動正式站。
- [x] 版本/正式DB名稱/遷移歷史與必要物件唯讀核對。
- [ ] 比對完整payload與來源基準、前端正式API端點、設定/SSO/金鑰保存及相依契約。
- [ ] 審查4筆DDL完整物件/權限/歷史資料影響，取得具體授權；正式備份VERIFYONLY與複本演練。
- [ ] 必要測試、各自發布及真人登入/公式/圖表驗收。
- Happy核對/備份/複本/分站部署；Boundary歷史資料不回填及公式policy預設停用；Invalid目標/備份/遷移差異即停止；Regression原匯入/角色/SSO/SMTP/公告/圖表。
- 回復：同批應用備份並保留環境設定/附件/金鑰，新增schema保留；不自動Down、drop或整庫還原。直寫角色不得臨時放大帳號權限。
- [已執行核對](verification.md)。未改程式，build/測試站發布不適用；未完成全部發布驗證，不標DONE。
