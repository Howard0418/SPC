# PORTAL-CHEM-ADJUST-CONTROL-NOTIFY-1118-TASK-001 驗證

## 行為與自動化

- SPC保存成功回應提供伺服器端添加／稀釋管制值提交前後值，以及線別、槽位、分析項目；Portal不使用瀏覽器舊值判斷異動。
- 只有添加或稀釋管制值有效值實際改變才通知；同值、只改槽容／備註、SPC拒絕保存均不通知。
- 2026-11-19依使用者確認，通知通道由Synology Chat改為既有SMTP Email；收件人為啟用、未封存、Person且有有效個人Email的`quality_assurance`及`process_engineering`角色人員，重複Email去重。
- Email含項目、前後值、修改者、時間、頁面連結及規格／範圍檢討提示。
- 通知失敗只記錄安全診斷，不回滾已提交參數。
- Email修正先以舊建構式得到預期紅燈，再完成4項焦點測試；Portal API完整298項、Portal API／Web Release build均0警告、0錯誤。

## 測試區發布

- 2026-11-19 Email版最終：Portal API `1.0.280` → `1.0.281`；Web `1.0.318` → `1.0.319`。由乾淨`1cfe152`隔離來源只套用本件Email修正；R2補強任何通知例外皆不回滾已提交參數，避免底層非SMTP例外造成保存後錯誤。
- 測試Login與引用JS／CSS皆HTTP 200且MIME正確；`/app-version`為`1.0.319.0`，瀏覽器實際API基底`http://172.16.110.27:8091/health`為200／`test`，同源`/api/auth/me`匿名401。
- API實際DLL為`1.0.281.0`，Web實際DLL為`1.0.319.0`；R2發布前備份`D:/PmrPortal_Backups/chemical-adjust-email-test-r2-20261119-131754`，API設定檔發布前後雜湊保持。

- Portal：API `1.0.269` → `1.0.270`；Web `1.0.307` → `1.0.308`。
- SPC：API `0.1.137` → `0.1.138`；Web `0.1.143` → `0.1.144`。
- Portal Login／`app-version`／實際JavaScript與CSS／測試API health／同源API匿名401通過，環境為`test`。
- SPC首頁與實際JavaScript／CSS皆HTTP 200且MIME正確；`/api/health`及`/api/version`皆HTTP 200、API `0.1.138`、環境`test`。
- SPC測試區發布前備份：`D:\SPC\release\backup-control-notify-test-20261118-214836`。
- 正式區未發布。

## 進度表同步

- 「小工作總表」第325列更新為「測試區已發布／待人工驗收」，待辦維持「是」；「專案進度表」新增原生里程碑，原有計數不變。
- 更新前備份：`進度表-20261118-control-notify-before.xlsx`。
- 原檔更新前SHA256 `A8E8BACAFE4D96EA942032B9788F24DA2738F42E4A57B662130CA1E57495DE27`；更新後 `90B568195E42F438A68EAE6A5CBA286064EA67FD52BFC24BCB59BAFE7C532D2A`。
- Artifact Tool重開、152個原生圖形、公式錯誤0、ZIP 13 entries／空名稱0通過；Microsoft Excel人工開啟未執行。
- 2026-11-19 Email版狀態已同步原進度表第325列及原生里程碑；更新前備份`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表-管制值Email通知-20261119-131653.xlsx`，取代後SHA-256為`AE6A55829977A31A21FD494CF41A0D7B3EBF1382D0FE6E74D6E7910BCFD49269`，公式錯誤0、OpenXML SDK驗證0錯誤、預覽已目視。中央未完成30件、SPC相關7件。
- R2最終版本已再同步為API281／Web319；更新前備份`C:/Users/ihao_ting.PMR.000/Desktop/SPC開發/Voice/備份/進度表-管制值Email通知R2-20261119-1319.xlsx`，取代後SHA-256為`B955F1CE7AA9158FB380A79AFFCF8336F1F6F65D7AC2E2099BAC25B2E7DCEEB5`，OpenXML SDK驗證0錯誤。

## 待人工驗收

1. 在測試區選一筆藥液項目修改「添加管制值」或「稀釋管制值」並儲存。
2. 確認具有效個人Email的品保與製程角色人員各收到一次Email，且郵件列出正確前後值。
3. 再以相同值儲存或只修改備註，確認不再通知。
