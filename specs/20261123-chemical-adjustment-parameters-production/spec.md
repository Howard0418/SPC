# SPC-CHEM-ADJUST-PROD-1123 測試核整參數同步正式

- 使用者授權將藥液量測調整公式頁已核整資料同步正式。頁面維護五欄 chemicalName、tankCapacityLiters、additionControlValue、dilutionControlValue、adjustmentRemark。正式既有其他欄位及資料維持。
- Given 測試與正式啟用 CHEM PPC 依機台代碼、槽位名稱及分析特性代碼唯一配對，When 逐欄對照，Then 只使用測試有明確資料的差異，空白不清空正式現值。非正數、不合欄位契約或配對衝突停止。
- Given 核准資料指紋固定、正式備份以 CHECKSUM 驗證，When 以序列化交易套用，Then 只更新當前設定五欄及更新追溯，另追加相應設定版本與稽核；公式內容、規格、F 表、歷史排程和量測保留。重送應為零變更。
- 正式目標固定 PMR_SPC_2026。作業屬資料同步，網站程式、版本與通知維持。回復依本批原值精確反向處理並追加歷程，避免覆蓋後續正式修改。

[驗證與回復](verification.md)
