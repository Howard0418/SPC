# 任務順序（待批准，不自動實作）
- 功能 ID：SPC-FORMULA-EFFECTIVE-TRACE-PLAN-20261020；spec v1。
- [規格](spec.md)｜[計畫](plan.md)｜[驗證](verification.md)
- [x] PLAN-001：本次完成現況、建議規則、相依、執行順序與驗收規劃。

| 排序／代號 | 最小工作與交付 | 必要驗證／相依 |
|---|---|---|
| 1／SPC-FORMULA-TRACE-TASK-001 | 定位TransFiles真正寫入端，核對來源時區／原始值、Portal與SPC語法精度，驗證直寫閘門方案；完成可實作契約與基準fixture | Happy：兩入口同輸入；Boundary：切日；Invalid：缺日期／原始輸入；Regression：既有公式與直寫契約。未釐清前不做跨系統整合。 |
| 2／SPC-FORMULA-TRACE-TASK-002 | 有效期間與不可變追溯的最小schema、版本保護及隔離庫migration演練 | AC-004／006／009；FK隔離、並行版號、交易故障、Up／Down／Up、無假造舊版；不和其他DB工作併行。 |
| 3／SPC-FORMULA-TRACE-TASK-003 | SPC選版／發布排程／計算契約、重疊鎖定、預覽過期防護與設定UI | AC-001～005／007；同批跨日、UTC等價、缺版、兩人並發、既有公式精度回歸。 |
| 4／SPC-FORMULA-TRACE-TASK-004 | Portal依日期預覽、確認及初驗／複驗留存 | AC-005～009；改日期重預覽、失敗無半筆、偽造版號／Viewer寫入拒絕。 |
| 5／SPC-FORMULA-TRACE-TASK-005 | TransFiles實際入口契約、直接交易保存與舊版寫入閘門 | AC-001／006／007／009；跨日同批、缺輸入隔離、重送冪等、失敗回復及不影響咬蝕／落塵。 |
| 6／SPC-FORMULA-TRACE-TASK-006 | 分段／逐點明細展示實際版本，端到端與Antigravity案例 | AC-008及全回歸；新舊混合、F變更後重現、多版本區間；測試站實際驗收，正式另授權。 |

TASK-001～006目前全部未執行；只有本次PLAN-001文件完成。每項實作前須細化對應spec與驗證，不將本表視為正式DB／跨系統發布授權。
獨立規格／管制界限有效期及統計分段另案，避免與本批公式計算同時改動。
