# 小工作與測試規劃
- SPC-ADJUST-20261028／v2；[規格](spec.md)｜[計畫](plan.md)。以下9件全數待執行，不把規劃父批次重複計入。

|建議順序／代號|優先／主系統|工作與相依|Happy Path|Boundary Case|Invalid Input|Regression Risk|
|---|---|---|---|---|---|---|
|1 SPC-ADJUST-1028-TASK-008|P0／Portal，相依SPC|保存成功後歷史查詢失敗（AC-008）|成功後GET可讀／可重試|保存成功但讀取逾時、切換競態|401／403／404／500／網路失敗|只重試GET，不重複保存|
|2 SPC-ADJUST-1028-TASK-007|P0／Portal，相依SPC|N2開收線載入（AC-007），依008共用診斷|早／中班×開／收線正確資料|無資料、跨日、N1對照|缺設定／無效階段明示|DV2不適用與既有日報保持|
|3 SPC-ADJUST-1028-TASK-009|P1／SPC，相依Portal與來源Excel|QE2分析值與判定差異（AC-009）|同來源公式／F表／版本逐層核對|原始精度、複驗、0、近規格上限|缺來源／單位不符不得猜係數|銅／雙氧水與既有trace保持|
|4 SPC-ADJUST-1028-TASK-001|P1／SPC|公式儲存後重算重繪（AC-001）|固定資料切標準／MR／Sigma得預期界限|樣本不足、零變異、手動界限／歷史分段|無效設定、未支援圖型、儲存失敗須明示|OOC、報表、Overall、既有設定與資料不改寫|
|5 SPC-ADJUST-1028-TASK-002|P1／Portal，相依SPC|藥液查詢NG與複驗優先（AC-002）|有複驗顯複驗；無複驗顯初驗|複驗0、初驗NG後複驗OK、等於規格、raw/stored精度|空白、非數字、缺規格、API失敗不可假OK|既有日期公式／trace／初複驗保存、權限、查詢條件|
|6 SPC-ADJUST-1028-TASK-003|P1／SPC|測試入口根網址修正（AC-003）|點擊根網址為portaltest HTTPS|有無尾斜線、硬重新載入、不同入口|缺設定不能退回舊IP；錯環境配置明示|正式目標保持、/Spc/Launch與安全回跳、built bundle實測|
|7 SPC-ADJUST-1028-TASK-004|P2／SPC|月報三個獨立Sheet（AC-004／004X），依001判定規則穩定|製程／藥液管制／藥液趨勢各自資料|空類別、跨月、閏月、既有產品資料|未知分類／錯誤月份不可混入別類|畫面／Excel／排程分類一致，原日期與產品資料保留|
|8 SPC-ADJUST-1028-TASK-005|P2／SPC|各Sheet已／未處置件數（AC-005／005X），依001及004|雙類／主副圖去重，已＋未＝異常|1001點以上、月末、同組多告警、查詢後結案|缺處置紀錄列未完成；讀取失敗不當0|Overall口徑、排除、分類總和／Excel、不得回推月底狀態|
|9 SPC-ADJUST-1028-TASK-006|P3／SPC|常態直方圖淺藍柱（AC-006）|兩種分析頁柱色一致淺藍|暗／亮、hover、手機、單柱／零資料|無量測仍顯既有空資料狀態|bins、頻數、常態曲線、界線與下載數值不變|

- [ ] TASK-001 實作／驗證／測試發布／文件及進度同步。
- [x] TASK-002 複驗優先與保存判定：正式發布及本人登入查詢驗證完成，詳[正式紀錄](../20261028-chemical-history-refresh/formal-release.md)。
- [ ] TASK-003 實作／驗證／測試發布／文件及進度同步。
- [ ] TASK-004 [實作／驗證／測試發布／原Excel同步完成](../20261028-monthly-category-sheets/verification.md)；正式待授權。
- [ ] TASK-005 實作／驗證／測試發布／文件及進度同步。
- [ ] TASK-006 實作／驗證／測試發布／文件及進度同步。

規劃交付檢查：[v1歷史](verification.md)／[v2截圖補充結果](../20261028-chemical-screenshot-followup/verification.md)。優先順序為後續選取依據，不是現在執行授權。

- [x] TASK-008 保存成功後歷史查詢失敗：正式發布及本人登入唯讀驗證完成，詳[正式紀錄](../20261028-chemical-history-refresh/formal-release.md)。
- [x] 007 N2開／收線現版驗證完成（原異常未重現）；詳[驗證](../20261028-chemical-n2-verification/verification.md)。
- [ ] TASK-009 QE2分析值差異（先核准計算來源；002在此後驗證判定）。

新增三件及截圖測試矩陣見[補充任務](../20261028-chemical-screenshot-followup/tasks.md)。執行順序以本表與補充規格v1為準，原v1排序留在Git歷史。
