# 合併變更：N1／N2 舊晚班匯入資料轉收線盤點
- 功能 ID：20260922-chemical-close-stage-inventory
- 規模：1
- 狀態：盤點完成；正式轉換已於 20260922-chemical-close-stage-apply 執行
- 專案：SPC
- 授權依據：2026-09-22 使用者同意兩庫唯讀盤點；其後授權正式轉換
- 後續規則：[9 月前僅開收線](../20260922-chemical-n1n2-shift-cutoff/spec.md)
- 需求基準：[班別與取樣階段](../20260917-chemical-shift-stage/spec.md)、[階段契約修復](../20260918-chemical-stage-repair/spec.md)

## 主題、已知現況與範圍
現行 CHEM、N1／N2 藥液匯入資料，盤點舊 SamplingPhase=CLOSE（原收線舊編碼）且尚在預設階段的資料。

盤點當次只讀。正式轉換見後案。

## 需求與驗收
- R-001：確認測試與正式庫是否存在 SamplingStage 欄位及索引。
- AC-001：列出兩庫 schema 狀態。
- R-002：盤點 CHEM、N1/N2、SamplingPhase=CLOSE、階段 GENERAL、日報批次候選。
- AC-002：依資料庫、線別、來源批次、衝突、重複列出候選筆數。
- R-003：預覽轉換後是否衝突。
- AC-003：列出已有同鍵 OPEN＋CLOSE 目標列的筆數；衝突不得自動改寫。

## 計畫與任務
- [x] 建立盤點規格。
- [x] 唯讀查詢兩庫 schema。
- [x] 唯讀查詢候選、批次與衝突。
- [x] 產出建議與驗證紀錄。
- [x] 後案正式轉換 198 筆。

## 驗證
- 方式：只讀 SQL，連線只允許確認的 PMR_SPC_TEST、PMR_SPC_2026。
- 證據連結：本目錄 [verification.md](verification.md)。
- 發布狀態：不適用。
