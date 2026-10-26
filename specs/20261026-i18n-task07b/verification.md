# 07B 驗證與發布
- 2026-10-26 DONE。28新鍵／84列draft，累計1607鍵三語。粒子固定UI／日期驗證／圖軸／legend／tooltip三語；原series名稱與C/U識別、數列、位置／單位／儀器／warning／來源原文保持。
- AC1/AC5：C/U×三語×四尺寸24情境，本機／發布通過，切換保留篩選／DOM／查詢次數與raw純文字；本機額外讀實際ECharts確認instance／series不變、U軸保留raw unit、tooltip跳脫與0。發布版不存取dev模組，內部option斷言不冒稱在發布站執行。英文手機截圖檢視。
- AC2/AC3：無效日期不送API、固定錯誤隨locale更新；不足20點與空資料不多送比較查詢；API503原文與HTML/i18n純文字；零值／null保持。81單元通過，含模板／draft hash／placeholder／零值與raw unit參數；build:test通過，既有chunk大小提示保留。
- AC4：三語C/U查詢逐欄from/to台北+08:00、location、particleSize、deviceCode trim、chartType保持；measurements固定pageSize50/sort desc與location-comparison取最新event時間保持。本機25／發布25通過，無未處理JS錯誤，業務API全攔截，無寫入。
- 只SpcSingleTest Web0.1.87/API0.1.72；IIS實際backend D:\SPC\release\test\backend與AppEnvironment=test核對，完整前端備份72檔、payload4／保護設定4、不重啟pool。
- 真匿名version/health HTTP200，environment=test/API0.1.72/status=ok；發布四檔SHA256等於dist，14同源API請求通過。
- 回復：核對test目錄，使用 .codex-tmp/i18n-task07b/backup-frontend 備份、資產先index最後，保留web.config/appsettings；DB／後端／正式未改。
- 譯文draft待TASK-20真人審稿。剩17件接續07C，另受阻1／暫緩2。
