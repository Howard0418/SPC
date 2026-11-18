# PORTAL-CHEM-ADJUST-CONTROL-NOTIFY-1118-TASK-001 驗證

## 行為與自動化

- SPC保存成功回應提供伺服器端添加／稀釋管制值提交前後值，以及線別、槽位、分析項目；Portal不使用瀏覽器舊值判斷異動。
- 只有添加或稀釋管制值有效值實際改變才通知；同值、只改槽容／備註、SPC拒絕保存均不通知。
- 通知對象為啟用的`quality_assurance`及`process_engineering`角色帳號；訊息含項目、前後值、修改者、時間、頁面連結及規格／範圍檢討提示。
- 通知失敗只記錄安全診斷，不回滾已提交參數。
- Portal API 293項、SPC維護11項通過；Portal與SPC API Release build均0警告、0錯誤。

## 測試區發布

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

## 待人工驗收

1. 在測試區選一筆藥液項目修改「添加管制值」或「稀釋管制值」並儲存。
2. 確認品保與製程角色人員各收到一次Synology Chat私訊，且訊息列出正確前後值。
3. 再以相同值儲存或只修改備註，確認不再通知。
