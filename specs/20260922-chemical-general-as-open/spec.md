# 合併變更：藥液 GENERAL 視同早班
- 功能 ID：20260922-chemical-general-as-open
- 規模：1
- 狀態：完成
- 專案：SPC＋Portal
- 授權依據：2026-09-22 使用者確認 GENERAL 都算早班、沒有一般；其他線別沒有開線／收線，僅 N1／N2 才分
- 需求基準：[7.4 班別](../../docs/SPC_REQUIREMENTS_BASELINE_2026-08-07.md)、[班別與取樣階段](../20260917-chemical-shift-stage/spec.md)

## 問題、預期行為與範圍
正式 DP／過硫酸鈉 18 筆量測，其中 15 筆班別 GENERAL、3 筆 OPEN。非 N1／N2 管制圖一遇到 OPEN 就改依日期對班，並過濾掉 GENERAL，只剩 3 點。

預期：班別 GENERAL／空白視同早班，畫面不顯示「一般」。開／收線僅 N1／N2。不 UPDATE 資料、不改正式站。

## 需求與驗收
- R-001：SamplingPhase GENERAL（及空白）視同早班 OPEN。
- AC-001：DP 混有 GENERAL＋OPEN 時，早班序列含全部日期點，不丟 GENERAL。
- R-002：僅 N1／N2 有開線／收線；其他線別階段維持 GENERAL。
- AC-002：既有 N1／N2 單線圖與其他線停用階段契約不變。
- R-003：畫面班別不出現「一般」。
- AC-003：Portal 班別標籤 GENERAL 顯示早班；圖表 tooltip 亦然。

## 計畫與任務
- 受影響：chemicalChart.js、SpcChartView.vue、Portal VariableMeasurementEntry.cshtml、需求／變更紀錄。
- [x] T-001：先寫 GENERAL 視同早班與 DP 混合點測試。
- [x] T-002：改製圖對班與標籤。
- [x] T-003：Portal 標籤；驗證後發布 SPC／Portal 測試站。

## 驗證
- 方式：Node 單元測試＋既有 N1／N2 單線案例。
- 實際結果及證據：[verification.md](verification.md)
- 發布：SPC 測試前端、Portal 測試 Web 已發布；正式不發布
