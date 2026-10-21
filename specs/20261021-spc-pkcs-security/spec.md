# SEC-FOLLOWUP-006 SPC Pkcs安全修補
日期2026-10-21；狀態DONE（006整體仍未完成），屬006必要子步驟，不新增主要工作計數。使用者要求執行全部小工作；每次僅執行此項，不同時發布其他系統。
- 現況：SqlClient傳遞相依Pkcs9.0.4產生NU1902/NU1903；採直接參照官方10.0.13(net10相容)，不更改EF/SQLClient/公式等業務版本。
- Given完成restore/build/publish When核對assets及deps Then僅解析與交付Pkcs10.0.13，原9.0.4不存在；套件audit完整結果另列，不以停用audit冒稱無弱點。
- Happy API/化學回歸；Boundary SQL與CMS載入；Invalid仍有9.0.4則拒部署；Regression formula/chart/SSO/ETL與DB指紋/設定保留。
- API版本0.1.68，Web0.1.69不變；測試部署只到SpcSingleTest/backend，核對DB=PMR_SPC_TEST/環境test，保留設定與private-data。正式不動，無migration/角色變更。
- [x] 最小套件調整與Release publish。
- [x] 相關build/test、deps與audit核對。
- [x] 測試站備份/受控發布/health/設定及DB指紋核對。
- [x] 文件/Git同步。
- 回復僅同批backend應用備份，保留設定/資料/keyring，不Down或還原DB；失敗先還原再啟站。首次實作前已在ai_docs/10_change_log.md記錄目的。
- 官方依據https://www.nuget.org/packages/System.Security.Cryptography.Pkcs/10.0.13 。

## 發布前回歸基準修正
修改前後API皆169通過/6失敗/8略過；確認6項為舊驗收/fixture與現行基準差異後，僅修必要測試，不改產品業務。依CHANGELOG_CUSTOM 2026-08-12全系統WE規則，取消項目專屬群組；SSO舊簽章只能登入既有帳號；回應message封裝與主檔fixture保持完整。InMemory刪除單元測試不宣稱交易原子性，SQL交易由隔離回歸另驗。修正後須全套再驗。
