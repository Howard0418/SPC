# 07A 驗證與發布
- 2026-10-26 DONE。73新鍵／219列draft，累計1579鍵三語。設備狀態／點位／監控固定文字、相對時間及狀態顯示三語，手機導覽堆疊，點位表格原欄位可橫捲。
- AC1/AC5：三頁×三語×四尺寸36情境，本機及發布通過，語言切換保留原篩選／DOM／URL／raw資料、不增加API；英文手機三頁截圖檢視。
- AC2：相對時間null／0／9.9／10／59.6／60／3599／3600／86399／86400保持原四捨五入與判定；0數值、online/stale/unavailable與running/stopped/unknown顯示保留。78單元通過，含模板／draft hash／placeholder／相對時間邊界。
- AC3/AC4：三語mapping409失敗保留草稿、重送逐欄DTO相同，原source/equipment/channel／unit／isEnabled／isPinned／warningLow=0／warningHigh／sortOrder／statusRole=ALARM／activeWhen=ZERO保持。兩頁timer 30000/10000ms前一毫秒不多讀、到點原頻率讀取、切語言不改timer。共41本機／41發布情境通過，無未處理JS錯誤。業務API全攔截。
- build:test通過，保留既有chunk大小與舊table直接tr編譯提示，本批不改業務或無關結構。
- 只SpcSingleTest Web0.1.86/API0.1.72；IIS實際backend D:\SPC\release\test\backend與AppEnvironment=test核對，完整前端備份69檔，payload4／保護設定4，不重啟pool。
- 真匿名version/health HTTP200，environment=test/API0.1.72/status=ok；發布四檔SHA256等於dist，14同源API請求通過。
- 回復：核對test目錄，使用 .codex-tmp/i18n-task07a/backup-frontend 備份，先資產後index、保留web.config/appsettings；正式／Chameleon／DB／後端未改。
- 譯文draft待TASK-20真人審稿；07拆A～E後本項完成剩18件，接續07B。另受阻1／暫緩2。
