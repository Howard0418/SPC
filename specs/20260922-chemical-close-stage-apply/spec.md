# 功能規格：正式 N1／N2 舊晚班編碼轉收線
- 功能 ID：20260922-chemical-close-stage-apply
- 版本：1
- 狀態：完成
- 涉及專案：SPC
- 授權依據：2026-09-22 使用者對正式 198 筆舊 CLOSE 顯示為晚班之說明後明確指示「改」
- 現行需求基準：[9 月前僅開收線](../20260922-chemical-n1n2-shift-cutoff/spec.md)、[班別與取樣階段](../20260917-chemical-shift-stage/spec.md)
- 前案：[舊晚班盤點](../20260922-chemical-close-stage-inventory/spec.md)

## 目的、現況與證據
正式 PMR_SPC_2026 有 198 筆 CHEM、N1／N2、SamplingPhase=CLOSE、SamplingStage=GENERAL。這批是 2026-09-02 開收線編碼（CLOSE＝收線），畫面後來把 CLOSE 標成晚班。測試庫已轉成 OPEN＋CLOSE；正式先前依「不要調整」維持原狀。使用者現改為授權正式轉換。

唯一索引已含 SamplingStage；與既有 OPEN＋CLOSE 衝突 0。195 筆同日另有 OPEN＋GENERAL 開線對，轉換後鍵為 OPEN＋CLOSE，不合併。

## 範圍與非範圍
- 範圍：僅 PMR_SPC_2026 上述 198 筆，只改 SamplingPhase、SamplingStage。
- 非範圍：測試庫、MIDDLE、其他線別、數值／日期／ID／主檔、Portal 庫、網站發布、OPEN＋GENERAL 開線列（本次不改成 OPEN＋OPEN）。

## 需求與驗收
| 需求 ID | 業務規則 | 驗收 ID | 可觀察的通過條件 |
|---|---|---|---|
| R-001 | 舊收線編碼改為現行兩維度 | AC-001 | 198 筆由 CLOSE＋GENERAL 變為 OPEN＋CLOSE；N1=16、N2=182 |
| R-002 | 其他欄位與非候選列不變 | AC-002 | 除兩階段欄與 SQL RowVersion 外逐欄相同；MIDDLE／其他線未變 |
| R-003 | 可回復、不重跑 | AC-003 | 提交前 COPY_ONLY＋CHECKSUM＋VERIFYONLY；提交後剩餘候選 0；可依 before 快照還原兩欄 |

## 例外與邊界
筆數不是 198、出現 OPEN＋CLOSE 同鍵、或無關欄位變動則整筆交易 rollback。不整庫還原。

## 假設與未決問題
正式 API 若尚未依階段查詢，畫面改顯示早班＋收線而非晚班；不因此擴大發布。無阻擋未決。
