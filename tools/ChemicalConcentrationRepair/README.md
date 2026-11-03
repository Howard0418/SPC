# 2026-11-03 測試濃度修正工具

[主規格](../../specs/20261103-chemical-f-consistency/spec.md)。此為指定原始活頁簿 SHA256／87 個主檔的單次修正，不是一般匯入器。拒絕正式、未知 SQL 主機／資料庫、配置漂移與已有排程衝突。預設 SELECT-only，`--verify` 只預覽，不建立憑證／量測；`--apply` 才在同一 serializable 交易內追加版本／F 快照及排程。

```powershell
dotnet run --project tools/ChemicalConcentrationRepair -- CONFIG MANIFEST REPORT
dotnet run --project tools/ChemicalConcentrationRepair -- CONFIG MANIFEST REPORT --verify
```

CONFIG 使用測試站本機設定，不提交連線字串。MANIFEST 為來源逐格翻譯後的 SHA／2026-11-03 起日、FCells 與 Rows（PpcId、線別、槽ID、特性碼、ExpectedConfig、Config、來源定位）。本次依107個來源公式項目、214初複驗式及既有線／槽／項目／單位唯一比對建立87列；其他欄位必須與原配置相同。輸入、before image 與輸出均屬 Git 外證據。

83項可計算配置啟用日期政策，舊無憑證匯入將拒絕；4項僅補正濃度配置，其他公式的 NA 上限另待確認，不建立排程。不更新全域啟用 F、不改其他兩式、規格或舊量測。原版本不覆寫；日期無排程不得套用最新 F。舊十月匯入仍需該日核准版本及原始時間／班別。

回復應先核對 report 的新增版本／revision 與 before image；在無新增計算憑證、設定未再修改的前提下回復舊主檔與原政策指標，保留所有追加歷史。不得刪歷史、receipt 或量測以重送。
