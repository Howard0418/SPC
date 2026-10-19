# 驗證紀錄

2026-10-19；範圍：工具及文件，非正式站更新。

| 驗收 | 證據／結果 |
| --- | --- |
| AC1／AC3／AC5 | 自動化主機/目標/forbidden output/hash/ZIP/路徑/保留設定/缺授權拒絕；本機 CLI 對正式包 Plan 因主機不符回 exit 1，在 IIS/SQL 動作前阻擋。 |
| AC2 | 檔案交易使用真實暫存檔，IIS／SQL 替身驗證 stop→IIS/檔案/DB備份→migration→apply→smoke 順序；DB 備份失敗不遷移、不改原檔且回復 pool。 |
| AC4／AC6 | smoke失敗回復舊檔與舊版 smoke；回復失敗維持停止；backup hash 竄改、重用 ID、不同備份身分均拒絕；原停止pool恢復停止。 |
| migration scope | 來源 catalogue、正式 pending 與核准 SQL ID 必須精確一致；自動化比對與漏列拒絕通過。 |
| 相容性 | PowerShell 7 與 Windows PowerShell 5.1 各 21 項測試通過；所有 PowerShell 檔案 parser 語法檢查通過。 |
| 真實交付包 | 以既有 SPC API0.1.60/Web0.1.63、Portal API1.0.178/Web1.0.221 產生兩份離線正式 bundle，ZIP及逐檔SHA256驗證通過；設定／私有資料排除。 |
| EF 審查工具 | SPC net10 與 Portal net8 檢查器均成功匯出 GetMigrations catalogue 及 idempotent SQL，不執行 Program、不開 DB；產出置 ignored release-staging/production-tool-verify，不提交。 |

Portal 檢查器第一次因 NuGet 網路受限還原失敗；取得網路存取後從 NuGet 正常還原並完成；無來源／版本變更。移除 ignore-failed-sources，避免缺套件時以不符版本繼續。

正式 IIS／SQL 適配器：本次未執行實際正式 Apply／Rollback，不能宣稱正式機現場演練或完整登入業務驗收。這是明確非範圍，現有正式站保持原狀。測試站發布不適用（僅工具與文件，不修改產品程式）。

回復：若未來工具更新需退回，可還原工具來源至前一提交；不需更動網站／資料庫。未來正式應用 rollback 必須同批備份且 schema 向後相容，新增檔保留、不得自動還原整庫。
