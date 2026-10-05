# 合併變更：N1／N2 9 月前僅開收線、資料不調整
- 功能 ID：20260922-chemical-n1n2-shift-cutoff
- 規模：1
- 狀態：業務規則已記錄；舊 CLOSE 已於後案轉換，開線 GENERAL 與 MIDDLE 仍不調整
- 專案：SPC
- 授權依據：2026-09-22 使用者確認 9 月前 N1/N2 只有開線／收線，9 月後才分早班／中班，並明確要求不要調整
- 前案：[舊晚班轉收線盤點](../20260922-chemical-close-stage-inventory/spec.md)、[班別與取樣階段](../20260917-chemical-shift-stage/spec.md)

## 主題、已知現況與範圍
N1／N2 藥液：
- 2026-09-01 前：現場只有開線與收線，沒有早班／中班。
- 2026-09-01 起：才分早班／中班。
- 9 月前後規則維持；其後使用者授權僅轉換正式舊 CLOSE 198 筆為 OPEN＋CLOSE，見 [正式轉換](../20260922-chemical-close-stage-apply/spec.md)。`OPEN`＋`GENERAL` 與 MIDDLE 仍不調整。

## 需求與驗收
- R-001：記錄 9 月前後班別／開收線規則，且禁止調整資料。
- AC-001：規格與需求索引載明截止日與不調整。
- R-002：唯讀核對兩庫 N1/N2 CHEM 在截止前後的 SamplingPhase／SamplingStage 分布。
- AC-002：列出 9 月前是否僅 OPEN／CLOSE、9 月後 MIDDLE 出現日期；查詢後筆數不變。

## 計畫與任務
- [x] 建立規格。
- [x] 兩庫唯讀分布查詢。
- [x] 取消前案正式轉換建議並同步變更紀錄。

## 驗證
- 方式：只讀 SQL，連線只允許 PMR_SPC_TEST、PMR_SPC_2026。
- 證據：[verification.md](verification.md)、[distribution.json](distribution.json)
- 發布狀態：不適用。
