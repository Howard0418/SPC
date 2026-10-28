# 藥液查詢複驗優先與判定
- SPC-ADJUST-1028-TASK-002，版本2，正式發布及必要驗證完成（DONE）。使用者授權優先完成四件藥液小工作；009因F值待確認不改公式。
- 主系統Portal顯示、相依SPC權威判定查詢；無schema／寫入／角色變更。測試通過後，使用者已另行明確授權002／008正式最小修補發布；已完成。
- 基準：[R-002及精度規則](../20261028-spc-adjustment-backlog/spec.md)。

## 需求及BDD
- Given保存初驗NG、複驗OK When查詢 Then主要分析值及判定使用複驗，另列初驗值／判定；複驗0亦有效，無複驗回初驗，滴定原始輸入不得冒充分析值。
- Given日期公式raw=1.004、stored=1.000且原USL=1.002 When查詢 Then使用已保存有效追溯的原始判定NG，顯示取位提示，不由Portal以1.00或最新規格重判。
- Given追溯有缺損或與目前保存結果不符 When查詢 Then明示無法判定，不猜公式／F值／OK。
- Given舊量測沒有日期公式追溯 When查詢 Then取該量測最新且對得上最終保存值的SPC計算紀錄及其規格快照；初驗依該保存規格及未經顯示取位的初驗值判定，標示來源。沒有歷史紀錄／規格就明示無法判定，不套目前主檔。
- Given主檔或公式後續更改 When歷史查詢 Then不重算／回寫歷史；完整分頁、日期、關鍵字、刪除授權及008 GET復原保持。

## 契約
SPC既有製程量測GET保留所有舊欄位，逐頁批次補`effectiveValue`、`hasRecheck`、`initialJudgment`、`judgment`、`judgmentSource`、`precisionNote`。Portal只顯示權威欄位；舊SPC未提供新欄位時仍顯示複驗優先值，但判定顯示無法判定，不假裝OK。新增欄位不改原value／recheckValue。

## 例外／限制
未知是資料不足，不等於NG或OOC。日期公式追溯採當前初驗receipt與最新複驗phase，核對stored結果及已存權威evidence；舊初驗若沒有raw追溯只能明示以保存值／保存規格判定。不修改009未決F。正式SPC目前0.1.71，新API欄位正式部署須核對差異及另行授權，不把測試更新混稱正式。
