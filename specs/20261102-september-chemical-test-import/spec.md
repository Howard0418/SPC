# 九月藥液隔離測試匯入
- ID：SPC-HISTORICAL-IMPORT-1102-TASK-002-SEP；既有TASK-002子範圍，不新增核心計數。
- 日期2026-11-02；合格1146筆測試匯入與對帳完成，78來源例外仍未完成。主系統TransFiles，相依SPC測試；不涉及Portal、咬蝕、落塵或正式，無schema／角色／IIS變更。
- 使用者指示「換9月份」，沿用已同意的獨立測試項目及報表日測試時間／時段，不作現場歷史。十月成果及16例外保留，不回頭重寫。

## 需求與BDD
- AC1：Given既有來源，When選2026-09，Then只盤點9/1～9/30；檔名與表內日期相符，保留SHA256及列格，來源不改。
- AC2：Given原公式、原始初複驗及明確F引用，WhenDecimal與SPC引擎各自計算，Then容差1e-8內與Excel一致；缺值／日期衝突／公式差異隔離並給調整建議，不猜F或更槽版本。
- AC3：Given測試API及PMR_SPC_TEST實際環境與本人低權限身分，When備份VERIFYONLY通過、預覽及prepare成功，Then只在新TEST-SEPCHEM項目受控commit；每筆重送不重複、網站追溯與圖表查回吻合。
- AC4：Given十月及原業務資料，When執行九月，Then定義／政策／全域F快照保持；專用特性RECORD_ONLY避免業務郵件，trace仍保留raw判定。
- 九月fixture固定報表日12:00+08:00，明示合成測試時間。避免午夜落到前一UTC日造成既有圖表起日漏點；不修改共用日期篩選、不修改十月時間。
- 日期範圍及月份與marker要一起鎖定；沿用十月預設行為，不容許九月manifest搭配十月marker。

## 回復
新增前COPY_ONLY／CHECKSUM／VERIFYONLY；回復限定本批專用PPC停用，保留不可變追溯。完整DB回復須先比對後續其他測試寫入，不覆蓋十月或別件成果。
