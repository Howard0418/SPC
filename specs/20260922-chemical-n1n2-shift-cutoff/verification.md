# 驗證紀錄：N1／N2 9 月前僅開收線、資料不調整
- 功能 ID：20260922-chemical-n1n2-shift-cutoff
- 日期：2026-09-22
- 範圍：兩庫唯讀
- 資料修正：未執行（使用者要求不要調整）
- 發布：不適用
- 截止：日報日 2026-09-01（9 月前／9 月起）

## 正式庫 PMR_SPC_2026
9 月前（2026-08）：僅 OPEN／CLOSE 兩個 SamplingPhase，階段皆 GENERAL；無 MIDDLE。
對應現場開線／收線舊編碼，沒有早班／中班。

| 線別 | OPEN+GENERAL | CLOSE+GENERAL | 日期 |
|---|---:|---:|---|
| N1 | 8 | 8 | 2026-08-25 |
| N2 | 149 | 152 | 2026-08-03～08-31 |

9 月起才出現中班 MIDDLE：N1 自 2026-09-10、N2 自 2026-09-11。
9/1～9/3 仍有舊 CLOSE+GENERAL（N1 8 筆、N2 30 筆）；N2 另有 10 筆 9/18 OPEN+CLOSE。以上皆維持現狀。

## 測試庫 PMR_SPC_TEST
9 月前已是 OPEN+OPEN／OPEN+CLOSE（先前測試庫階段修復結果）。9 月起 N1 有 MIDDLE+GENERAL 7 筆（9/10）、N2 有 MIDDLE+CLOSE 30 筆（9/11～9/16）。本次亦不還原、不續改。

## 結論
AC-001／AC-002 通過：規則已記錄；兩庫分布與「9 月前只有開收線、9 月後才分早中班」相符；**未寫入任何資料**。前案正式 198 筆轉換取消。

## 證據
- distribution.json
- 
elease-staging/chemical-n1n2-shift-cutoff-20260922/distribution.json
