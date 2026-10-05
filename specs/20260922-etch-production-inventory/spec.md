# 小型變更：咬蝕正式庫唯讀盤點
- 功能 ID：20260922-etch-production-inventory
- 版本：1
- 狀態：已完成（僅盤點，未寫入）
- 專案：SPC＋Portal
- 授權依據：2026-09-22 使用者選 A「先唯讀盤點正式咬蝕」
- 需求基準：[需求索引](../../docs/requirements.md)、[9月咬蝕第3批](../20260918-etch-month-import/verification.md)

## 問題、預期行為與範圍
比對測試庫已匯入的 2026-09 咬蝕 48 份與正式目標庫現況；只讀、不寫入、不發布。
正式匯入工具與寫入不在本次範圍。

## 需求與驗收
- R-001：唯讀盤點正式／測試 Portal 與 SPC 的 9 月咬蝕日報與 ETCH 來源量測。
- AC-001：產出環境、筆數、48 鍵差異 CSV、主檔缺口與建議下一步。
- R-002：標示不得匯入的隔離日報。
- AC-002：負值 4 份與 9/18 未完整 4 份列於隔離清單。

## 計畫與任務
- 受影響：僅 specs/release-staging 盤點產物；無應用程式變更。
- [x] T-001：唯讀查詢。
- [x] T-002：差異表與 findings。
- [x] T-003：同步需求索引與 CHANGELOG。

## 驗證
- 方式：SqlClient 唯讀；manifest tch-month-20260918/input.json
- 實際結果：見 verification.md 與 findings.json
- 發布狀態：不適用（無程式／資料寫入）
- 限制：Portal production 連線庫名為 PMR_PORTAL_UAT，是否為真正正式 Portal 待確認。
